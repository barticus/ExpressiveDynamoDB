using System;
using System.Linq.Expressions;
using NUnit.Framework;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using System.Linq;
using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.Test.FilterConditionExpressionVisitorTests
{
    public class BuildConditionsTestCases
    {
        public Expression<Func<SampleEntity1, bool>> Expression { get; set; } = default!;
        public Dictionary<string, Condition> ExpectedConditions { get; set; } = default!;

        private static Condition ConditionFrom(ComparisonOperator comparisonOperator, params AttributeValue[] attributeValues)
        {
            var condition = new Condition();
            condition.ComparisonOperator = comparisonOperator;
            condition.AttributeValueList = attributeValues.ToList();
            return condition;
        }

        public static IEnumerable<TestCaseData> GetCases()
        {
            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue("SAMPLEENTITY#myId"))}
                    },
                Expression = (a) => a.Id == "SAMPLEENTITY#myId",
            })
            {
                TestName = "EqualsExpression_String_PrimaryKeyConstant"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue("SAMPLEENTITY#myId"))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue("SAMPLEENTITY#myName"))}
                    },
                Expression = (a) => a.Id == "SAMPLEENTITY#myId" && a.Name == "SAMPLEENTITY#myName",
            })
            {
                TestName = "EqualsExpression_String_CompositeKeyConstant"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue("SAMPLEENTITY#myId"))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue("SAMPLEENTITY#myId"))}
                    },
                Expression = (a) => a.Id == "SAMPLEENTITY#myId" && a.Name == "SAMPLEENTITY#myId",
            })
            {
                TestName = "EqualsExpression_String_CompositeKeyConstant_SameKey"
            };

            var id = "SAMPLEENTITY#myId";
            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))}
                    },
                Expression = (a) => a.Id == id,
            })
            {
                TestName = "EqualsExpression_String_PrimaryKeyMember"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "innerObject.pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))}
                    },
                Expression = (a) => a.InnerObject!.Id == id,
            })
            {
                TestName = "EqualsExpression_NestedType"
            };

            var name = "SAMPLEENTITY#myName";
            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))}
                    },
                Expression = (a) => a.Id == id && a.Name == name,
            })
            {
                TestName = "EqualsExpression_String_CompositePrimaryKeyMember"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.BEGINS_WITH, new AttributeValue(name))}
                    },
                Expression = (a) => a.Name.StartsWith(name),
            })
            {
                TestName = "StartsWithExpression_String"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.CONTAINS, new AttributeValue(name))}
                    },
                Expression = (a) => a.Name.Contains(name),
            })
            {
                TestName = "ContainsExpression_String"
            };

            var lowerBounds = "c";
            var upperBounds = "m";
            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue(lowerBounds), new AttributeValue(upperBounds))}
                    },
                Expression = (a) => a.Id == id && Functions.Between(a.Name, lowerBounds, upperBounds),
            })
            {
                TestName = "BetweenExpression_String"
            };

            var middleBounds = "a";
            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue(middleBounds))}
                    },
                Expression = (a) => Functions.Between(a.Name, middleBounds, middleBounds),
            })
            {
                TestName = "BetweenExpression_String_Middle"
            };

            var lowerIntBounds = 5;
            var upperIntBounds = 10;
            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "age", ConditionFrom(ComparisonOperator.BETWEEN, new AttributeValue { N = lowerIntBounds.ToString() }, new AttributeValue{ N = upperIntBounds.ToString() })}
                    },
                Expression = (a) => Functions.Between(a.Age, lowerIntBounds, upperIntBounds),
            })
            {
                TestName = "BetweenExpression_Integer"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))}
                    },
                Expression = (a) => a.Id == id || a.Name == name,
            })
            {
                TestName = "EqualsExpression_String_OrCondition"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))},
                        { "age", ConditionFrom(ComparisonOperator.EQ, new AttributeValue { N = lowerIntBounds.ToString() })}
                    },
                Expression = (a) => a.Id == id && (a.Name == name || a.Age == lowerIntBounds),
            })
            {
                TestName = "EqualsExpression_String_MultipleConditions"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "pk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(id))},
                        { "sk", ConditionFrom(ComparisonOperator.EQ, new AttributeValue(name))},
                        { "age", ConditionFrom(ComparisonOperator.EQ, new AttributeValue { N = lowerIntBounds.ToString() })}
                    },
                Expression = (a) => a.Id == id && a.Name == name || a.Age == lowerIntBounds,
            })
            {
                TestName = "EqualsExpression_String_MultipleConditions2"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "stringArray", ConditionFrom(ComparisonOperator.CONTAINS, new AttributeValue("test"))}
                    },
                Expression = (a) => a.StringArray.Contains("test"),
            })
            {
                TestName = "ContainsExpression_StringArray"
            };

            var arr1 = new[] { "string1", "string2" };
            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "sk", ConditionFrom(ComparisonOperator.IN, new AttributeValue { SS = arr1.ToList() })}
                    },
                Expression = (a) => arr1.Contains(a.Name),
            })
            {
                TestName = "InExpression_StringArray"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "intArray", ConditionFrom(ComparisonOperator.CONTAINS, new AttributeValue { N = "60" })}
                    },
                Expression = (a) => a.IntArray.Contains(60),
            })
            {
                TestName = "ContainsExpression_IntArray"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "intArray", ConditionFrom(ComparisonOperator.NOT_NULL)}
                    },
                Expression = (a) => Functions.AttributeExists(a.IntArray),
            })
            {
                TestName = "AttributeExistsExpression_IntArray"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "innerObject.pk", ConditionFrom(ComparisonOperator.NOT_NULL)}
                    },
                Expression = (a) => Functions.AttributeExists(a.InnerObject!.Id),
            })
            {
                TestName = "AttributeExistsExpression_NestedType"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition> {
                        { "intArray", ConditionFrom(ComparisonOperator.NULL)}
                    },
                Expression = (a) => Functions.AttributeNotExists(a.IntArray),
            })
            {
                TestName = "AttributeExistsExpression_IntArray"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition>
                {
                },
                Expression = (a) => Functions.Size(a.IntArray) > 3,
            })
            {
                TestName = "SizeExpression_IntArray"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition>
                {
                },
                Expression = (a) => a.IntArray.Count() > 3,
            })
            {
                TestName = "EnumerableCountExpression_IntArray"
            };

            yield return new TestCaseData(new BuildConditionsTestCases
            {
                ExpectedConditions = new Dictionary<string, Condition>
                {
                },
                Expression = (a) => Functions.AttributeType(a.IntArray, AttributeType.NS),
            })
            {
                TestName = "AttributeTypeExpression_IntArray"
            };

        }
    }
}