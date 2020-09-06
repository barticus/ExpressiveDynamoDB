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
        public void BuildConditions_TestCases(ExpressionTestCase testCase)
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

        [Test]
        [TestCaseSource(typeof(ExpressionTestCase), nameof(ExpressionTestCase.GetCases))]
        public void BuildExpression_TestCases(ExpressionTestCase testCase)
        {
            if (string.IsNullOrWhiteSpace(testCase.ExpectedExpression))
            {
                Assert.Ignore();
                return;
            }
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
                Assert.AreEqual(expectedAttributeValue, kvp.Value);
            }
        }

        public class ExpressionTestCase
        {
            public Expression<Func<SampleEntity1, bool>> Expression { get; set; } = default!;
            public Dictionary<string, Condition> ExpectedConditions { get; set; } = default!;
            public string? ExpectedExpression { get; set; }
            public Dictionary<string, string> ExpectedExpressionAttributeNames { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, DynamoDBEntry> ExpectedExpressionAttributeValues { get; set; } = new Dictionary<string, DynamoDBEntry>();

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
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pString", "SAMPLEENTITY#myId"}
                    },
                    Expression = (a) => a.Id == "SAMPLEENTITY#myId",
                    ExpectedExpression = "#pk = :pString"
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
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id}
                    },
                    Expression = (a) => a.Id == id,
                    ExpectedExpression = "#pk = :id"
                })
                {
                    TestName = "EqualsExpression_String_PrimaryKeyMember"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "innerObject.pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))}
                    },
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#innerObjectpk", "innerObject.pk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id}
                    },
                    Expression = (a) => a.InnerObject!.Id == id,
                    ExpectedExpression = "#innerObjectpk = :id"
                })
                {
                    TestName = "EqualsExpression_NestedType"
                };

                var name = "SAMPLEENTITY#myName";
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))}
                    },
                    Expression = (a) => a.Id == id && a.Name == name,
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"},
                        { "#sk", "sk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id},
                        { ":name", name}
                    },
                    ExpectedExpression = "#pk = :id AND #sk = :name"
                })
                {
                    TestName = "EqualsExpression_String_CompositePrimaryKeyMember"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.BEGINS_WITH, new AttributeValue(name))}
                    },
                    Expression = (a) => a.Name.StartsWith(name),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#sk", "sk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":name", name}
                    },
                    ExpectedExpression = "begins_with(#sk, :name)"
                })
                {
                    TestName = "StartsWithExpression_String"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.CONTAINS, new AttributeValue(name))}
                    },
                    Expression = (a) => a.Name.Contains(name),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#sk", "sk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":name", name}
                    },
                    ExpectedExpression = "contains(#sk, :name)"
                })
                {
                    TestName = "ContainsExpression_String"
                };

                var lowerBounds = "c";
                var upperBounds = "m";
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue(lowerBounds), new AttributeValue(upperBounds))}
                    },
                    Expression = (a) => a.Id == id && Functions.Between(a.Name, lowerBounds, upperBounds),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"},
                        { "#sk", "sk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id},
                        { ":lowerBounds", lowerBounds},
                        { ":upperBounds", upperBounds}
                    },
                    ExpectedExpression = "#pk = :id AND #sk BETWEEN :lowerBounds AND :upperBounds"
                })
                {
                    TestName = "BetweenExpression_String"
                };

                var middleBounds = "a";
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue(middleBounds))}
                    },
                    Expression = (a) => Functions.Between(a.Name, middleBounds, middleBounds),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#sk", "sk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":middleBounds", middleBounds}
                    },
                    ExpectedExpression = "#sk BETWEEN :middleBounds AND :middleBounds"
                })
                {
                    TestName = "BetweenExpression_String_Middle"
                };

                var lowerIntBounds = 5;
                var upperIntBounds = 10;
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "age", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue { N = lowerIntBounds.ToString() }, new AttributeValue{ N = upperIntBounds.ToString() })}
                    },
                    Expression = (a) => Functions.Between(a.Age, lowerIntBounds, upperIntBounds),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#age", "age"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":lowerIntBounds", lowerIntBounds},
                        { ":upperIntBounds", upperIntBounds}
                    },
                    ExpectedExpression = "#age BETWEEN :lowerIntBounds AND :upperIntBounds"
                })
                {
                    TestName = "BetweenExpression_Integer"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))}
                    },
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"},
                        { "#sk", "sk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id},
                        { ":name", name}
                    },
                    Expression = (a) => a.Id == id || a.Name == name,
                    ExpectedExpression = "#pk = :id OR #sk = :name"
                })
                {
                    TestName = "EqualsExpression_String_OrCondition"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))},
                        { "age", ConditionFrom(ComparisonOperator.EQ, new AttributeValue { N = lowerIntBounds.ToString() })}
                    },
                    Expression = (a) => a.Id == id && (a.Name == name || a.Age == lowerIntBounds),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"},
                        { "#sk", "sk"},
                        { "#age", "age"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id},
                        { ":name", name},
                        { ":lowerIntBounds", lowerIntBounds}
                    },
                    ExpectedExpression = "#pk = :id AND (#sk = :name OR #age = :lowerIntBounds)"
                })
                {
                    TestName = "EqualsExpression_String_MultipleConditions"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))},
                        { "age", ConditionFrom(ComparisonOperator.EQ, new AttributeValue { N = lowerIntBounds.ToString() })}
                    },
                    Expression = (a) => a.Id == id && a.Name == name || a.Age == lowerIntBounds,
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"},
                        { "#sk", "sk"},
                        { "#age", "age"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id},
                        { ":name", name},
                        { ":lowerIntBounds", lowerIntBounds}
                    },
                    ExpectedExpression = "(#pk = :id AND #sk = :name) OR #age = :lowerIntBounds"
                })
                {
                    TestName = "EqualsExpression_String_MultipleConditions2"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "stringArray", ConditionFrom(ComparisonOperator.CONTAINS, new AttributeValue("test"))}
                    },
                    Expression = (a) => a.StringArray.Contains("test"),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#stringArray", "stringArray"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pString", "test"}
                    },
                    ExpectedExpression = "contains(#stringArray, :pString)"
                })
                {
                    TestName = "ContainsExpression_StringArray"
                };

                var arr1 = new[] { "string1", "string2" };
                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.IN, new AttributeValue { SS = arr1.ToList() })}
                    },
                    Expression = (a) => arr1.Contains(a.Name),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#sk", "sk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":arr1", arr1}
                    },
                    ExpectedExpression = "#sk IN (:arr1)"
                })
                {
                    TestName = "InExpression_StringArray"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "intArray", ConditionFrom(ComparisonOperator.CONTAINS, new AttributeValue { N = "60" })}
                    },
                    Expression = (a) => a.IntArray.Contains(60),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#intArray", "intArray"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pInt32", 60}
                    },
                    ExpectedExpression = "contains(#intArray, :pInt32)"
                })
                {
                    TestName = "ContainsExpression_IntArray"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "intArray", ConditionFrom(ComparisonOperator.NOT_NULL)}
                    },
                    Expression = (a) => Functions.AttributeExists(a.IntArray),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#intArray", "intArray"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pInt32", 60}
                    },
                    ExpectedExpression = "attribute_exists(#intArray)"
                })
                {
                    TestName = "AttributeExistsExpression_IntArray"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "innerObject.pk", ConditionFrom(ComparisonOperator.NOT_NULL)}
                    },
                    Expression = (a) => Functions.AttributeExists(a.InnerObject!.Id),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#innerObjectpk", "innerObject.pk"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pInt32", 60}
                    },
                    ExpectedExpression = "attribute_exists(#innerObjectpk)"
                })
                {
                    TestName = "AttributeExistsExpression_NestedType"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                        { "intArray", ConditionFrom(ComparisonOperator.NULL)}
                    },
                    Expression = (a) => Functions.AttributeNotExists(a.IntArray),
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#intArray", "intArray"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pInt32", 60}
                    },
                    ExpectedExpression = "attribute_not_exists(#intArray)"
                })
                {
                    TestName = "AttributeExistsExpression_IntArray"
                };

                yield return new TestCaseData(new ExpressionTestCase
                {
                    ExpectedConditions = new Dictionary<string, Condition> {
                    },
                    Expression = (a) => Functions.Size(a.IntArray) > 3,
                    ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#intArray", "intArray"}
                    },
                    ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pInt32", 3}
                    },
                    ExpectedExpression = "size(#intArray) > :pInt32"
                })
                {
                    TestName = "SizeExpression_IntArray"
                };

            }
        }
    }
}