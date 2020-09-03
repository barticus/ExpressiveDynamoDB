using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Ddb = Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Extensions
{
    public static class DynamoDBEntryExtensions
    {
        public static Dictionary<string, AttributeValue> ToAttributeMap(this Dictionary<string, Ddb.DynamoDBEntry> entries)
        {
            return new Ddb.Document(entries).ToAttributeMap();
        }
    }
}