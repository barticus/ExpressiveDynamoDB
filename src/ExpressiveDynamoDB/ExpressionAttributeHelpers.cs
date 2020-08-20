using System.Collections.Generic;
using System.Linq;

namespace ExpressiveDynamoDB.FieldTransformers
{
    public static class ExpressionAttributeHelpers
    {
        public const string AttributeValuePrefix = ":";
        public const string AttributeKeyPrefix = "#";

        public static string GetAttributeName(string desiredName, string prefix, string[] existingNames)
        {
            var valueName = $"{prefix}{desiredName}";
            var counter = existingNames.Length;
            while(existingNames.Contains(valueName)){
                valueName = $"{prefix}{desiredName}{counter}";
                counter++;
            }
            return valueName;
        }

        public static string GetAttributeValueName(string desiredName, string[] existingNames) => GetAttributeName(desiredName, AttributeValuePrefix, existingNames);

        public static string GetAttributeKeyName(string desiredName, string[] existingNames) => GetAttributeName(desiredName, AttributeKeyPrefix, existingNames);
        
        public static string GetAttributeKeyName(string desiredName, Dictionary<string, string> existingAttributeKeys) => GetAttributeName(desiredName, AttributeKeyPrefix, existingAttributeKeys.Keys.ToArray());
    }
}