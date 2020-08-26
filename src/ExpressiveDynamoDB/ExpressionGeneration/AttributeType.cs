namespace ExpressiveDynamoDB.ExpressionGeneration
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
}