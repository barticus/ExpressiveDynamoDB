namespace ExpressiveDynamoDB.QueryGeneration
{
    public static class Functions
    {
        public static bool Between(this object attribute, object lowerBounds, object upperBounds) => false;
        public static bool AttributeExists(this object attribute) => false;
        public static bool AttributeNotExists(this object attribute) => false;
        public static bool AttributeType(this object attribute, AttributeType type) => false;
        public static int Size(this object attribute) => 0;
    }
}