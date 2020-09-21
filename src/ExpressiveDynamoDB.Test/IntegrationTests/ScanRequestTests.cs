using ExpressiveDynamoDB.Extensions;
using NUnit.Framework;
using Amazon.DynamoDBv2.Model;
using System.Threading.Tasks;
using System;

namespace ExpressiveDynamoDB.Test.IntegrationTests
{
    public class ScanRequestTests: BaseTest
    {
        [Test]
        [TestCaseSource(typeof(ScanRequestTestCases), nameof(ScanRequestTestCases.GetCases))]
        public async Task BuildExpression_TestCases(ScanRequestTestCases testCase)
        {
            // Arrange
            await InsertDataAsync(testCase.SeedData);
            Console.WriteLine("Seeded: " + System.Text.Json.JsonSerializer.Serialize(testCase.SeedData));
            var scanRequest = new ScanRequest(TableName).SetFilterExpression<SampleEntity1>(testCase.Expression);
            
            // Act
            var output = await DynamoDBClient.ScanAsync(scanRequest);
            Console.WriteLine("Found: " + System.Text.Json.JsonSerializer.Serialize(output.Items));

            // Assert
            Assert.AreEqual(testCase.ExpectedResultCount, output.Count);
        }
    }
}