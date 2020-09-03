using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
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

        public Dictionary<string, AttributeValue> AttributeKeyFrom(Primitive partitionKeyValue, Primitive? sortKey = null)
        {
            var values = new Dictionary<string, DynamoDBEntry> {
                { PartitionKey, partitionKeyValue }
            };
            if(!string.IsNullOrWhiteSpace(SortKey) && string.IsNullOrWhiteSpace(sortKey))
            {
                throw new ArgumentNullException(nameof(sortKey));
            }
            if(string.IsNullOrWhiteSpace(SortKey) && !string.IsNullOrWhiteSpace(sortKey))
            {
                throw new ArgumentNullException(nameof(SortKey));
            }
            if(!string.IsNullOrWhiteSpace(SortKey) && !string.IsNullOrWhiteSpace(sortKey))
            {
                values.Add(SortKey, sortKey);
            }
            return new Document(values).ToAttributeMap();
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

            return propertyInfo.DynamoDBAttributeName();
        }
    }
}