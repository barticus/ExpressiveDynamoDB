
using System;
using System.Reflection;
using Amazon.DynamoDBv2;

namespace ExpressiveDynamoDB.ExpressionGeneration
{
    public class SupportedMethod
    {
        public MethodInfo MethodInfo { get; }

        public int ExpectedArgumentCount { get; set; }

        public bool VisitArguments { get; set; }

        public bool VisitObject { get; set; }

        public ComparisonOperator ComparisonOperator { get; }

        public SupportedMethod(MethodInfo info, ComparisonOperator comparisonOperator)
        {
            MethodInfo = info;
            ComparisonOperator = comparisonOperator;
        }

        public static SupportedMethod StringContainsMethod = new SupportedMethod(((Func<string, bool>)"".Contains).GetMethodInfo(), ComparisonOperator.CONTAINS)
        {
            ExpectedArgumentCount = 1,
            VisitArguments = true,
            VisitObject = true
        };

        public static SupportedMethod StringStartsWithMethod = new SupportedMethod(((Func<string, bool>)"".StartsWith).GetMethodInfo(), ComparisonOperator.BEGINS_WITH)
        {
            ExpectedArgumentCount = 1,
            VisitArguments = true,
            VisitObject = true
        };

        public static SupportedMethod BetweenMethod = new SupportedMethod(typeof(Functions).GetMethod(nameof(Functions.Between)), ComparisonOperator.BETWEEN)
        {
            ExpectedArgumentCount = 3,
            VisitArguments = true,
            VisitObject = false
        };

        // public static SupportedMethod EnumerableContainsMethod = new SupportedMethod(((Func<object, bool>)new object[0].Contains).GetMethodInfo(), ComparisonOperator.CONTAINS)
        // {
        //     ExpectedArgumentCount = 1,
        //     VisitArguments = true,
        //     VisitObject = true
        // };

    }
}