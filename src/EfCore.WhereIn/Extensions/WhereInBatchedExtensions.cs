using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EfCore.WhereIn
{
    /// <summary>
    /// Provides an extension method for filtering entities using inline SQL IN clauses in Entity Framework Core,
    /// supporting arbitrarily large collections by batching into groups of 2100 or fewer.
    /// </summary>
    public static class WhereInBatchedExtensions
    {
        /// <summary>
        /// Filters a sequence of entities based on whether a specified property value exists in a given collection,
        /// supporting arbitrarily large collections by batching into groups of 2100 or fewer.
        /// This avoids SQL Server's parameter limit but may result in complex queries and performance cost.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TValue">The type of the property to filter on.</typeparam>
        /// <param name="source">The source queryable sequence.</param>
        /// <param name="selector">An expression selecting the property to filter on.</param>
        /// <param name="values">A collection of values to match against (no limit).</param>
        /// <returns>
        /// A filtered <see cref="IQueryable{T}"/> containing only entities whose property value is in <paramref name="values"/>.
        /// </returns>
        /// <remarks>
        /// This method splits the input values into batches of 2100 or fewer and combines the resulting predicates with OR.
        /// For very large collections, this can result in large/complex SQL and may impact performance.
        /// </remarks>
        public static IQueryable<T> WhereInBatched<T, TValue>(
            this IQueryable<T> source,
            Expression<Func<T, TValue>> selector,
            IEnumerable<TValue> values)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (values == null) throw new ArgumentNullException(nameof(values));

            var array = values.Distinct().ToArray();
            if (array.Length == 0)
                return source.Where(_ => false);

            var batches = array
                .Select((v, i) => new { v, i })
                .GroupBy(x => x.i / WhereInExtensions.SqlServerInClauseLimit)
                .Select(g => g.Select(x => x.v).ToArray())
                .ToArray();

            Expression<Func<T, bool>>? combined = null;
            foreach (var batch in batches)
            {
                var predicate = WhereInExtensions.BuildInPredicate(selector, batch);
                combined = combined == null
                    ? predicate
                    : OrElse(combined, predicate);
            }

            return source.Where(combined!);
        }

        /// <summary>
        /// Combines two predicates with a logical OR.
        /// </summary>
        private static Expression<Func<T, bool>> OrElse<T>(
            Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.OrElse(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter));
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
