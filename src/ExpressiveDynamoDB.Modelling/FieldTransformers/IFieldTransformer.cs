using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Modelling.FieldTransformers
{
    public interface IFieldTransformer
    {
        DynamoDBEntry Transform(DynamoDBEntry input);
    }
}