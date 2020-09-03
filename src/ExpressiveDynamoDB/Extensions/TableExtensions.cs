using System.Linq.Expressions;
using Amazon.DynamoDBv2.DocumentModel;

namespace ExpressiveDynamoDB.Extensions
{
    public static class TableExtensions
    {
        public static Search Query<T>(this Table table, Expression<T> expression)
        {
            var qf = new QueryFilter();
            qf.AddCondition<T>(expression);
            return table.Query(qf);
        }
    }
}