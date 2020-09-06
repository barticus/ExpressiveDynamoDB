using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.ExpressionGeneration
{
    public static class UnmappedComparisonOperator
    {
        public static readonly ComparisonOperator Size = new ComparisonOperator("SIZE");
    }
}