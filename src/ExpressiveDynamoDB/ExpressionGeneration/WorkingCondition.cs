using System;
using System.Collections.Generic;
using System.Linq;
using Ddb = Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using ExpressiveDynamoDB.Extensions;

namespace ExpressiveDynamoDB.ExpressionGeneration
{
    internal sealed class WorkingCondition
    {
        public string? PropertyName { get; set; }
        public string? MemberExpression { get; set; }
        public bool IsPropertyNameSet => !string.IsNullOrWhiteSpace(PropertyName);
        public ComparisonOperator? ComparisonOperator { get; set; }
        public Dictionary<string, Ddb.DynamoDBEntry> Values { get; set; } = new Dictionary<string, Ddb.DynamoDBEntry>();
        public bool CanMapToCondition { get; set; } = true;

        public KeyValuePair<string, Condition>? ToCondition()
        {
            if (PropertyName == null || ComparisonOperator == null)
            {
                throw new ArgumentException("ComparisonOperator has not been set!");
            }

            if (!CanMapToCondition)
            {
                return null;
            }

            var condition = new Condition();
            condition.ComparisonOperator = ComparisonOperator;
            condition.AttributeValueList = Values.ToAttributeMap().Values.ToList();
            return new KeyValuePair<string, Condition>(PropertyName, condition);
        }

        private static readonly Dictionary<ComparisonOperator, Func<string, string[], string>> ComparisonOperatorToExpressionMap = new Dictionary<ComparisonOperator, Func<string, string[], string>> {
            {ComparisonOperator.EQ, (member, args) => $"{member} = {args[0]}"},
            {ComparisonOperator.NE, (member, args) => $"{member} <> {args[0]}"},
            {ComparisonOperator.LE, (member, args) => $"{member} <= {args[0]}"},
            {ComparisonOperator.LT, (member, args) => $"{member} < {args[0]}"},
            {ComparisonOperator.GT, (member, args) => $"{member} > {args[0]}"},
            {ComparisonOperator.GE, (member, args) => $"{member} >= {args[0]}"},
            {ComparisonOperator.BEGINS_WITH, (member, args) => $"begins_with({member}, {args[0]})"},
            {ComparisonOperator.CONTAINS, (member, args) => $"contains({member}, {args[0]})"},
            {ComparisonOperator.BETWEEN, (member, args) => $"{member} BETWEEN {args[0]} AND {(args.Length > 1 ? args[1] : args[0])}"},
            {ComparisonOperator.NOT_NULL, (member, args) => $"attribute_exists({member})"},
            {ComparisonOperator.NULL, (member, args) => $"attribute_not_exists({member})"},
            {ComparisonOperator.IN, (member, args) => $"{member} IN ({string.Join(", ", args)})"},
            {UnmappedComparisonOperator.Size, (member, args) => $"size({member})"},
            {UnmappedComparisonOperator.AttributeType, (member, args) => $"attribute_type({member}, {args[0]})"},
        };

        public string ToExpressionStatement()
        {
            if (PropertyName == null)
            {
                throw new InvalidOperationException("PropertyName was not defined");
            }
            if (ComparisonOperator == null)
            {
                throw new InvalidOperationException("ComparisonOperator was not defined");
            }
            if (!ComparisonOperatorToExpressionMap.ContainsKey(ComparisonOperator))
            {
                throw new InvalidOperationException($"{ComparisonOperator} cannot be mapped to an expression");
            }
            var attributeName = AttributeNameKey(MemberExpression ?? PropertyName!);
            var attributeValueKeys = Values
                .SelectMany(kvp => ExplodeAttributeValueEntries(kvp.Key, kvp.Value))
                .Select(kvp => kvp.Key)
                .ToArray();
            return ComparisonOperatorToExpressionMap[ComparisonOperator](attributeName, attributeValueKeys);
        }

        public static string AttributeNameKey(string attributeName)
        {
            if (attributeName.Contains("#")) return attributeName;
            return $"#{attributeName.Replace(".", ".#")}";
        }

        public static Dictionary<string, string> AttributeNameKeys(string attributeName)
        {
            return attributeName.Split('.').ToDictionary(s => AttributeNameKey(s), s => s);
        }

        public Dictionary<string, Ddb.DynamoDBEntry> ExplodeAttributeValueEntries(string attributeValue, Ddb.DynamoDBEntry entry)
        {
            var returnValues = new Dictionary<string, Ddb.DynamoDBEntry>();
            if (ComparisonOperator == ComparisonOperator.IN)
            {
                var attribute = new Dictionary<string, Ddb.DynamoDBEntry>{
                    { attributeValue , entry }
                }.ToAttributeMap().First().Value;
                
                var index = 0;
                var returnAttributes = new Dictionary<string, AttributeValue>();
                if(attribute.IsLSet)
                {
                    attribute.L.ForEach((item) => {
                        returnAttributes.Add(AttributeValueKey($"{attributeValue}_{index}"), item);
                        index++;
                    });
                }
                if(attribute.NS.Any())
                {
                    attribute.NS.ForEach((item) => {
                        returnAttributes.Add(AttributeValueKey($"{attributeValue}_{index}"), new AttributeValue() { N = item });
                        index++;
                    });
                }
                if(attribute.SS.Any())
                {
                    attribute.SS.ForEach((item) => {
                        returnAttributes.Add(AttributeValueKey($"{attributeValue}_{index}"), new AttributeValue(item));
                        index++;
                    });
                }

                returnValues = Ddb.Document
                    .FromAttributeMap(returnAttributes)
                    .ToDictionary(d => d.Key, d => d.Value);
            }
            else
            {
                returnValues.Add(AttributeValueKey(attributeValue), entry);
            }

            return returnValues;
        }

        public static string AttributeValueKey(string attributeValue)
        {
            if (attributeValue.Contains(":")) return attributeValue;
            return $":{attributeValue.Replace(".", "")}";
        }
    }
}