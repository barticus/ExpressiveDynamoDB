using System;
using System.Collections.Generic;

namespace ExpressiveDynamoDB.Extensions
{
    public static class DictionaryExtensions
    {
        public enum OnDuplicateKey { THROW, SKIP, REPLACE }

        public static void AddRange<T, S>(this Dictionary<T, S> source, IEnumerable<KeyValuePair<T, S>> collection, OnDuplicateKey onDuplicateKey = OnDuplicateKey.REPLACE)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key) || onDuplicateKey == OnDuplicateKey.THROW)
                {
                    source.Add(item.Key, item.Value);
                }
                else
                {
                    switch(onDuplicateKey)
                    {
                        case OnDuplicateKey.REPLACE:
                            source[item.Key] = item.Value;
                            break;
                        case OnDuplicateKey.SKIP:
                        default:
                            break;
                    }
                }
            }
        }
    }
}