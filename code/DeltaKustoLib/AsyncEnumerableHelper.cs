using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib
{
    public static class AsyncEnumerableHelper
    {
        public static async Task<IEnumerable<T>> ToEnumerableAsync<T>(this IAsyncEnumerable<T> asyncCollection)
        {
            var list = new List<T>();

            await foreach (var item in asyncCollection)
            {
                list.Add(item);
            }

            return list;
        }
    }
}