
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace ExpressiveDynamoDB.ExpressionGeneration
{
    public class SupportedMethod
    {
        public Func<MethodInfo, bool> MatchesMethod { get; }

        public int ExpectedArgumentCount { get; set; }

        public bool VisitArguments { get; set; }

        public bool VisitObject { get; set; }

        public ComparisonOperator ComparisonOperator { get; }

        public ComparisonOperator? ComparisonOperatorIfPropertyWasArgument { get; set; }

        public bool CanMapToCondition { get; set; } = true;

        public Dictionary<Type, IPropertyConverter> TypeConverters { get; } = new Dictionary<Type, IPropertyConverter>();

        public SupportedMethod(MethodInfo info, ComparisonOperator comparisonOperator) : this((MethodInfo) => MethodInfo.Equals(info), comparisonOperator)
        { }

        public SupportedMethod(Func<MethodInfo, bool> methodMatcher, ComparisonOperator comparisonOperator)
        {
            MatchesMethod = methodMatcher;
            ComparisonOperator = comparisonOperator;
        }

        public SupportedMethod AddTypeConverter<TT, TC>() where TC: IPropertyConverter, new()
        {
            TypeConverters.Add(typeof(TT), new TC());
            return this;
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

        public static SupportedMethod BetweenMethod = new SupportedMethod((info) => info.DeclaringType == typeof(Functions) && info.Name == nameof(Functions.Between), ComparisonOperator.BETWEEN)
        {
            ExpectedArgumentCount = 3,
            VisitArguments = true,
            VisitObject = false
        };

        public static SupportedMethod EnumerableContainsMethod = new SupportedMethod(
            (info) => info.DeclaringType.Name == nameof(Enumerable)
                && info.Name == nameof(Enumerable.Contains), ComparisonOperator.CONTAINS)
        {
            ExpectedArgumentCount = 2,
            VisitArguments = true,
            VisitObject = false, // this is an extension method, so object is argument anyway
            ComparisonOperatorIfPropertyWasArgument = ComparisonOperator.IN
        };

        public static SupportedMethod AttributeExistsMethod = new SupportedMethod(typeof(Functions).GetMethod(nameof(Functions.AttributeExists)), ComparisonOperator.NOT_NULL)
        {
            ExpectedArgumentCount = 1,
            VisitArguments = true,
            VisitObject = false
        };

        public static SupportedMethod AttributeNotExistsMethod = new SupportedMethod(typeof(Functions).GetMethod(nameof(Functions.AttributeNotExists)), ComparisonOperator.NULL)
        {
            ExpectedArgumentCount = 1,
            VisitArguments = true,
            VisitObject = false
        };

        public static SupportedMethod SizeMethod = new SupportedMethod(typeof(Functions).GetMethod(nameof(Functions.Size)), UnmappedComparisonOperator.Size)
        {
            ExpectedArgumentCount = 1,
            VisitArguments = true,
            VisitObject = false,
            CanMapToCondition = false,
        };

        public static SupportedMethod EnumerableCountMethod = new SupportedMethod(
            (info) => info.DeclaringType.Name == nameof(Enumerable)
                && info.Name == nameof(Enumerable.Count), UnmappedComparisonOperator.Size)
        {
            ExpectedArgumentCount = 1,
            VisitArguments = true,
            VisitObject = false, // this is an extension method, so object is argument anyway
            CanMapToCondition = false,
        };

        public static SupportedMethod AttributeTypeMethod = new SupportedMethod(typeof(Functions).GetMethod(nameof(Functions.AttributeType)), UnmappedComparisonOperator.AttributeType)
        {
            ExpectedArgumentCount = 2,
            VisitArguments = true,
            VisitObject = false,
            CanMapToCondition = false,
        }.AddTypeConverter<AttributeType, AttributeTypeConverter>();

    }
}