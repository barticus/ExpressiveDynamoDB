using System;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Modelling.FieldTransformers
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple=true)]
    public abstract class FieldTransformerAttribute : Attribute, IFieldTransformer
    {
        public abstract DynamoDBEntry Transform(DynamoDBEntry input);
    }
}