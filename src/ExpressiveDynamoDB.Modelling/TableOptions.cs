
using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.Modelling
{
    public class TableOptions
    {
        
        public IAmazonDynamoDB DynamoDb { get; }
        public IEntityMapper EntityMapper { get; }

        public TableOptions(IAmazonDynamoDB dynamoDb, IEntityMapper entityMapper)
        {
            DynamoDb = dynamoDb;
            EntityMapper = entityMapper;
        }
    }
}