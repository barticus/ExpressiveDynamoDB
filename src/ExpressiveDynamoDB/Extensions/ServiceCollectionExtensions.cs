using ExpressiveDynamoDB;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ExpressiveDynamoDBExtensions
    {
        public static IServiceCollection AddExpressiveDynamoDB(
            this IServiceCollection serviceCollection
        )
        {
            serviceCollection.TryAddScoped<IEntityMapper, EntityMapper>();
            serviceCollection.TryAddScoped<IDynamoDBContext>((sp) => new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>()));
            return serviceCollection;
        }
    }
}