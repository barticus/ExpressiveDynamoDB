namespace ExpressiveDynamoDB.ExpressionGeneration
{
    public static class Functions
    {
        public static bool Between(object attribute, object lowerBounds, object upperBounds) => false;
        public static bool AttributeExists(object attribute) => false;
        public static bool AttributeNotExists(object attribute) => false;
        public static bool AttributeType(object attribute, AttributeType type) => false;
        public static int Size(object attribute) => 0;
    }
}