using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;

namespace ExpressiveDynamoDB.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static string DynamoDbAttributeName(this PropertyInfo propertyInfo)
        {
            var propertyAttribute = propertyInfo.GetCustomAttributes(typeof(DynamoDBAttribute), true)
                        .OfType<DynamoDBPropertyAttribute>()
                        .SingleOrDefault();

            return propertyAttribute?.AttributeName ?? propertyInfo.Name;
        }
    }
}