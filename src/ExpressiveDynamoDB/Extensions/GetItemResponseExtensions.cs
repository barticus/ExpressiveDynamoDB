using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace ExpressiveDynamoDB.Extensions
{
    public static class GetItemResponseExtensions
    {
        public static T? As<T>(this GetItemResponse itemResponse, EntityMapper entityMapper) where T : class
        {
            if (!itemResponse.IsItemSet)
            {
                return null;
            }
            return entityMapper.FromDocument<T>(Document.FromAttributeMap(itemResponse.Item));
        }
    }
}