using System;
using System.Linq.Expressions;
using Amazon.DynamoDBv2.Model;
using ExpressiveDynamoDB.ExpressionGeneration;

namespace ExpressiveDynamoDB.Extensions
{
    public static class ScanRequestExtensions
    {
        public static ScanRequest SetFilterExpression<T>(this ScanRequest sr, Expression<Func<T, bool>> expression)
        {
            var ddbExpression = FilterConditionExpressionVisitor.BuildExpression<T>(expression);
            sr.FilterExpression = ddbExpression.ExpressionStatement;
            sr.ExpressionAttributeNames.AddRange(ddbExpression.ExpressionAttributeNames);
            sr.ExpressionAttributeValues.AddRange(ddbExpression.ExpressionAttributeValues.ToAttributeMap());
            return sr;
        }
    }
}