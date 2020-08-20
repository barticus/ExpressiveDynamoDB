using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.FieldTransformers
{
    public interface IFieldTransformer
    {
        DynamoDBEntry Transform(DynamoDBEntry input);
    }
}