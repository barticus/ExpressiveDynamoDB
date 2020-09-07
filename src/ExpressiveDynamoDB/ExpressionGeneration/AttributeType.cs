using System;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB
{
    public enum AttributeType 
    {
        ///<summary>
        /// String
        ///</summary>
        S,

        ///<summary>
        /// String Set
        ///</summary>
        SS,

        ///<summary>
        /// Number
        ///</summary>
        N,

        ///<summary>
        /// Number Set
        ///</summary>
        NS,

        ///<summary>
        /// Binary
        ///</summary>
        B,

        ///<summary>
        /// Binary Set
        ///</summary>
        BS,

        ///<summary>
        /// Boolean
        ///</summary>
        BOOL,

        ///<summary>
        /// Null
        ///</summary>
        NULL,

        ///<summary>
        /// List
        ///</summary>
        L,

        ///<summary>
        /// Map
        ///</summary>
        M
    }

    public class AttributeTypeConverter : IPropertyConverter
    {
        public DynamoDBEntry ToEntry(object value)
        {
            if(!(value is AttributeType attributeValue))
            {
                throw new ArgumentException($"{nameof(value)} should have been of type {nameof(AttributeType)}");
            }
            return attributeValue.ToString();
        }

        public object FromEntry(DynamoDBEntry entry)
        {
            var entryValue = entry.AsString();
            if(string.IsNullOrWhiteSpace(entryValue))
            {
                throw new ArgumentException($"{nameof(entry)} should have been of type {nameof(String)}");
            }
            return Enum.Parse<AttributeType>(entryValue);
        }
    }
}