using NUnit.Framework;

namespace ExpressiveDynamoDB.Test
{
    public class KeySchemaTests
    {
        [Test]
        public void Test1()
        {
            // Arrange

            // Act
            var result = KeySchema.From<SampleEntity1>(e => e.Id, e => e.Name);

            // Assert
            Assert.AreEqual("pk", result.PartitionKey);
            Assert.AreEqual("sk", result.SortKey);
        }

        [Test]
        public void Test2()
        {
            // Arrange

            // Act
            var result = KeySchema.From<SampleEntity1>(e => e.Id);

            // Assert
            Assert.AreEqual("pk", result.PartitionKey);
        }
    }
}