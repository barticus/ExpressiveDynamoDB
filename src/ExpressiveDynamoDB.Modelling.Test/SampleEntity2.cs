using ExpressiveDynamoDB.Modelling.FieldTransformers;

namespace ExpressiveDynamoDB.Modelling.Test
{
    [PrefixPartitionKeyTransformerAttribute("PARTITION")]
    [PrefixSortKeyTransformerAttribute("SORT")]
    public class SampleEntity2
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}