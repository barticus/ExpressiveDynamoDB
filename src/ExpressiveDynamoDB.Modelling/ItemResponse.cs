namespace ExpressiveDynamoDB.Modelling
{
    public class ItemResponse<T>
    {
        public T Item { get; }

        public ItemResponse(T item)
        {
            Item = item;
        }
    }
}