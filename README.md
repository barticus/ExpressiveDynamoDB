# ExpressiveDynamoDB

A library to assist with the generation of DynamoDB Expressions, and interacting with the DynamoDB SDK.

## Getting Started

### Installation

This library is available as a [nuget package](https://www.nuget.org/packages/ExpressiveDynamoDB/)

```bash
dotnet add package ExpressiveDynamoDB
```

*NOTE* that this library is a work in progress and you should perform your own testing to confirm it is fit for production.

### Writing a Query Expression with the Document Model

You may be using the Document Model as it appears to be the suggested way to interact with DynamoDB.

[See here for more info](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DotNetSDKMidLevel.html)

Generally, you'll interact with your DynamoDB table via the Table object.

```csharp
Table myTable = Table.LoadTable(client, "MyTableName");
```

And lets say you have a data model class such as

```csharp
public class MyEntity
{
    [DynamoDBProperty("pk")]
    public string Id { get; set; } = default!;

    [DynamoDBProperty("sk")]
    public string Name { get; set; } = default!;

    [DynamoDBProperty("age")]
    public int Age { get; set; }
}
```

*note* that `DynamoDBProperty` attributes are respected for mapping to underlying attribute names.

ExpressiveDynamoDB adds some extension methods to help you write your query conditions like:

```csharp
using ExpressiveDynamoDB.Extensions;

// native operators
myTable.Query<MyEntity>(e => e.Id == "ENTITY#1234" && e.Name == "USER#BOB"));
myTable.Query<MyEntity>(e => e.Id == "ENTITY#1234" && e.Name.StartsWith("USER#"));
myTable.Query<MyEntity>(e => e.Id == "ENTITY#1234" && e.Name.Contains("BOB"));

// DynamoDB specific functions (more to come)
var myQuery = myTable.Query<MyEntity>(e => e.Id == "ENTITY#1234" && Functions.Between(e.Name, "USER#A", "USER#F"));
```

If you'd like to map back to your entity type, you can use DI in your class to inject IEntityMapper, which allows you to run:

```csharp
var myEntities = (await myQuery.GetNextSetAsync()).Select(d => EntityMapper.FromDocument<MyEntity>(d));
```

### Writing a Query Expression with the Low Level Client

If you do not wish to use the Document Model (which requires a call to DescribeTable), low level client operations are also supported.

```csharp
using ExpressiveDynamoDB.Extensions;

//assumes the following through your DI
IAmazonDynamoDB myClient = new AmazonDynamoDB();
IEntityMapper myMapper = new EntityMapper();

var myResponse = myClient.Query(new QueryRequest("MyTableName")
    .KeyConditionExpression<MyEntity>(e => e.Id == "ENTITY#1234" && e.Name.StartsWith("USER#"))
    .FilterExpression<MyEntity>(e => e.Age > 30)
);
var myEntities = myResponse.Items<MyEntity>(myMapper);
```

## Roadmap

1. Integration testing against local DynamoDB
2. Support for update expression types, e.g. `e => e.Age += 1`
3. Nested type support and further testing
4. A considered approach to modelling a table in C# with full support for expressions, indexes, entity mapping (to many types)
5. .NET 5 Source Generator for helping write IAM policies for DynamoDB access maybe?

## Contributing

Please raise issues using the Issue Template.

Please raise pull requests using the Pull Request Template.

If you have recommendations on improving this library, please get in touch.
