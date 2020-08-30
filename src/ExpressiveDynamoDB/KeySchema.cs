using System;
using System.Linq.Expressions;
using System.Reflection;
using ExpressiveDynamoDB.Extensions;

namespace ExpressiveDynamoDB
{
    public class KeySchema
    {
        public string PartitionKey { get; }
        public string? SortKey { get; }
        public string? IndexName { get; }

        public KeySchema(string partitionKey, string? sortKey = null, string? indexName = null)
        {
            PartitionKey = partitionKey;
            SortKey = sortKey;
            IndexName = indexName;
        }

        public static KeySchema From<T>(
            Expression<Func<T, object>> partitionKeyExpression,
            string? indexName = null
        )
        {
            return new KeySchema(PropertyNameFromExpression(partitionKeyExpression), indexName: indexName);
        }

        public static KeySchema From<T>(
            Expression<Func<T, object>> partitionKeyExpression,
            Expression<Func<T, object>> sortKeyExpression,
            string? indexName = null
        )
        {
            return new KeySchema(
                PropertyNameFromExpression(partitionKeyExpression),
                PropertyNameFromExpression(sortKeyExpression),
                indexName
            );
        }

        private static string PropertyNameFromExpression<T, TProp>(
            Expression<Func<T, TProp>> propertyExpression
        )
        {
            var propertyInfo = (propertyExpression.Body as MemberExpression)?.Member as PropertyInfo;

            if (propertyInfo is null)
                throw new InvalidOperationException("Please provide a valid property expression.");

            return propertyInfo.DynamoDbAttributeName();
        }
    }
}