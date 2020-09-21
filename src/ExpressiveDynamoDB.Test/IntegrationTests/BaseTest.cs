using ExpressiveDynamoDB.Extensions;
using NUnit.Framework;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using System.Linq;
using Amazon.DynamoDBv2;
using System.Threading.Tasks;
using System;

namespace ExpressiveDynamoDB.Test.IntegrationTests
{
    public abstract class BaseTest
    {
        protected static AmazonDynamoDBClient DynamoDBClient = default!;
        protected static string TableName = "ExpressiveDynamoDBTests";

        static readonly Dictionary<string, AttributeValue>[] SeedData = Enumerable.Range(0, 10).Select(i => new Dictionary<string, AttributeValue> {
                { "pk", new AttributeValue($"ENTITY#{i.ToString("N4")}") },
                { "sk", new AttributeValue($"ENTITY#{i.ToString("N4")}") },
                { "name", new AttributeValue($"Name {i}") },
                { "age", new AttributeValue() { N = $"{i}" } }
            }
        ).ToArray();

        [OneTimeSetUp]
        public static async Task OneTimeSetUp()
        {
            var endpoint = await DockerHelper.StartDynamoDBAsync();
            DynamoDBClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                ServiceURL = endpoint
            });

            await DockerHelper.CreateTableIfNotExists(DynamoDBClient, TableName, new List<KeySchemaElement>{
                new KeySchemaElement {
                    AttributeName = "pk",
                    KeyType = KeyType.HASH
                },
                new KeySchemaElement {
                    AttributeName = "sk",
                    KeyType = KeyType.RANGE
                },
            }, new List<AttributeDefinition> {
                new AttributeDefinition {
                    AttributeName = "pk",
                    AttributeType = ScalarAttributeType.S
                },
                new AttributeDefinition {
                    AttributeName = "sk",
                    AttributeType = ScalarAttributeType.S
                },
            }, new ProvisionedThroughput(10, 10));
        }

        protected static async Task InsertDataAsync(Dictionary<string, AttributeValue>[] seedData)
        {
            foreach (var item in seedData)
            {
                await DynamoDBClient.PutItemAsync(TableName, item);
            }
        }

        public static Dictionary<string, AttributeValue>[] GenerateData(int count, Func<int, Dictionary<string, AttributeValue>> producer)
        {
            return Enumerable.Range(0, count).Select(i => producer(i)).ToArray();
        }
    }
}