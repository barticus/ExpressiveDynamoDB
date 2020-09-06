using ExpressiveDynamoDB.ExpressionGeneration;

namespace ExpressiveDynamoDB
{
    public static class Functions
    {
        public static bool Between<T>(T attribute, T lowerBounds, T upperBounds) => false;
        public static bool AttributeExists(object attribute) => false;
        public static bool AttributeNotExists(object attribute) => false;
        public static bool AttributeType(object attribute, AttributeType type) => false;
        public static int Size(object attribute) => 0;
    }
}