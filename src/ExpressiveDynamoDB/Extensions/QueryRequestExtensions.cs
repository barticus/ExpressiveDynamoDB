using System;
using System.Linq.Expressions;
using Amazon.DynamoDBv2.Model;
using ExpressiveDynamoDB.ExpressionGeneration;
using static ExpressiveDynamoDB.ExpressionGeneration.FilterConditionExpressionVisitor;

namespace ExpressiveDynamoDB.Extensions
{
    public static class QueryRequestExtensions
    {
        public static QueryRequest FilterExpression<T>(this QueryRequest qr, Expression<Func<T, bool>> expression)
        {
            var ddbExpression = FilterConditionExpressionVisitor.BuildExpression<T>(expression);
            qr.FilterExpression = ddbExpression.ExpressionStatement;
            qr.ExpressionAttributeNames.AddRange(ddbExpression.ExpressionAttributeNames);
            qr.ExpressionAttributeValues.AddRange(ddbExpression.ExpressionAttributeValues.ToAttributeMap());
            return qr;
        }

        public static QueryRequest KeyConditionExpression<T>(this QueryRequest qr, Expression<Func<T, bool>> expression)
        {
            var ddbExpression = FilterConditionExpressionVisitor.BuildExpression<T>(expression, AllowedOperations.KEY_CONDITIONS_ONLY);
            qr.KeyConditionExpression = ddbExpression.ExpressionStatement;
            qr.ExpressionAttributeNames.AddRange(ddbExpression.ExpressionAttributeNames);
            qr.ExpressionAttributeValues.AddRange(ddbExpression.ExpressionAttributeValues.ToAttributeMap());
            return qr;
        }
    }
}