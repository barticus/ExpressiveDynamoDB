using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using ExpressiveDynamoDB.Extensions;
using ExpressiveDynamoDB.FieldTransformers;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB
{
    public class KeySchema
    {
        public string PartitionKey { get; }
        public string? SortKey { get; }
        public string? IndexName { get; }

        private List<IFieldTransformer> PartitionKeyTransformers = new List<IFieldTransformer>();
        private List<IFieldTransformer> SortKeyTransformers = new List<IFieldTransformer>();

        public KeySchema(string partitionKey, string? sortKey = null, string? indexName = null)
        {
            PartitionKey = partitionKey;
            SortKey = sortKey;
            IndexName = indexName;
        }

        public KeySchema AddPartitionKeyTransformer(IFieldTransformer transformer)
        {
            PartitionKeyTransformers.Add(transformer);
            return this;
        }

        public KeySchema AddSortKeyTransformer(IFieldTransformer transformer)
        {
            SortKeyTransformers.Add(transformer);
            return this;
        }

        public DynamoDBEntry TransformPartitionKey(DynamoDBEntry partitionKey)
        {
            foreach(var transformer in PartitionKeyTransformers)
            {
                partitionKey = transformer.Transform(partitionKey);
            }
            return partitionKey;
        }

        public DynamoDBEntry TransformSortKey(DynamoDBEntry sortKey)
        {
            foreach(var transformer in SortKeyTransformers)
            {
                sortKey = transformer.Transform(sortKey);
            }
            return sortKey;
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