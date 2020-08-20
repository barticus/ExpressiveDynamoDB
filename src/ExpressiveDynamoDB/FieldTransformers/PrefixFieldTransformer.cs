using System;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.FieldTransformers
{
    public class PrefixFieldTransformer : IFieldTransformer
    {
        private string Prefix { get; set; }

        public PrefixFieldTransformer(string prefix)
        {
            Prefix = prefix;
        }

        public DynamoDBEntry Transform(DynamoDBEntry input)
        {
            var sInput = (string)input;
            if (sInput == null)
                throw new ArgumentException("input should be a string.");

            return $"{Prefix}#{sInput}";
        }
    }
}