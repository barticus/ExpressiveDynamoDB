using System;
using System.Linq.Expressions;
using Amazon.DynamoDBv2.DocumentModel;
using ExpressiveDynamoDB.ExpressionGeneration;

namespace ExpressiveDynamoDB.Extensions
{
    public static class QueryFilterExtensions
    {
        public static void AddCondition<T>(this QueryFilter qf, Expression<Func<T, bool>> expression)
        {
            var conditions = FilterConditionExpressionVisitor.BuildConditions<T>(expression);
            foreach(var kvp in conditions)
            {
                qf.AddCondition(kvp.Key, kvp.Value);
            }
        }

        public static void AddKeyCondition<T>(this QueryFilter qf, Expression<Func<T, bool>> expression)
        {
            var conditions = FilterConditionExpressionVisitor.BuildConditions<T>(
                expression, 
                FilterConditionExpressionVisitor.AllowedOperations.KEY_CONDITIONS_ONLY
            );
            foreach(var kvp in conditions)
            {
                qf.AddCondition(kvp.Key, kvp.Value);
            }
        }
    }
}