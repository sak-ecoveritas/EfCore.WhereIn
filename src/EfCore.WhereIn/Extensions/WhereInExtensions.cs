using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EfCore.WhereIn
{
    /// <summary>
    /// Provides extension methods for filtering entities using inline SQL IN and NOT IN clauses in Entity Framework Core.
    /// </summary>
    public static class WhereInExtensions
    {
        /// <summary>
        /// The maximum number of parameters allowed in a SQL Server IN clause (2100).
        /// </summary>
        public const int SqlServerInClauseLimit = 2100;

        /// <summary>
        /// Filters a sequence of entities based on whether a specified property value exists in a given collection.
        /// Forces EF Core to generate an inline SQL IN (...) clause instead of using OPENJSON.
        /// Throws an exception if the collection exceeds SQL Server's parameter limit.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TValue">The type of the property to filter on.</typeparam>
        /// <param name="source">The source queryable sequence.</param>
        /// <param name="selector">An expression selecting the property to filter on.</param>
        /// <param name="values">A collection of values to match against.</param>
        /// <returns>A filtered <see cref="IQueryable{T}"/> containing only entities whose property value is in <paramref name="values"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/>, <paramref name="selector"/>, or <paramref name="values"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="values"/> exceeds SQL Server's IN clause parameter limit.</exception>
        public static IQueryable<T> WhereIn<T, TValue>(
            this IQueryable<T> source,
            Expression<Func<T, TValue>> selector,
            IEnumerable<TValue> values)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (values == null) throw new ArgumentNullException(nameof(values));

            var array = values.Distinct().ToArray();

            if (array.Length == 0)
            {
                return source.Where(_ => false);
            }

            if (array.Length > SqlServerInClauseLimit)
            {
                throw new ArgumentException($"The number of values ({array.Length}) exceeds SQL Server's IN clause limit of {SqlServerInClauseLimit}.", nameof(values));
            }

            return source.Where(BuildInPredicate(selector, array));
        }

        /// <summary>
        /// Filters a sequence of entities based on whether a specified property value does NOT exist in a given collection.
        /// Forces EF Core to generate an inline SQL NOT IN (...) clause instead of using OPENJSON.
        /// Throws an exception if the collection exceeds SQL Server's parameter limit.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TValue">The type of the property to filter on.</typeparam>
        /// <param name="source">The source queryable sequence.</param>
        /// <param name="selector">An expression selecting the property to filter on.</param>
        /// <param name="values">A collection of values to exclude.</param>
        /// <returns>A filtered <see cref="IQueryable{T}"/> containing only entities whose property value is NOT in <paramref name="values"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/>, <paramref name="selector"/>, or <paramref name="values"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="values"/> exceeds SQL Server's IN clause parameter limit.</exception>
        public static IQueryable<T> WhereNotIn<T, TValue>(
            this IQueryable<T> source,
            Expression<Func<T, TValue>> selector,
            IEnumerable<TValue> values)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (values == null) throw new ArgumentNullException(nameof(values));

            var array = values.Distinct().ToArray();

            if (array.Length == 0)
            {
                return source;
            }

            if (array.Length > SqlServerInClauseLimit)
            {
                throw new ArgumentException($"The number of values ({array.Length}) exceeds SQL Server's IN clause limit of {SqlServerInClauseLimit}.", nameof(values));
            }

            return source.Where(BuildNotInPredicate(selector, array));
        }

        /// <summary>
        /// Builds a predicate for an IN clause using a HashSet for the given selector and values.
        /// </summary>
        /// <remarks>
        /// This method constructs an expression tree that, when executed, checks if the property selected by <paramref name="selector"/>
        /// exists in the provided <paramref name="array"/>. It does this by creating a HashSet from the array and calling its Contains method
        /// for each entity. This allows EF Core to translate the predicate into an efficient SQL IN clause.
        /// </remarks>
        internal static Expression<Func<T, bool>> BuildInPredicate<T, TValue>(Expression<Func<T, TValue>> selector, TValue[] array)
        {
            // Create a constant expression representing a HashSet containing all values to match
            var hashSet = Expression.Constant(new HashSet<TValue>(array));
            // Get the MethodInfo for HashSet<TValue>.Contains(TValue)
            var containsMethod = typeof(HashSet<TValue>).GetMethod(nameof(HashSet<TValue>.Contains), new[] { typeof(TValue) });
            // Build an expression that calls hashSet.Contains(selector.Body)
            var body = Expression.Call(hashSet, containsMethod!, selector.Body);
            // Return a lambda expression: entity => hashSet.Contains(entity.Property)
            return Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
        }

        /// <summary>
        /// Builds a predicate for a NOT IN clause using a HashSet for the given selector and values.
        /// </summary>
        /// <remarks>
        /// This method constructs an expression tree that, when executed, checks if the property selected by <paramref name="selector"/>
        /// does NOT exist in the provided <paramref name="array"/>. It creates a HashSet from the array and negates the Contains method call.
        /// This allows EF Core to translate the predicate into an efficient SQL NOT IN clause.
        /// </remarks>
        private static Expression<Func<T, bool>> BuildNotInPredicate<T, TValue>(Expression<Func<T, TValue>> selector, TValue[] array)
        {
            // Create a constant expression representing a HashSet containing all values to exclude
            var hashSet = Expression.Constant(new HashSet<TValue>(array));
            // Get the MethodInfo for HashSet<TValue>.Contains(TValue)
            var containsMethod = typeof(HashSet<TValue>).GetMethod(nameof(HashSet<TValue>.Contains), new[] { typeof(TValue) });
            // Build an expression that calls hashSet.Contains(selector.Body)
            var containsCall = Expression.Call(hashSet, containsMethod!, selector.Body);
            // Negate the contains call to represent NOT IN
            var notContains = Expression.Not(containsCall);
            // Return a lambda expression: entity => !hashSet.Contains(entity.Property)
            return Expression.Lambda<Func<T, bool>>(notContains, selector.Parameters);
        }
    }
}
