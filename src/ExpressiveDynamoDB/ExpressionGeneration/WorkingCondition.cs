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
        public ComparisonOperator? ComparisonOperator { get; set; }
        public Dictionary<string, Ddb.DynamoDBEntry> Values { get; set; } = new Dictionary<string, Ddb.DynamoDBEntry>();

        public KeyValuePair<string, Condition> ToCondition()
        {
            if (PropertyName == null || ComparisonOperator == null)
            {
                throw new ArgumentException("ComparisonOperator has not been set!");
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
            {ComparisonOperator.BETWEEN, (member, args) => $"{member} BETWEEN {args[0]} AND {(args.Length > 1 ? args[1] : args[0])}"}
        };

        public string ToExpressionStatement()
        {
            var memberExpressionName = $"#{PropertyName}";
            if(ComparisonOperator == null)
            {
                throw new InvalidOperationException("ComparisonOperator was not defined");
            }
            if(!ComparisonOperatorToExpressionMap.ContainsKey(ComparisonOperator))
            {
                throw new InvalidOperationException($"{ComparisonOperator} cannot be mapped to an expression");
            }
            return ComparisonOperatorToExpressionMap[ComparisonOperator](memberExpressionName, Values.Keys.Select(k => $":{k}").ToArray());
        }
    }
}