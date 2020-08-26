
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ExpressiveDynamoDB.Extensions;
using Ddb = Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.ExpressionGeneration
{
    public abstract class ExpressionVisitorBase : ExpressionVisitor
    {
        protected abstract IDictionary<ExpressionType, string> SupportedExpressionTypesMap { get; }
        private readonly Ddb.Expression _expression;
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        public ExpressionVisitorBase(Ddb.Expression expression)
        {
            _expression = expression;
        }

        protected void Finalise()
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

            if (!SupportedExpressionTypesMap.ContainsKey(expression.NodeType))
            {
                throw new InvalidOperationException("Operation invalid for DynamoDB Expression");
            }

            _stringBuilder.Append(SupportedExpressionTypesMap[expression.NodeType]);

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

        protected virtual string GetAttributeValueName(string desiredName, Ddb.DynamoDBEntry? entryValue)
        {
            var valueName = $":{desiredName}";
            var counter = _expression.ExpressionAttributeValues.Count;
            while(_expression.ExpressionAttributeValues.ContainsKey(valueName) && _expression.ExpressionAttributeValues[valueName] == entryValue){
                valueName = $":{desiredName}{counter}";
                counter++;
            }
            return valueName;
        }
        
        protected static ConstantExpression GetMemberConstant(MemberExpression node)
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

        protected static Ddb.DynamoDBEntry? TryConvertToDynamoDbEntry(ConstantExpression expression)
        {
            var lambda = Expression.Lambda<Func<Ddb.DynamoDBEntry>>(Expression.Convert(expression, typeof(Ddb.DynamoDBEntry)));
            var func = lambda.Compile();
            return func();
        }

        protected static object GetFieldValue(MemberExpression node)
        {
            var fieldInfo = (FieldInfo)node.Member;

            var instance = (node.Expression == null) ? null : TryEvaluate(node.Expression).Value;

            return fieldInfo.GetValue(instance);
        }

        protected static object GetPropertyValue(MemberExpression node)
        {
            var propertyInfo = (PropertyInfo)node.Member;

            var instance = (node.Expression == null) ? null : TryEvaluate(node.Expression).Value;

            return propertyInfo.GetValue(instance, null);
        }

        protected static ConstantExpression TryEvaluate(Expression expression)
        {

            if (expression.NodeType == ExpressionType.Constant)
            {
                return (ConstantExpression)expression;
            }
            throw new NotSupportedException();

        }
    }
}