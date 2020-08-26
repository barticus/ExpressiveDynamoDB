using ExpressiveDynamoDB.ExpressionGeneration;
using System;
using System.Linq.Expressions;
using NUnit.Framework;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System.Linq;
using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.Test
{
    public class FilterConditionExpressionVisitorTests
    {
        public string AttributesToDocumentJson(List<AttributeValue> attributeValues)
        {
            var counter = 0;
            return Document.FromAttributeMap(attributeValues.ToDictionary(a => $"{counter++}", a => a)).ToJson();
        }

        [Test]
        [TestCaseSource(typeof(ExpressionTestCase), nameof(ExpressionTestCase.GetCases))]
        public void BuildStatement_TestCases(ExpressionTestCase testCase)
        {
            // Arrange

            // Act
            var output = FilterConditionExpressionVisitor.BuildStatement(testCase.Expression);

            // Assert
            Assert.AreEqual(testCase.ExpectedConditions.Count, output.Count);
            foreach (var kvp in output)
            {
                Assert.IsTrue(testCase.ExpectedConditions.ContainsKey(kvp.Key));
                var expectedCondition = kvp.Value;
                var returnedCondition = testCase.ExpectedConditions[kvp.Key];
                Assert.AreEqual(expectedCondition.ComparisonOperator, returnedCondition.ComparisonOperator);
                var expectedValues = expectedCondition.AttributeValueList;
                var receivedValues = returnedCondition.AttributeValueList;
                Assert.AreEqual(expectedValues.Count, receivedValues.Count);
                Assert.AreEqual(AttributesToDocumentJson(expectedValues), AttributesToDocumentJson(receivedValues));
            }
        }

        public class ExpressionTestCase
        {
            public Expression<Func<SampleEntity1, bool>> Expression { get; set; }
            public Dictionary<string, Condition> ExpectedConditions { get; set; }

            private static Condition ConditionFrom(ComparisonOperator comparisonOperator, params AttributeValue[] attributeValues)
            {
                var condition = new Condition();
                condition.ComparisonOperator = comparisonOperator;
                condition.AttributeValueList = attributeValues.ToList();
                return condition;
            }
            public static IEnumerable<TestCaseData> GetCases()
            {
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue("SAMPLEENTITY#myId"))}
                    },
                    Expression = (a) => a.Id == "SAMPLEENTITY#myId"
                })
                {
                    TestName = "EqualsExpression_String_PrimaryKeyConstant"
                };

                var id = "SAMPLEENTITY#myId";
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))}
                    },
                    Expression = (a) => a.Id == id
                })
                {
                    TestName = "EqualsExpression_String_PrimaryKeyMember"
                };

                var name = "SAMPLEENTITY#myName";
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))}
                    },
                    Expression = (a) => a.Id == id && a.Name == name
                })
                {
                    TestName = "EqualsExpression_String_CompositePrimaryKeyMember"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.BEGINS_WITH, new AttributeValue(name))}
                    },
                    Expression = (a) => a.Id == id && a.Name.StartsWith(name)
                })
                {
                    TestName = "StartsWithExpression_String_CompositePrimaryKeyMember"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.CONTAINS, new AttributeValue(name))}
                    },
                    Expression = (a) => a.Id == id && a.Name.Contains(name)
                })
                {
                    TestName = "ContainsExpression_String_CompositePrimaryKeyMember"
                };

                var lowerBounds = "c";
                var upperBounds = "m";
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue(lowerBounds), new AttributeValue(upperBounds))}
                    },
                    Expression = (a) => a.Id == id && Functions.Between(a.Name, lowerBounds, upperBounds)
                })
                {
                    TestName = "BetweenExpression_String_CompositePrimaryKeyMembers"
                };

                var middleBounds = "a";
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue(middleBounds))}
                    },
                    Expression = (a) => a.Id == id && Functions.Between(a.Name, middleBounds, middleBounds)
                })
                {
                    TestName = "BetweenExpression_String_CompositePrimaryKeyMember"
                };

                var lowerIntBounds = 5;
                var upperIntBounds = 10;
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "age", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue { N = lowerIntBounds.ToString() }, new AttributeValue{ N = upperIntBounds.ToString() })}
                    },
                    Expression = (a) => a.Id == id && Functions.Between(a.Age, lowerIntBounds, upperIntBounds)
                })
                {
                    TestName = "BetweenExpression_Integer_CompositePrimaryKeyMembers"
                };

                // var arr1 = new [] { "test1", "test2" };
                // yield return new TestCaseData(new ExpressionTestCase
                // {
                //     ExpectedConditions = new Dictionary<string, Condition> {
                //         { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                //         { "age", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue { N = lowerIntBounds.ToString() }, new AttributeValue{ N = upperIntBounds.ToString() })}
                //     },
                //     Expression = (a) => a.Id == id && arr1.Contains("test")
                // })
                // {
                //     TestName = "ContainsExpression_StringArray"
                // };

            }
        }
    }
}