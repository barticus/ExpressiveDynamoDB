using Ddb = Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Modelling
{
    public class ExpressiveTableFactory
    {
        public IEntityMapper EntityMapper { get; set; }

        public ExpressiveTableFactory(IEntityMapper entityMapper)
        {
            EntityMapper = entityMapper;
        }
        
        public ExpressiveTable FromTable(Ddb.Table table)
        {
            return new ExpressiveTable(table, EntityMapper);
        }
    }
}