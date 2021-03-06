using System;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Modelling.FieldTransformers
{
    public class PrefixPartitionKeyTransformerAttribute : PartitionKeyTransformerAttribute
    {
        private string Prefix { get; set; }

        public PrefixPartitionKeyTransformerAttribute(string prefix)
        {
            Prefix = prefix;
        }

        public override DynamoDBEntry Transform(DynamoDBEntry input)
        {
            var sInput = (string)input;
            if (sInput == null)
                throw new ArgumentException("input should be a string.");

            return $"{Prefix}#{sInput}";
        }
    }
}