using System.Linq;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.FieldTransformers
{
    public static class FieldTransformerHelpers
    {
        public static DynamoDBEntry ApplyAll(IFieldTransformer[] fieldTransformers, DynamoDBEntry input)
        {
            foreach(var transformer in fieldTransformers)
            {
                input = transformer.Transform(input);
            }
            return input;
        }

        public static DynamoDBEntry ApplyAllFrom<TM, TF>(DynamoDBEntry input) where TF: FieldTransformerAttribute
        {
            var fieldTransformerAttributes = typeof(TM).GetCustomAttributes(typeof(FieldTransformerAttribute), true)
                        .OfType<TF>()
                        .ToArray();

            return ApplyAll(fieldTransformerAttributes, input);
        }
    }
}