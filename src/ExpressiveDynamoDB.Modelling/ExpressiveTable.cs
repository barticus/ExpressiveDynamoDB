using System.Threading;
using System.Threading.Tasks;
using Ddb = Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Modelling
{
    public class ExpressiveTable
    {
        public Ddb.Table Table { get; set; }

        public IEntityMapper EntityMapper { get; set; }

        public ExpressiveTable(Ddb.Table table, IEntityMapper entityMapper)
        {
            Table = table;
            EntityMapper = entityMapper;
        }

        public async Task<T> GetItemAsync<T>(
            Ddb.Primitive hashKey, 
            CancellationToken cancellationToken = default
        ) {
            return EntityMapper.FromDocument<T>(await Table.GetItemAsync(hashKey, cancellationToken));
        }

        public async Task<T> GetItemAsync<T>(
            Ddb.Primitive hashKey, 
            Ddb.Primitive rangeKey, 
            CancellationToken cancellationToken = default
        ) {
            return EntityMapper.FromDocument<T>(await Table.GetItemAsync(hashKey, rangeKey, cancellationToken));
        }
    }
}