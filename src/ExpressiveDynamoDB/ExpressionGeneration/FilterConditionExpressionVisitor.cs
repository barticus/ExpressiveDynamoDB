
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressiveDynamoDB.Extensions;
using Ddb = Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using System.Text;
using Amazon.DynamoDBv2.DataModel;

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

        private static readonly IReadOnlyDictionary<ExpressionType, string> JoinOperations = new Dictionary<ExpressionType, string>{
            {ExpressionType.AndAlso, " AND "},
            {ExpressionType.OrElse, " OR "},
        };

        private static readonly IReadOnlyDictionary<AllowedOperations, IReadOnlyList<SupportedMethod>> SupportedMethodsMap = new Dictionary<AllowedOperations, IReadOnlyList<SupportedMethod>>{
            { AllowedOperations.ALL, new List<SupportedMethod>{
                SupportedMethod.BetweenMethod,
                SupportedMethod.EnumerableContainsMethod,
                SupportedMethod.AttributeExistsMethod,
                SupportedMethod.AttributeNotExistsMethod,
                SupportedMethod.StringContainsMethod,
                SupportedMethod.StringStartsWithMethod,
                SupportedMethod.SizeMethod,
                SupportedMethod.EnumerableCountMethod,
                SupportedMethod.AttributeTypeMethod,
            }},
            { AllowedOperations.KEY_CONDITIONS_ONLY, new List<SupportedMethod>{
                SupportedMethod.BetweenMethod,
                SupportedMethod.StringStartsWithMethod,
            }},
        };

        private readonly Dictionary<string, Condition> Conditions = new Dictionary<string, Condition>();
        private readonly List<WorkingCondition> ProcessedWorkingConditions = new List<WorkingCondition>();
        private readonly StringBuilder StringBuilder = new StringBuilder();
        private readonly Dictionary<Type, IPropertyConverter> TypeConverters = new Dictionary<Type, IPropertyConverter>();

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
        private bool ShouldWriteToSameCondition { get; set; } = false;

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

        public Ddb.Expression GetGeneratedExpression()
        {
            var expression = new Ddb.Expression();
            var statement = StringBuilder.ToString();
            if (statement.First() == '(' && statement.Last() == ')')
            {
                statement = statement.Substring(1, statement.Length - 2);
            }
            expression.ExpressionStatement = statement;
            foreach (var wc in ProcessedWorkingConditions)
            {
                if (!wc.IsPropertyNameSet) continue;

                expression.ExpressionAttributeNames.AddRange(WorkingCondition.AttributeNameKeys(wc.PropertyName!));
                foreach (var kvp in wc.Values)
                {
                    var explodedValues = wc.ExplodeAttributeValueEntries(kvp.Key, kvp.Value);
                    expression.ExpressionAttributeValues.AddRange(explodedValues);
                }
            }
            return expression;
        }

        private void SaveAndResetWorkingCondition()
        {
            if (WorkingCondition != null && WorkingCondition.IsPropertyNameSet)
            {
                if (ShouldWriteToSameCondition)
                {
                    WorkingCondition.MemberExpression = WorkingCondition.ToExpressionStatement();
                    return;
                }
                ProcessedWorkingConditions.Add(WorkingCondition);
                var kvp = WorkingCondition.ToCondition();
                if (kvp.HasValue)
                {
                    Conditions.Add(kvp.Value.Key, kvp.Value.Value);
                }
                StringBuilder.Append(WorkingCondition.ToExpressionStatement());
            }
            WorkingCondition = null;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            if (ExpressionTypeMap[AllowedOperationSet].ContainsKey(expression.NodeType))
            {
                ShouldWriteToSameCondition = true;
            }

            if (JoinOperations.ContainsKey(expression.NodeType))
            {
                StringBuilder.Append("(");
            }

            Visit(expression.Left);

            if (JoinOperations.ContainsKey(expression.NodeType))
            {
                StringBuilder.Append(JoinOperations[expression.NodeType]);
            }

            Visit(expression.Right);
            ShouldWriteToSameCondition = false;

            if (ExpressionTypeMap[AllowedOperationSet].ContainsKey(expression.NodeType))
            {
                WorkingCondition!.ComparisonOperator = ExpressionTypeMap[AllowedOperationSet][expression.NodeType];
                SaveAndResetWorkingCondition();
            }

            if (JoinOperations.ContainsKey(expression.NodeType))
            {
                StringBuilder.Append(")");
            }

            return expression;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            var memberPath = GetMemberPath(expression);
            switch (expression.Expression.NodeType)
            {
                case ExpressionType.Constant:
                    {
                        return HandleConstant(memberPath, GetMemberConstant(expression));
                    }
                default:
                    {
                        return HandleMember(memberPath, expression);
                    }
            }
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            return HandleConstant($"p{expression.Type.Name}", expression);
        }

        private Expression HandleMember(string name, MemberExpression expression)
        {
            Visit(expression.Expression);
            WorkingCondition!.PropertyName = name;
            return expression;
        }

        private Expression HandleConstant(string constantName, ConstantExpression expression)
        {
            var ddbExpressionValue = TryConvertToDynamoDbEntry(expression);
            if (ddbExpressionValue == null)
            {
                throw new InvalidOperationException($"{expression.Value.GetType().Name} is not a DynamoDBEntry type");
            }
            AddWorkingConditionValue(constantName, ddbExpressionValue);
            return expression;
        }

        private void AddWorkingConditionValue(string constantName, Ddb.DynamoDBEntry entry)
        {
            var index = 1;
            var runningConstant = constantName;
            var existingCondition = FindConditionValue(runningConstant);
            while(existingCondition != null)
            {
                if(existingCondition.Value.Value.Equals(entry))
                {
                    // value is already stored as a condition
                    break;
                }
                index++;
                runningConstant = $"{constantName}{index}";
                existingCondition = FindConditionValue(runningConstant);
            }
            WorkingCondition!.Values[runningConstant] = entry;
        }

        private KeyValuePair<string, Ddb.DynamoDBEntry>? FindConditionValue(string conditionValueKey)
        {
            if(WorkingCondition!.Values.ContainsKey(conditionValueKey)) {
                return WorkingCondition!.Values.First(c => c.Key == conditionValueKey);
            }
            var foundWorkingCondition = ProcessedWorkingConditions.FirstOrDefault(wc => wc.Values.ContainsKey(conditionValueKey));
            return foundWorkingCondition != null ? foundWorkingCondition.Values.First(c => c.Key == conditionValueKey) : (KeyValuePair<string, Ddb.DynamoDBEntry>?)null;
        }

        private static string GetMemberPath(MemberExpression? me)
        {
            var parts = new List<string>();

            while (me != null)
            {
                var memberName = me.Member.Name;
                if (me.Member is PropertyInfo propertyInfo)
                {
                    memberName = propertyInfo.DynamoDBAttributeName();
                }
                parts.Add(memberName);

                me = me.Expression as MemberExpression;
            }

            parts.Reverse();
            return string.Join(".", parts);
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
            var foundMethod = SupportedMethodsMap[AllowedOperationSet].FirstOrDefault(m => m.MatchesMethod(expression.Method));
            if (foundMethod == null)
            {
                throw new InvalidOperationException($"{expression.Method.DeclaringType.Name}.{expression.Method.Name} is not a supported method!");
            }

            if (foundMethod.ExpectedArgumentCount != expression.Arguments.Count())
            {
                throw new InvalidOperationException($"{expression.Method.Name} method is expecting {foundMethod.ExpectedArgumentCount} arguments.");
            }

            if(foundMethod.TypeConverters.Any())
            {
                foreach(var kvp in foundMethod.TypeConverters)
                {
                    if(!TypeConverters.ContainsKey(kvp.Key))
                    {
                        TypeConverters.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            bool methodCalledOnProperty = false;
            if (foundMethod.VisitObject)
            {
                Visit(expression.Object);
                methodCalledOnProperty = WorkingCondition!.IsPropertyNameSet;
            }

            if (foundMethod.VisitArguments)
            {
                var index = 0;
                foreach (var argument in expression.Arguments)
                {
                    Visit(argument);
                    if (index == 0 && !foundMethod.VisitObject)
                    {
                        methodCalledOnProperty = WorkingCondition!.IsPropertyNameSet;
                    }
                    index++;
                }
            }
            var wasPropertyArgument = !methodCalledOnProperty && WorkingCondition!.IsPropertyNameSet;

            WorkingCondition!.ComparisonOperator = wasPropertyArgument && foundMethod.ComparisonOperatorIfPropertyWasArgument != null ?
                foundMethod.ComparisonOperatorIfPropertyWasArgument :
                foundMethod.ComparisonOperator;
            WorkingCondition.CanMapToCondition = foundMethod.CanMapToCondition;

            SaveAndResetWorkingCondition();
            return expression;
        }

        private Ddb.DynamoDBEntry? TryConvertToDynamoDbEntry(ConstantExpression expression)
        {
            if(TypeConverters.ContainsKey(expression.Value.GetType()))
            {
                return TypeConverters[expression.Value.GetType()].ToEntry(expression.Value);
            }
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

        private static ConstantExpression TryEvaluate(Expression? expression)
        {
            if (expression != null && expression.NodeType == ExpressionType.Constant)
            {
                return (ConstantExpression)expression;
            }
            throw new NotSupportedException($"{expression?.NodeType} could not be converted to a constant.");

        }

        public static IReadOnlyDictionary<string, Condition> BuildConditions<T>(Expression<Func<T, bool>> expression, AllowedOperations allowedOperationSet = AllowedOperations.ALL)
        {
            var visitor = new FilterConditionExpressionVisitor(allowedOperationSet);
            visitor.Visit(expression);
            return visitor.GetGeneratedConditions();
        }

        public static Ddb.Expression BuildExpression<T>(Expression<Func<T, bool>> expression, AllowedOperations allowedOperationSet = AllowedOperations.ALL)
        {
            var visitor = new FilterConditionExpressionVisitor(allowedOperationSet);
            visitor.Visit(expression);
            return visitor.GetGeneratedExpression();
        }
    }
}