using ExpressiveDynamoDB.ExpressionGeneration;
using NUnit.Framework;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System.Linq;

namespace ExpressiveDynamoDB.Test.FilterConditionExpressionVisitorTests
{
    public class BuildConditionsTests
    {
        
        public string AttributesToDocumentJson(List<AttributeValue> attributeValues)
        {
            var counter = 0;
            return Document.FromAttributeMap(attributeValues.ToDictionary(a => $"{counter++}", a => a)).ToJson();
        }

        [Test]
        [TestCaseSource(typeof(BuildConditionsTestCases), nameof(BuildConditionsTestCases.GetCases))]
        public void BuildConditions_TestCases(BuildConditionsTestCases testCase)
        {
            // Arrange

            // Act
            var output = FilterConditionExpressionVisitor.BuildConditions(testCase.Expression);

            // Assert
            Assert.AreEqual(testCase.ExpectedConditions.Count, output.Count);
            foreach (var kvp in output)
            {
                Assert.IsTrue(testCase.ExpectedConditions.ContainsKey(kvp.Key), $"{kvp.Key} was not found as an expected condition");
                var expectedCondition = testCase.ExpectedConditions[kvp.Key];
                var returnedCondition = kvp.Value;
                Assert.AreEqual(expectedCondition.ComparisonOperator, returnedCondition.ComparisonOperator);
                var expectedValues = expectedCondition.AttributeValueList;
                var receivedValues = returnedCondition.AttributeValueList;
                Assert.AreEqual(expectedValues.Count, receivedValues.Count, $"{kvp.Key} was expecting {expectedValues.Count} values");
                Assert.AreEqual(AttributesToDocumentJson(expectedValues), AttributesToDocumentJson(receivedValues));
            }
        }
    }
}