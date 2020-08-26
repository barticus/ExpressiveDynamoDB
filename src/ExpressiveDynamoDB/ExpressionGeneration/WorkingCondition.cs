using System;
using System.Collections.Generic;
using System.Linq;
using Ddb = Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;

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
            condition.AttributeValueList = new Ddb.Document(Values).ToAttributeMap().Values.ToList();
            return new KeyValuePair<string, Condition>(PropertyName, condition);
        }
    }
}