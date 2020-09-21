using System;
using System.Linq.Expressions;
using NUnit.Framework;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System.Linq;
using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.Test.FilterConditionExpressionVisitorTests
{
    public class BuildExpressionTestCases
    {
        public Expression<Func<SampleEntity1, bool>> Expression { get; set; } = default!;
        public string ExpectedExpression { get; set; } = default!;
        public Dictionary<string, string> ExpectedExpressionAttributeNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, DynamoDBEntry> ExpectedExpressionAttributeValues { get; set; } = new Dictionary<string, DynamoDBEntry>();

        public static IEnumerable<TestCaseData> GetCases()
        {
            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
                ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"},
                        { "#sk", "sk"}
                    },
                ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pString", "SAMPLEENTITY#myId"},
                        { ":pString2", "SAMPLEENTITY#myName"}
                    },
                Expression = (a) => a.Id == "SAMPLEENTITY#myId" && a.Name == "SAMPLEENTITY#myName",
                ExpectedExpression = "#pk = :pString AND #sk = :pString2"
            })
            {
                TestName = "EqualsExpression_String_CompositeKeyConstant"
            };

            yield return new TestCaseData(new BuildExpressionTestCases
            {
                ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#pk", "pk"},
                        { "#sk", "sk"}
                    },
                ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pString", "SAMPLEENTITY#myId"}
                    },
                Expression = (a) => a.Id == "SAMPLEENTITY#myId" && a.Name == "SAMPLEENTITY#myId",
                ExpectedExpression = "#pk = :pString AND #sk = :pString"
            })
            {
                TestName = "EqualsExpression_String_CompositeKeyConstant_SameKey"
            };

            var id = "SAMPLEENTITY#myId";
            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
                ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#innerObject", "innerObject"},
                        { "#pk", "pk"}
                    },
                ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":id", id}
                    },
                Expression = (a) => a.InnerObject!.Id == id,
                ExpectedExpression = "#innerObject.#pk = :id"
            })
            {
                TestName = "EqualsExpression_NestedType"
            };

            var name = "SAMPLEENTITY#myName";
            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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
            yield return new TestCaseData(new BuildExpressionTestCases
            {
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
            yield return new TestCaseData(new BuildExpressionTestCases
            {
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
            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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
            yield return new TestCaseData(new BuildExpressionTestCases
            {
                Expression = (a) => arr1.Contains(a.Name),
                ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#sk", "sk"}
                    },
                ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":arr1_0", arr1[0]},
                        { ":arr1_1", arr1[1]}
                    },
                ExpectedExpression = "#sk IN (:arr1_0, :arr1_1)"
            })
            {
                TestName = "InExpression_StringArray"
            };

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
                Expression = (a) => Functions.AttributeExists(a.InnerObject!.Id),
                ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#innerObject", "innerObject"},
                        { "#pk", "pk"}
                    },
                ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pInt32", 60}
                    },
                ExpectedExpression = "attribute_exists(#innerObject.#pk)"
            })
            {
                TestName = "AttributeExistsExpression_NestedType"
            };

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
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

            yield return new TestCaseData(new BuildExpressionTestCases
            {
                Expression = (a) => a.IntArray.Count() > 3,
                ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#intArray", "intArray"}
                    },
                ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pInt32", 3}
                    },
                ExpectedExpression = "size(#intArray) > :pInt32"
            })
            {
                TestName = "EnumerableCountExpression_IntArray"
            };

            yield return new TestCaseData(new BuildExpressionTestCases
            {
                Expression = (a) => Functions.AttributeType(a.IntArray, AttributeType.NS),
                ExpectedExpressionAttributeNames = new Dictionary<string, string> {
                        { "#intArray", "intArray"}
                    },
                ExpectedExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        { ":pAttributeType", "NS"}
                    },
                ExpectedExpression = "attribute_type(#intArray, :pAttributeType)"
            })
            {
                TestName = "AttributeTypeExpression_IntArray"
            };

        }
    }
}