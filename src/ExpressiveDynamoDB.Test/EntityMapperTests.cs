using NUnit.Framework;
using ExpressiveDynamoDB;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.DataModel;
using System.Collections.Generic;

namespace ExpressiveDynamoDB.Test
{

    public class EntityMapperTests
    {
        private EntityMapper EntityMapper() => new EntityMapper(new DynamoDBContext(new AmazonDynamoDBClient()));
        
        [Test]
        public void Test1()
        {
            // Arrange
            var entityMapper = EntityMapper();
            var input = new SampleEntity1{
                Id = "test id",
                Name = "hello there!"
            };

            // Act
            var result = entityMapper.ToDocument(input);

            // Assert
            var attributeMap = result.ToAttributeMap();
            Assert.AreEqual(2, attributeMap.Count);
            Assert.IsTrue(attributeMap.ContainsKey("pk"));
            Assert.IsTrue(attributeMap.ContainsKey("sk"));
            Assert.AreEqual(input.Id, attributeMap["pk"].S);
            Assert.AreEqual(input.Name, attributeMap["sk"].S);
        }

        [Test]
        public void Test2()
        {
            // Arrange
            var entityMapper = EntityMapper();
            var attributeMap = new Dictionary<string, AttributeValue> {
                { "pk", new AttributeValue("test id")},
                { "sk", new AttributeValue("hello there!")}
            };
            var input = Document.FromAttributeMap(attributeMap);

            // Act
            var result = entityMapper.FromDocument<SampleEntity1>(input);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Id, attributeMap["pk"].S);
            Assert.AreEqual(result.Name, attributeMap["sk"].S);
        }
    }
}