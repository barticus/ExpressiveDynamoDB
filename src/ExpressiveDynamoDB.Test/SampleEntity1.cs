using Amazon.DynamoDBv2.DataModel;

namespace ExpressiveDynamoDB.Test
{
    public class SampleEntity1
    {
        [DynamoDBProperty("pk")]
        public string Id { get; set; } = default!;

        [DynamoDBProperty("sk")]
        public string Name { get; set; } = default!;

        [DynamoDBProperty("age")]
        public int Age { get; set; }

        [DynamoDBProperty("stringArray")]
        public string[] StringArray { get; set; } = default!;

        [DynamoDBProperty("intArray")]
        public int[] IntArray { get; set; } = default!;

        [DynamoDBProperty("innerObject")]
        public SampleEntity1? InnerObject { get; set; }
    }
}