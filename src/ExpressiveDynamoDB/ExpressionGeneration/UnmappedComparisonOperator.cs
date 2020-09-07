using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.ExpressionGeneration
{
    public static class UnmappedComparisonOperator
    {
        public static readonly ComparisonOperator Size = new ComparisonOperator("SIZE");
        public static readonly ComparisonOperator AttributeType = new ComparisonOperator("ATTRIBUTE_TYPE");
    }
}