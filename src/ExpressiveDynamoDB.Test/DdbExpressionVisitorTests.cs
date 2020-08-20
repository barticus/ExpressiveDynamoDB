using ExpressiveDynamoDB;
using ExpressiveDynamoDB.QueryGeneration;
using System;
using System.Linq.Expressions;
using NUnit.Framework;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Test
{
    public class DdbExpressionVisitorTests
    {
        [Test]
        [TestCaseSource(typeof(ExpressionTestCase), nameof(ExpressionTestCase.GetCases))]
        public void BuildStatement_TestCases(ExpressionTestCase testCase)
        {
            // Arrange

            // Act
            var output = DdbExpressionVisitor.BuildStatement(testCase.Expression);

            // Assert
            Assert.AreEqual(testCase.ExpectedExpressionResult, output.ExpressionStatement);
            foreach(var expectedAttributeValue in testCase.ExpectedAttributeValues)
            {
                Assert.AreEqual(expectedAttributeValue.Item2, output.ExpressionAttributeValues[expectedAttributeValue.Item1]);
            }
            foreach(var expectedAttributeName in testCase.ExpectedAttributeNames)
            {
                Assert.AreEqual(expectedAttributeName.Item2, output.ExpressionAttributeNames[expectedAttributeName.Item1]);
            }
        }

        public class ExpressionTestCase
        {
            public Expression<Func<SampleEntity1, bool>> Expression { get; set; }
            public string ExpectedExpressionResult { get; set; }
            public (string, DynamoDBEntry)[] ExpectedAttributeValues { get; set; } 
            public (string, string)[] ExpectedAttributeNames { get; set; } 

            public static IEnumerable<TestCaseData> GetCases()
            {
                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":pString", "SAMPLEENTITY#myId")
                    },
                    ExpectedExpressionResult = "(#Id = :pString)",
                    Expression = (a) => a.Id == "SAMPLEENTITY#myId"
                }) {
                    TestName = "EqualsExpression_String_PrimaryKeyConstant"
                };

                var id = "SAMPLEENTITY#myId";
                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":id", id)
                    },
                    ExpectedExpressionResult = "(#Id = :id)",
                    Expression = (a) => a.Id == id
                }) {
                    TestName = "EqualsExpression_String_PrimaryKeyMember"
                };
                
                var name = "SAMPLEENTITY#myName";
                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk"),
                        ("#Name", "sk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":id", id),
                        (":name", name)
                    },
                    ExpectedExpressionResult = "((#Id = :id) AND (#Name = :name))",
                    Expression = (a) => a.Id == id && a.Name == name
                }) {
                    TestName = "EqualsExpression_String_CompositePrimaryKeyMember"
                };

                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk"),
                        ("#Name", "sk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":id", id),
                        (":name", name)
                    },
                    ExpectedExpressionResult = "((#Id = :id) AND begins_with(#Name, :name))",
                    Expression = (a) => a.Id == id && a.Name.StartsWith(name)
                }) {
                    TestName = "StartsWithExpression_String_CompositePrimaryKeyMember"
                };

                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk"),
                        ("#Name", "sk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":id", id),
                        (":name", name)
                    },
                    ExpectedExpressionResult = "((#Id = :id) AND contains(#Name, :name))",
                    Expression = (a) => a.Id == id && a.Name.Contains(name)
                }) {
                    TestName = "ContainsExpression_String_CompositePrimaryKeyMember"
                };

                var lowerBounds = "c";
                var upperBounds = "m";
                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk"),
                        ("#Name", "sk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":id", id),
                        (":lowerBounds", lowerBounds),
                        (":upperBounds", upperBounds)
                    },
                    ExpectedExpressionResult = "((#Id = :id) AND #Name BETWEEN :lowerBounds AND :upperBounds)",
                    Expression = (a) => a.Id == id && a.Name.Between(lowerBounds, upperBounds)
                }) {
                    TestName = "BetweenExpression_String_CompositePrimaryKeyMembers"
                };

                var middleBounds = "a";
                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk"),
                        ("#Name", "sk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":id", id),
                        (":middleBounds", middleBounds)
                    },
                    ExpectedExpressionResult = "((#Id = :id) AND #Name BETWEEN :middleBounds AND :middleBounds)",
                    Expression = (a) => a.Id == id && a.Name.Between(middleBounds, middleBounds)
                }) {
                    TestName = "BetweenExpression_String_CompositePrimaryKeyMember"
                };

                var lowerIntBounds = 5;
                var upperIntBounds = 10;
                yield return new TestCaseData(new ExpressionTestCase {
                    ExpectedAttributeNames = new [] {
                        ("#Id", "pk"),
                        ("#Name", "sk")
                    },
                    ExpectedAttributeValues = new (string, DynamoDBEntry)[] {
                        (":id", id),
                        (":lowerIntBounds", lowerIntBounds),
                        (":upperIntBounds", upperIntBounds)
                    },
                    ExpectedExpressionResult = "((#Id = :id) AND #Name BETWEEN :lowerIntBounds AND :upperIntBounds)",
                    Expression = (a) => a.Id == id && a.Name.Between(lowerIntBounds, upperIntBounds)
                }) {
                    TestName = "BetweenExpression_Integer_CompositePrimaryKeyMembers"
                };
            }
        }
    }
}