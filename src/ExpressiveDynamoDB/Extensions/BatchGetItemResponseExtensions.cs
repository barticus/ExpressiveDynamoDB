using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace ExpressiveDynamoDB.Extensions
{
    public static class BatchGetItemResponseExtensions
    {
        public static T[] Items<T>(this BatchGetItemResponse itemResponse, EntityMapper entityMapper) where T : class
        {
            var items = new List<T>();
            foreach(var response in itemResponse.Responses)
            {
                items.AddRange(response.Value.Select(i => entityMapper.FromDocument<T>(Document.FromAttributeMap(i))));
            }
            return items.ToArray();
        }
    }
}