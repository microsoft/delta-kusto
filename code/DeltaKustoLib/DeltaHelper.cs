using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib
{
    internal static class DeltaHelper
    {
        public static IEnumerable<M> GetCreated<M, K>(
            IEnumerable<M> currents,
            IEnumerable<M> targets,
            Func<M, K> keyExtractor)
            where K : notnull
        {
            var currentKeys = currents.Select(m => keyExtractor(m));
            var targetKeys = targets.Select(m => keyExtractor(m));
            var createdKeys = targetKeys.Except(currentKeys).ToImmutableHashSet();

            foreach (var t in targets)
            {
                if (createdKeys.Contains(keyExtractor(t)))
                {
                    yield return t;
                }
            }
        }

        public static IEnumerable<(M before, M after)> GetUpdated<M, K>(
            IEnumerable<M> currents,
            IEnumerable<M> targets,
            Func<M, K> keyExtractor)
            where K : notnull
        {
            var currentKeys = currents.Select(m => keyExtractor(m));
            var currentMap = currents.ToImmutableDictionary(m => keyExtractor(m));
            var targetKeys = targets.Select(m => keyExtractor(m));
            var updateKeys = targetKeys.Intersect(currentKeys).ToImmutableHashSet();

            foreach (var t in targets)
            {
                var key = keyExtractor(t);

                if (updateKeys.Contains(key))
                {
                    var current = currentMap[key];

                    if (!current!.Equals(t))
                    {
                        yield return (current, t);
                    }
                }
            }
        }

        public static IEnumerable<M> GetDropped<M, K>(
            IEnumerable<M> currents,
            IEnumerable<M> targets,
            Func<M, K> keyExtractor)
            where K : notnull
        {
            var currentKeys = currents.Select(m => keyExtractor(m));
            var targetKeys = targets.Select(m => keyExtractor(m));
            var droppedKeys = currentKeys.Except(targetKeys).ToImmutableHashSet();

            foreach (var t in currents)
            {
                if (droppedKeys.Contains(keyExtractor(t)))
                {
                    yield return t;
                }
            }
        }
    }
}