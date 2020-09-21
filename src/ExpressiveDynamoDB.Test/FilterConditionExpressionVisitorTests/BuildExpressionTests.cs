using Amazon.DynamoDBv2.DocumentModel;
using ExpressiveDynamoDB.ExpressionGeneration;
using NUnit.Framework;

namespace ExpressiveDynamoDB.Test.FilterConditionExpressionVisitorTests
{
    public class BuildExpressionTests
    {

        [Test]
        [TestCaseSource(typeof(BuildExpressionTestCases), nameof(BuildExpressionTestCases.GetCases))]
        public void BuildExpression_TestCases(BuildExpressionTestCases testCase)
        {
            // Arrange

            // Act
            var output = FilterConditionExpressionVisitor.BuildExpression(testCase.Expression);

            // Assert
            Assert.AreEqual(testCase.ExpectedExpression, output.ExpressionStatement);
            foreach (var kvp in output.ExpressionAttributeNames)
            {
                Assert.IsTrue(testCase.ExpectedExpressionAttributeNames.ContainsKey(kvp.Key), "missing ExpectedExpressionAttributeNames {0}, found {1}", kvp.Key, string.Join(", ", testCase.ExpectedExpressionAttributeNames.Keys));
                var expectedAttributeName = testCase.ExpectedExpressionAttributeNames[kvp.Key];
                Assert.AreEqual(expectedAttributeName, kvp.Value);
            }
            foreach (var kvp in output.ExpressionAttributeValues)
            {
                Assert.IsTrue(testCase.ExpectedExpressionAttributeValues.ContainsKey(kvp.Key), "missing ExpectedExpressionAttributeValues {0}, found {1}", kvp.Key, string.Join(", ", testCase.ExpectedExpressionAttributeValues.Keys));
                var expectedAttributeValue = testCase.ExpectedExpressionAttributeValues[kvp.Key];
                var stringGivenValue = (string)kvp.Value;
                if (stringGivenValue != null)
                {
                    Assert.AreEqual(expectedAttributeValue.AsString(), stringGivenValue);
                }
                else
                {
                    Assert.AreEqual(expectedAttributeValue, kvp.Value);
                }
            }
        }
    }
}