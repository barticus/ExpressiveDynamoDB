using System;
using System.Linq.Expressions;
using NUnit.Framework;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using System.Linq;

namespace ExpressiveDynamoDB.Test.IntegrationTests
{
    public class ScanRequestTestCases
    {
        public Expression<Func<SampleEntity1, bool>> Expression { get; set; } = default!;
        public int ExpectedResultCount { get; set; } = 0;
        public Dictionary<string, AttributeValue>[] SeedData { get; set; } = new Dictionary<string, AttributeValue>[0];

        public static IEnumerable<TestCaseData> GetCases()
        {
            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0001",
                SeedData = BaseTest.GenerateData(3, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue($"ENTITY#0001") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                }),
                ExpectedResultCount = 3
            })
            {
                TestName = "EqualsExpression_String_PrimaryKeyConstant"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0002" && a.Name == "ITEM#0000",
                SeedData = BaseTest.GenerateData(1, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue($"ENTITY#0002") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                }),
                ExpectedResultCount = 1
            })
            {
                TestName = "EqualsExpression_String_CompositeKeyConstant"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0003" && a.Name == "ENTITY#0003",
                SeedData = BaseTest.GenerateData(1, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue($"ENTITY#0003") },
                    { "sk", new AttributeValue($"ENTITY#0003") },
                }),
                ExpectedResultCount = 1
            })
            {
                TestName = "EqualsExpression_String_CompositeKeyConstant_SameKey"
            };

            var id = "ENTITY#0004";
            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == id,
                SeedData = BaseTest.GenerateData(2, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue(id) },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                }),
                ExpectedResultCount = 2
            })
            {
                TestName = "EqualsExpression_String_PrimaryKeyMember"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0005" && a.InnerObject!.Id == "INNERENTITY#0005",
                SeedData = BaseTest.GenerateData(2, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0005") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    { "innerObject", new AttributeValue(){ M = new Dictionary<string, AttributeValue> {
                        { "pk", new AttributeValue("INNERENTITY#0005") },
                    }} },
                }),
                ExpectedResultCount = 2
            })
            {
                TestName = "EqualsExpression_NestedType"
            };

            var namePartial = "NAME#";
            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Name.StartsWith(namePartial),
                SeedData = BaseTest.GenerateData(2, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0006") },
                    { "sk", new AttributeValue($"NAME#{i.ToString("D4")}") }
                }),
                ExpectedResultCount = 2
            })
            {
                TestName = "StartsWithExpression_String"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0007" && a.Name.Contains(namePartial),
                SeedData = BaseTest.GenerateData(2, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0007") },
                    { "sk", new AttributeValue($"PREFIX#NAME#{i.ToString("D4")}") }
                }),
                ExpectedResultCount = 2
            })
            {
                TestName = "ContainsExpression_String"
            };

            var lowerBounds = "C";
            var upperBounds = "F";
            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0008" && Functions.Between(a.Name, lowerBounds, upperBounds),
                SeedData = BaseTest.GenerateData(10, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0008") },
                    { "sk", new AttributeValue($"{(char)(i + 65)}#NAME") }
                }),
                ExpectedResultCount = 3
            })
            {
                TestName = "BetweenExpression_String"
            };

            var middleBounds = "a";
            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0009" && Functions.Between(a.Name, middleBounds, middleBounds),
                SeedData = BaseTest.GenerateData(5, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0009") },
                    { "sk", new AttributeValue($"{(char)(i + 65)}#NAME") }
                }),
                ExpectedResultCount = 0
            })
            {
                TestName = "BetweenExpression_String_Middle"
            };

            var lowerIntBounds = 5;
            var upperIntBounds = 10;
            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0010" && Functions.Between(a.Age, lowerIntBounds, upperIntBounds),
                SeedData = BaseTest.GenerateData(15, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0010") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    { "age", new AttributeValue(){ N = i.ToString()} }
                }),
                ExpectedResultCount = 6
            })
            {
                TestName = "BetweenExpression_Integer"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Name == "ORCONDITION#0001" || a.Age == 999,
                SeedData = BaseTest.GenerateData(5, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0011") },
                    { "sk", new AttributeValue($"ORCONDITION#{i.ToString("D4")}") },
                    { "age", new AttributeValue(){ N = (999-i).ToString()} }
                }),
                ExpectedResultCount = 2
            })
            {
                TestName = "EqualsExpression_String_OrCondition"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0012" && (a.Name == "ITEM#0003" || a.Age == 5),
                SeedData = BaseTest.GenerateData(10, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0012") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    { "age", new AttributeValue(){ N = i.ToString()} }
                }),
                ExpectedResultCount = 2
            })
            {
                TestName = "EqualsExpression_String_MultipleConditions"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0013" && a.StringArray.Contains("0004"),
                SeedData = BaseTest.GenerateData(10, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0013") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    { "stringArray", new AttributeValue { L = Enumerable.Range(i-3, 5).Select(ii => new AttributeValue(ii.ToString("D4"))).ToList()}}
                }),
                ExpectedResultCount = 5
            })
            {
                TestName = "ContainsExpression_StringArray"
            };

            var arr1 = new[] { "ITEM#0002", "ITEM#0004" };
            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0014" && arr1.Contains(a.Name),
                SeedData = BaseTest.GenerateData(10, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0014") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                }),
                ExpectedResultCount = 2
            })
            {
                TestName = "InExpression_StringArray"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0015" && a.IntArray.Contains(60),
                SeedData = BaseTest.GenerateData(10, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0015") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    { "intArray", new AttributeValue { NS = Enumerable.Range(i-3, 5).Select(ii => (ii*10).ToString()).ToList()}}
                }),
                ExpectedResultCount = 5
            })
            {
                TestName = "ContainsExpression_IntArray"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0016" && Functions.AttributeExists(a.IntArray),
                SeedData = BaseTest.GenerateData(9, (i) =>
                {
                    var dict = new Dictionary<string, AttributeValue> {
                        { "pk", new AttributeValue("ENTITY#0016") },
                        { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    };
                    if (i % 4 == 0)
                    {
                        dict.Add("intArray", new AttributeValue { NS = Enumerable.Range(i, 5).Select(ii => (ii).ToString()).ToList() });
                    }
                    return dict;
                }),
                ExpectedResultCount = 3
            })
            {
                TestName = "AttributeExistsExpression_IntArray"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0017" && Functions.AttributeExists(a.InnerObject!.Name),
                SeedData = BaseTest.GenerateData(9, (i) =>
                {
                    var dict = new Dictionary<string, AttributeValue> {
                        { "pk", new AttributeValue("ENTITY#0017") },
                        { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                        { "innerObject", new AttributeValue(){ M = new Dictionary<string, AttributeValue> {
                            { "pk", new AttributeValue("INNERENTITY#0017") },
                        }} },
                    };
                    if (i % 4 == 0)
                    {
                        dict["innerObject"].M.Add("sk", new AttributeValue("test"));
                    }
                    return dict;
                }),
                ExpectedResultCount = 3
            })
            {
                TestName = "AttributeExistsExpression_NestedType"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0018" && Functions.AttributeNotExists(a.IntArray),
                SeedData = BaseTest.GenerateData(9, (i) =>
                {
                    var dict = new Dictionary<string, AttributeValue> {
                        { "pk", new AttributeValue("ENTITY#0018") },
                        { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    };
                    if (i % 4 == 0)
                    {
                        dict.Add("intArray", new AttributeValue { NS = Enumerable.Range(i, 5).Select(ii => (ii).ToString()).ToList() });
                    }
                    return dict;
                }),
                ExpectedResultCount = 6
            })
            {
                TestName = "AttributeNotExistsExpression_IntArray"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0019" && Functions.AttributeNotExists(a.InnerObject!.Name),
                SeedData = BaseTest.GenerateData(9, (i) =>
                {
                    var dict = new Dictionary<string, AttributeValue> {
                        { "pk", new AttributeValue("ENTITY#0019") },
                        { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                        { "innerObject", new AttributeValue(){ M = new Dictionary<string, AttributeValue> {
                            { "pk", new AttributeValue("INNERENTITY#0019") },
                        }} },
                    };
                    if (i % 4 == 0)
                    {
                        dict["innerObject"].M.Add("sk", new AttributeValue("test"));
                    }
                    return dict;
                }),
                ExpectedResultCount = 6
            })
            {
                TestName = "AttributeNotExistsExpression_NestedType"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0020" && Functions.Size(a.IntArray) > 3,
                SeedData = BaseTest.GenerateData(10, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0020") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    { "intArray", new AttributeValue { NS = Enumerable.Range(0, i+1).Select(ii => ii.ToString()).ToList()}}
                }),
                ExpectedResultCount = 7
            })
            {
                TestName = "SizeExpression_IntArray"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0021" && a.IntArray.Count() > 3,
                SeedData = BaseTest.GenerateData(10, (i) => new Dictionary<string, AttributeValue> {
                    { "pk", new AttributeValue("ENTITY#0021") },
                    { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    { "intArray", new AttributeValue { NS = Enumerable.Range(0, i+1).Select(ii => ii.ToString()).ToList()}}
                }),
                ExpectedResultCount = 7
            })
            {
                TestName = "EnumerableCountExpression_IntArray"
            };

            yield return new TestCaseData(new ScanRequestTestCases
            {
                Expression = (a) => a.Id == "ENTITY#0022" && Functions.AttributeType(a.IntArray, AttributeType.NS),
                SeedData = BaseTest.GenerateData(9, (i) =>
                {
                    var dict = new Dictionary<string, AttributeValue> {
                        { "pk", new AttributeValue("ENTITY#0022") },
                        { "sk", new AttributeValue($"ITEM#{i.ToString("D4")}") },
                    };
                    if (i % 4 == 0)
                    {
                        dict.Add("intArray", new AttributeValue { NS = Enumerable.Range(i, 5).Select(ii => (ii).ToString()).ToList() });
                    }
                    else
                    {
                        dict.Add("intArray", new AttributeValue { SS = Enumerable.Range(i, 5).Select(ii => (ii).ToString()).ToList() });
                    }
                    return dict;
                }),
                ExpectedResultCount = 3
            })
            {
                TestName = "AttributeTypeExpression_IntArray"
            };

        }
    }
}