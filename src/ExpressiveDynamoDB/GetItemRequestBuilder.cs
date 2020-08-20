using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using ExpressiveDynamoDB.FieldTransformers;

namespace ExpressiveDynamoDB
{
    public class GetItemRequestBuilder
    {
        private GetItemRequest GetItemRequest { get; set; } = new GetItemRequest();
        public KeySchema KeySchema { get; private set; }

        public GetItemRequestBuilder(KeySchema keySchema)
        {
            KeySchema = keySchema;
        }

        public GetItemRequestBuilder WithKey<T>(string partitionKey, string? sortKey = null)
        {
            var key = new Dictionary<string, AttributeValue>();
            var partitionKeyAttributeName = ExpressionAttributeHelpers.GetAttributeKeyName(KeySchema.PartitionKey, GetItemRequest.ExpressionAttributeNames);
            partitionKey = FieldTransformerHelpers.ApplyAllFrom<T, PartitionKeyTransformerAttribute>(partitionKey);
            key.Add(partitionKeyAttributeName, new AttributeValue(partitionKey));
            GetItemRequest.ExpressionAttributeNames.Add(partitionKeyAttributeName, KeySchema.PartitionKey);

            if (sortKey != null && KeySchema.SortKey != null)
            {
                var sortKeyAttributeName = ExpressionAttributeHelpers.GetAttributeKeyName(KeySchema.SortKey, GetItemRequest.ExpressionAttributeNames);
                sortKey = FieldTransformerHelpers.ApplyAllFrom<T, SortKeyTransformerAttribute>(sortKey);
                key.Add(sortKeyAttributeName, new AttributeValue(sortKey));
                GetItemRequest.ExpressionAttributeNames.Add(sortKeyAttributeName, KeySchema.SortKey);
            }
            GetItemRequest.Key = key;
            return this;
        }

        public GetItemRequest Build()
        {
            return GetItemRequest;
        }
    }
}