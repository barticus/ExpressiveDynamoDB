
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressiveDynamoDB.Extensions;
using Ddb = Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.ExpressionGeneration
{
    public class FilterConditionExpressionVisitor : ExpressionVisitor
    {
        public enum AllowedOperations
        {
            ALL,
            KEY_CONDITIONS_ONLY
        }

        // Supported operations: https://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/P_Amazon_DynamoDBv2_Model_Condition_ComparisonOperator.htm
        private static readonly IReadOnlyDictionary<AllowedOperations, IReadOnlyDictionary<ExpressionType, ComparisonOperator>> ExpressionTypeMap = new Dictionary<AllowedOperations, IReadOnlyDictionary<ExpressionType, ComparisonOperator>>{
            { AllowedOperations.ALL, new Dictionary<ExpressionType, ComparisonOperator>{
                {ExpressionType.Equal, ComparisonOperator.EQ},
                {ExpressionType.NotEqual, ComparisonOperator.NE},
                {ExpressionType.LessThanOrEqual, ComparisonOperator.LE},
                {ExpressionType.LessThan, ComparisonOperator.LT},
                {ExpressionType.GreaterThan, ComparisonOperator.GT},
                {ExpressionType.GreaterThanOrEqual, ComparisonOperator.GE},
            }},
            { AllowedOperations.KEY_CONDITIONS_ONLY, new Dictionary<ExpressionType, ComparisonOperator>{
                {ExpressionType.Equal, ComparisonOperator.EQ},
                {ExpressionType.LessThanOrEqual, ComparisonOperator.LE},
                {ExpressionType.LessThan, ComparisonOperator.LT},
                {ExpressionType.GreaterThan, ComparisonOperator.GT},
                {ExpressionType.GreaterThanOrEqual, ComparisonOperator.GE},
            }},
        };

        private static readonly IReadOnlyDictionary<AllowedOperations, IReadOnlyList<SupportedMethod>> SupportedMethodsMap = new Dictionary<AllowedOperations, IReadOnlyList<SupportedMethod>>{
            { AllowedOperations.ALL, new List<SupportedMethod>{
                SupportedMethod.BetweenMethod,
                //SupportedMethod.EnumerableContainsMethod,
                SupportedMethod.StringContainsMethod,
                SupportedMethod.StringStartsWithMethod,
            }},
            { AllowedOperations.KEY_CONDITIONS_ONLY, new List<SupportedMethod>{
                SupportedMethod.BetweenMethod,
                SupportedMethod.StringStartsWithMethod,
            }},
        };

        private readonly Dictionary<string, Condition> Conditions = new Dictionary<string, Condition>();

        private WorkingCondition? _workingCondition = null;
        private WorkingCondition? WorkingCondition
        {
            get
            {
                if (_workingCondition == null)
                {
                    _workingCondition = new WorkingCondition();
                }
                return _workingCondition;
            }
            set
            {
                _workingCondition = value;
            }
        }
        private AllowedOperations AllowedOperationSet { get; set; }

        public FilterConditionExpressionVisitor(AllowedOperations allowedOperationSet = AllowedOperations.ALL)
        {
            AllowedOperationSet = allowedOperationSet;
        }

        public FilterConditionExpressionVisitor(Dictionary<string, Condition> conditions, AllowedOperations allowedOperationSet = AllowedOperations.ALL) : this(allowedOperationSet)
        {
            Conditions = conditions;
        }

        public IReadOnlyDictionary<String, Condition> GetGeneratedConditions()
        {
            return Conditions;
        }

        public Ddb.ScanFilter AsScanFilter()
        {
            var sf = new Ddb.ScanFilter();
            foreach (var kvp in Conditions)
            {
                sf.AddCondition(kvp.Key, kvp.Value);
            }
            return sf;
        }

        public Ddb.QueryFilter AsQueryFilter()
        {
            var qf = new Ddb.QueryFilter();
            foreach (var kvp in Conditions)
            {
                qf.AddCondition(kvp.Key, kvp.Value);
            }
            return qf;
        }

        private void SaveAndResetWorkingCondition()
        {
            if (WorkingCondition != null && !string.IsNullOrEmpty(WorkingCondition.PropertyName))
            {
                var kvp = WorkingCondition.ToCondition();
                Conditions.Add(kvp.Key, kvp.Value);
            }
            WorkingCondition = null;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            Visit(expression.Left);
            Visit(expression.Right);

            if (ExpressionTypeMap[AllowedOperationSet].ContainsKey(expression.NodeType))
            {
                WorkingCondition!.ComparisonOperator = ExpressionTypeMap[AllowedOperationSet][expression.NodeType];
                SaveAndResetWorkingCondition();
            }

            return expression;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            switch (expression.Expression.NodeType)
            {
                case ExpressionType.Constant:
                case ExpressionType.MemberAccess:
                    {
                        return HandleConstant(expression.Member.Name, GetMemberConstant(expression));
                    }
                default:
                    {
                        return HandleMember(expression);
                    }
            }
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            return HandleConstant($"p{expression.Type.Name}", expression);
        }

        private Expression HandleMember(MemberExpression expression)
        {
            Visit(expression.Expression);
            var memberAttributeName = expression.Member.Name;
            if (expression.Member is PropertyInfo propertyInfo)
            {
                memberAttributeName = propertyInfo.DynamoDbAttributeName();
            }
            WorkingCondition!.PropertyName = memberAttributeName;
            return expression;
        }

        private Expression HandleConstant(string constantName, ConstantExpression expression)
        {
            var ddbExpressionValue = TryConvertToDynamoDbEntry(expression);
            if (ddbExpressionValue == null)
            {
                throw new InvalidOperationException($"{expression.Value.GetType().Name} is not a DynamoDBEntry type");
            }
            WorkingCondition!.Values[constantName] = ddbExpressionValue;
            return expression;
        }

        private static ConstantExpression GetMemberConstant(MemberExpression node)
        {
            object value;

            if (node.Member.MemberType == MemberTypes.Field)
            {
                value = GetFieldValue(node);
            }
            else if (node.Member.MemberType == MemberTypes.Property)
            {
                value = GetPropertyValue(node);
            }
            else
            {
                throw new NotSupportedException();
            }

            return Expression.Constant(value, node.Type);
        }


        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var foundMethod = SupportedMethodsMap[AllowedOperationSet].FirstOrDefault(m => expression.Method.Equals(m.MethodInfo));
            if (foundMethod == null)
            {
                throw new InvalidOperationException($"{expression.Method.Name} is not a supported method!");
            }

            if (foundMethod.ExpectedArgumentCount != expression.Arguments.Count())
            {
                throw new InvalidOperationException($"{foundMethod.MethodInfo.Name} method is expecting {foundMethod.ExpectedArgumentCount} arguments.");
            }

            if (foundMethod.VisitObject)
            {
                Visit(expression.Object);
            }

            if (foundMethod.VisitArguments)
            {
                foreach (var argument in expression.Arguments)
                {
                    Visit(argument);
                }
            }

            WorkingCondition!.ComparisonOperator = foundMethod.ComparisonOperator;

            SaveAndResetWorkingCondition();
            return expression;
        }

        private static Ddb.DynamoDBEntry? TryConvertToDynamoDbEntry(ConstantExpression expression)
        {
            var lambda = Expression.Lambda<Func<Ddb.DynamoDBEntry>>(Expression.Convert(expression, typeof(Ddb.DynamoDBEntry)));
            var func = lambda.Compile();
            return func();
        }

        private static object GetFieldValue(MemberExpression node)
        {
            var fieldInfo = (FieldInfo)node.Member;

            var instance = (node.Expression == null) ? null : TryEvaluate(node.Expression).Value;

            return fieldInfo.GetValue(instance);
        }

        private static object GetPropertyValue(MemberExpression node)
        {
            var propertyInfo = (PropertyInfo)node.Member;

            var instance = (node.Expression == null) ? null : TryEvaluate(node.Expression).Value;

            return propertyInfo.GetValue(instance, null);
        }

        private static ConstantExpression TryEvaluate(Expression expression)
        {

            if (expression.NodeType == ExpressionType.Constant)
            {
                return (ConstantExpression)expression;
            }
            throw new NotSupportedException();

        }

        public static IReadOnlyDictionary<string, Condition> BuildStatement<T>(Expression<T> expression)
        {
            var visitor = new FilterConditionExpressionVisitor();
            visitor.Visit(expression);
            return visitor.GetGeneratedConditions();
        }
    }
}