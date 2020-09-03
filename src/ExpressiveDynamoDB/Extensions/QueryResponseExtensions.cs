using System.Linq;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace ExpressiveDynamoDB.Extensions
{
    public static class QueryResponseExtensions
    {
        public static T[] Items<T>(this QueryResponse qr, IEntityMapper entityMapper)
        {
            return qr.Items.Select(i => entityMapper.FromDocument<T>(Document.FromAttributeMap(i))).ToArray();
        }
    }
}