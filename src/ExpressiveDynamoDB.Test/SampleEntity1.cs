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
    }
}