using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using ExpressiveDynamoDB.FieldTransformers;

namespace ExpressiveDynamoDB
{
    public interface ITable
    {
        Task<ItemResponse<T>> GetItemAsync<T>(
            string partitionKey,
            string? sortKey = null,
            CancellationToken cancellationToken = default
        );

        Task<ItemResponse<T>> GetItemAsync<T>(
            GetItemRequest itemRequest,
            CancellationToken cancellationToken = default
        );
    }

    public class BaseKeyTransformationBuilder
    {
        private List<(IFieldTransformer, object properties)> PartitionKeyTransformers { get; } = new List<(IFieldTransformer, object properties)>();
        private List<(IFieldTransformer, object properties)> SortKeyTransformers { get; } = new List<(IFieldTransformer, object properties)>();

        protected void HasPartitionKeyTransformer(IFieldTransformer transformer)
        {
            PartitionKeyTransformers.Add((transformer, null));
        }

        protected void HasPartitionKeyTransformer<T>(IFieldTransformer transformer, T properties)
        {
            PartitionKeyTransformers.Add((transformer, properties));
        }

        protected void HasSortKeyTransformer(IFieldTransformer transformer)
        {
            SortKeyTransformers.Add((transformer, null));
        }

        protected void HasSortKeyTransformer<T>(IFieldTransformer transformer, T properties)
        {
            SortKeyTransformers.Add((transformer, properties));
        }
    }

    public class PrimaryKeyTransformationBuilder: BaseKeyTransformationBuilder
    {

    }

    public class ItemCollectionKeyTransformationBuilder: BaseKeyTransformationBuilder
    {

    }

    public class ItemCollection {}

    public interface IItemModelBuilder
    {
        IItemModelBuilder PrimaryKey(Action<PrimaryKeyTransformationBuilder> ktbAction);
        IItemModelBuilder PrimaryKey(ItemCollection itemCollection, Action<ItemCollectionKeyTransformationBuilder> ktbAction);
    }

    public class ItemModelBuilder<T>: IItemModelBuilder
    {
        public IItemModelBuilder PrimaryKey(Action<PrimaryKeyTransformationBuilder> ktbAction)
        {
            return this;
        }

        public IItemModelBuilder PrimaryKey(ItemCollection itemCollection, Action<ItemCollectionKeyTransformationBuilder> ktbAction)
        {
            return this;
        }
    }

    public class TableModelBuilder
    {
        private KeySchema ResolvedPrimaryKey { get; set; }

        private List<IItemModelBuilder> ItemModelBuilders { get; set; }

        public ItemModelBuilder<T> ItemModel<T>(Action<ItemModelBuilder<T>> itemModelBuilderAction)
        {
            var itmb = new ItemModelBuilder<T>();
            ItemModelBuilders.Add(itmb);
            itemModelBuilderAction(itmb);
            return itmb;
        }

        public void PrimaryKey<T>(
            Expression<Func<T, object>> partitionKeyExpression
        )
        {
            ResolvedPrimaryKey = KeySchema.From<T>(partitionKeyExpression);
        }

        public void PrimaryKey<T>(
            Expression<Func<T, object>> partitionKeyExpression,
            Expression<Func<T, object>> sortKeyExpression
        )
        {
            ResolvedPrimaryKey = KeySchema.From<T>(partitionKeyExpression, sortKeyExpression);
        }
    }

    public abstract class Table : ITable
    {
        private string TableName { get; set; }
        private KeySchema PrimaryKey { get; set; }
        private IAmazonDynamoDB DynamoDb { get; }
        private IEntityMapper EntityMapper { get; }

        public Table(TableOptions tableOptions)
        {
            DynamoDb = tableOptions.DynamoDb;
            EntityMapper = tableOptions.EntityMapper;

            var modelBuilder = new TableModelBuilder();
            OnModelBuilding(modelBuilder);

            if(TableName == null)
            {
                throw new ArgumentException($"{nameof(TableName)} must be defined.");
            }
        }

        public Table(string tableName, TableOptions tableOptions): this(tableOptions)
        {
            TableName = tableName;
        }
        

        public abstract void OnModelBuilding(TableModelBuilder tableModelBuilder);

        public async Task<ItemResponse<T>> GetItemAsync<T>(
            GetItemRequest getItemRequest,
            CancellationToken cancellationToken = default
        )
        {
            getItemRequest.TableName = TableName;
            var result = await DynamoDb.GetItemAsync(getItemRequest, cancellationToken);
            return new ItemResponse<T>(EntityMapper.FromDocument<T>(Document.FromAttributeMap(result.Item)));
        }

        public Task<ItemResponse<T>> GetItemAsync<T>(
            Action<GetItemRequestBuilder> getItemRequestBuilderAction,
            CancellationToken cancellationToken = default
        )
        {
            var girb = new GetItemRequestBuilder(PrimaryKey);
            getItemRequestBuilderAction(girb);
            return GetItemAsync<T>(girb.Build(), cancellationToken);
        }

        public Task<ItemResponse<T>> GetItemAsync<T>(string partitionKey, string? sortKey = null, CancellationToken cancellationToken = default) => 
            GetItemAsync<T>((girb) => girb.WithKey<T>(partitionKey, sortKey), cancellationToken);
    }
}