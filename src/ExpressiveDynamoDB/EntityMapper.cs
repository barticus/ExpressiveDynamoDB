using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB
{
    public interface IEntityMapper
    {
        Document ToDocument<T>(T item);

        T FromDocument<T>(Document document);
    }

    public class EntityMapper: IEntityMapper
    {
        private IDynamoDBContext DbContext { get; }
        
         public EntityMapper(IDynamoDBContext dbContext)
         {
             DbContext = dbContext;
         }

         public Document ToDocument<T>(T item)
         {
             return DbContext.ToDocument(item);
         }

        public T FromDocument<T>(Document document)
        {
             return DbContext.FromDocument<T>(document);
        }

    }
}