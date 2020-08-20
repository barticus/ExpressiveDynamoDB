using System.Linq;
using NUnit.Framework;

namespace ExpressiveDynamoDB.Test
{
    public class GetItemRequestBuilderTests
    {
        [Test]
        public void Test1()
        {
            // Arrange
            var schema = new KeySchema("pk", "sk");
            var girb = new GetItemRequestBuilder(schema);

            // Act
            var result = girb.WithKey<SampleEntity2>("partition", "sort").Build();

            // Assert
            Assert.AreEqual(2, result.Key.Count());
            Assert.AreEqual("#pk", result.Key.First().Key);
            Assert.AreEqual("PARTITION#partition", result.Key.First().Value.S);
            Assert.AreEqual("#sk", result.Key.Last().Key);
            Assert.AreEqual("SORT#sort", result.Key.Last().Value.S);
            Assert.AreEqual(2, result.ExpressionAttributeNames.Count());
            Assert.AreEqual("#pk", result.ExpressionAttributeNames.First().Key);
            Assert.AreEqual("pk", result.ExpressionAttributeNames.First().Value);
            Assert.AreEqual("#sk", result.ExpressionAttributeNames.Last().Key);
            Assert.AreEqual("sk", result.ExpressionAttributeNames.Last().Value);
        }
    }
}