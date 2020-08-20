
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ExpressiveDynamoDB.Extensions;
using Ddb = Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.QueryGeneration
{
    public enum AllowedExpressionTypes
    {
        KEY_CONDITIONS_ONLY,
        ALL
    }

    public class DdbExpressionVisitor : ExpressionVisitor
    {
        private static IDictionary<ExpressionType, string> ExpressionTypeMap = new Dictionary<ExpressionType, string>{
            {ExpressionType.Equal, " = "},
            {ExpressionType.GreaterThan, " > "},
            {ExpressionType.GreaterThanOrEqual, " >= "},
            {ExpressionType.LessThan, " < "},
            {ExpressionType.LessThanOrEqual, " <= "},
            {ExpressionType.NotEqual, " <> "},
            {ExpressionType.And, " AND "},
            {ExpressionType.AndAlso, " AND "},
            {ExpressionType.Or, " OR "},
        };

        private static MethodInfo StringContainsMethod = ((Func<string, bool>)"".Contains).GetMethodInfo();
        private static MethodInfo StringStartsWithMethod = ((Func<string, bool>)"".StartsWith).GetMethodInfo();
        private static MethodInfo BetweenMethod = typeof(Functions).GetMethod(nameof(Functions.Between));
        private static MethodInfo EnumerableContainsMethod = ((Func<object, bool>)new object[0].Contains).GetMethodInfo();


        private readonly Ddb.Expression _expression;
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public DdbExpressionVisitor(Ddb.Expression expression)
        {
            _expression = expression;
        }

        public void Finalise()
        {
            _expression.ExpressionStatement = _stringBuilder.ToString();
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Negate)
            {
                _stringBuilder.Append("NOT ");
            }

            Visit(expression.Operand);

            return expression;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            _stringBuilder.Append("(");

            Visit(expression.Left);

            if (!ExpressionTypeMap.ContainsKey(expression.NodeType))
            {
                throw new InvalidOperationException("Operation invalid for DynamoDB Expression");
            }

            _stringBuilder.Append(ExpressionTypeMap[expression.NodeType]);

            Visit(expression.Right);

            _stringBuilder.Append(")");

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
            var memberExpressionName = $"#{expression.Member.Name}";
            var memberAttributeName = expression.Member.Name;
            if (expression.Member is PropertyInfo propertyInfo)
            {
                memberAttributeName = propertyInfo.DynamoDbAttributeName();
            }
            _stringBuilder.Append(memberExpressionName);
            _expression.ExpressionAttributeNames[memberExpressionName] = memberAttributeName;
            return expression;
        }

        private Expression HandleConstant(string constantName, ConstantExpression expression)
        {
            var ddbExpressionValue = TryConvertToDynamoDbEntry(expression);
            if (ddbExpressionValue == null)
            {
                throw new InvalidOperationException($"{expression.Value.GetType().Name} is not a DynamoDBEntry type");
            }
            
            var valueName = GetAttributeValueName(constantName, ddbExpressionValue);
            _stringBuilder.Append(valueName);
            _expression.ExpressionAttributeValues[valueName] = ddbExpressionValue;

            return expression;
        }

        private string GetAttributeValueName(string desiredName, Ddb.DynamoDBEntry? entryValue)
        {
            var valueName = $":{desiredName}";
            var counter = _expression.ExpressionAttributeValues.Count;
            while(_expression.ExpressionAttributeValues.ContainsKey(valueName) && _expression.ExpressionAttributeValues[valueName] == entryValue){
                valueName = $":{desiredName}{counter}";
                counter++;
            }
            return valueName;
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
            if (expression.Method.Equals(StringContainsMethod))
            {
                if(expression.Arguments.Count() != 1)
                {
                    throw new InvalidOperationException("Contains method is expecting 1 and only 1 argument.");
                }

                _stringBuilder.Append("contains(");
                Visit(expression.Object);
                _stringBuilder.Append(", ");
                Visit(expression.Arguments[0]);
                _stringBuilder.Append(")");
                return expression;
            }

            if (expression.Method.Equals(StringStartsWithMethod))
            {
                if(expression.Arguments.Count() != 1)
                {
                    throw new InvalidOperationException("StartsWith method is expecting 1 and only 1 argument.");
                }

                _stringBuilder.Append("begins_with(");
                Visit(expression.Object);
                _stringBuilder.Append(", ");
                Visit(expression.Arguments[0]);
                _stringBuilder.Append(")");
                return expression;
            }

            if (expression.Method.Equals(BetweenMethod))
            {
                if(expression.Arguments.Count() != 3)
                {
                    throw new InvalidOperationException("Between method is expecting 3 and exactly 3 arguments.");
                }

                Visit(expression.Arguments[0]);
                _stringBuilder.Append(" BETWEEN ");
                Visit(expression.Arguments[1]);
                _stringBuilder.Append(" AND ");
                Visit(expression.Arguments[2]);
                return expression;
            }

            if(expression.Method.Equals(EnumerableContainsMethod))
            {

            }

            throw new InvalidOperationException($"{expression.Method.Name} is not a supported method!");
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

        public static Ddb.Expression BuildStatement<T>(Expression<T> expression)
        {
            var ddbExpression = new Ddb.Expression();
            var visitor = new DdbExpressionVisitor(ddbExpression);
            visitor.Visit(expression);
            visitor.Finalise();
            return ddbExpression;
        }
    }
}