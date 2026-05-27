using MelloSilveiraTools.Core.Logger;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Enumerable"/>.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="IEnumerable{T}" /> to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
    /// <returns><paramref name="defaultValue" /> if <paramref name="sources" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="sources" /> that passes the test specified by <paramref name="predicate" />.</returns>
    public static TSource? FirstOrDefaultWithoutValidate<TSource>(this IEnumerable<TSource> sources, Func<TSource, bool> predicate, TSource? defaultValue = default)
    {
        foreach (TSource element in sources)
        {
            if (predicate(element))
                return element;
        }

        return defaultValue;
    }

    /// <summary>
    /// Returns the first element of the sequence that satisfies a condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="IEnumerable{T}" /> to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The first element in <paramref name="sources" /> that passes the test specified by <paramref name="predicate" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no element in the source matches the predicate.</exception>
    public static TSource FirstWithoutValidate<TSource>(this IEnumerable<TSource> sources, Func<TSource, bool> predicate)
    {
        foreach (TSource element in sources)
        {
            if (predicate(element))
                return element;
        }

        throw new InvalidOperationException("No element matched the predicate.");
    }

    /// <summary>
    /// Indicates if <paramref name="sources"/> is empty.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="IEnumerable{T}" /> to check if is empty.</param>
    /// <returns>True, if <paramref name="sources"/> is empty. False, otherwise.</returns>
    public static bool IsEmpty<TSource>(this IEnumerable<TSource> sources) => !sources.Any();

    /// <summary>
    /// Indicates if <paramref name="sources"/> is null or empty.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="IEnumerable{T}" /> to check if is null or empty.</param>
    /// <returns>True, if <paramref name="sources"/> is null or empty. False, otherwise.</returns>
    public static bool IsNullOrEmpty<TSource>(this IEnumerable<TSource>? sources) => sources is null || sources.IsEmpty();

    /// <summary>
    /// Indicates if <paramref name="sources"/> is not null nor empty.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="IEnumerable{T}" /> to check if is not null nor empty.</param>
    /// <returns>True, if <paramref name="sources"/> is not null nor empty. False, otherwise.</returns>
    public static bool IsNotNullOrEmpty<TSource>(this IEnumerable<TSource> sources) => sources is not null && sources.Any();

    /// <summary>
    /// If <paramref name="element"/> is not null, adds it in <paramref name="sources"/>
    /// </summary>
    /// <param name="sources">A list of double to add an <paramref name="element"/>.</param>
    /// <param name="element">The element to be added in <paramref name="sources"/> if not null.</param>
    /// <returns>The <paramref name="sources"/> received to add an <paramref name="element"/>.</returns>
    public static List<double> FluentAddIfNotNull(this List<double> sources, double? element)
    {
        if (element != null)
            sources.Add(element.Value);

        return sources;
    }

    /// <summary>
    /// If <paramref name="collection"/> is not null, adds the <paramref name="collection"/> of the given collection to the end of this <paramref name="sources"/>. 
    /// If required, the capacity of the list is increased to twice the previous capacity or the new size, whichever is larger.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="List{T}" /> to add an <paramref name="collection"/>.</param>
    /// <param name="collection">The object to add to the <see cref="List{T}"/>.</param>
    /// <returns>The <paramref name="sources"/> received to add an <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException">The <see cref="List{T}" /> is null.</exception>
    public static ICollection<TSource> FluentAddRangeIfNotNull<TSource>(this List<TSource> sources, IEnumerable<TSource> collection)
    {
        if (collection != null)
            sources.AddRange(collection);

        return sources;
    }

    /// <summary>
    /// Adds an item to the <see cref="ICollection{T}"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="ICollection{T}" /> to add an <paramref name="element"/>.</param>
    /// <param name="element">The object to add to the <see cref="ICollection{T}"/>.</param>
    /// <returns>The <paramref name="sources"/> received to add an <paramref name="element"/>.</returns>
    /// <exception cref="NotSupportedException">The <see cref="ICollection{T}" /> is read-only.</exception>
    public static List<TSource> FluentAdd<TSource>(this List<TSource> sources, TSource element)
    {
        sources.Add(element);
        return sources;
    }

    /// <summary>
    /// Adds the <paramref name="collection"/> of the given collection to the end of this <paramref name="sources"/>. 
    /// If required, the capacity of the list is increased to twice the previous capacity or the new size, whichever is larger.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="List{T}" /> to add an <paramref name="collection"/>.</param>
    /// <param name="collection">The object to add to the <see cref="List{T}"/>.</param>
    /// <returns>The <paramref name="sources"/> received to add an <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException">The <see cref="List{T}" /> is null.</exception>
    public static ICollection<TSource> FluentAddRange<TSource>(this List<TSource> sources, IEnumerable<TSource> collection)
    {
        sources.AddRange(collection);
        return sources;
    }

    /// <summary>
    /// Gets the runtime <see cref="Type"/> of the actual elements stored in <paramref name="sources"/>.
    /// When the sequence has at least one element, the runtime type of that element is returned
    /// (useful for polymorphic collections where <typeparamref name="TSource"/> may be a base type
    /// or interface). When the sequence is empty, the static <typeparamref name="TSource"/> is
    /// returned as a fallback.
    /// </summary>
    /// <typeparam name="TSource">The static element type of the sequence.</typeparam>
    /// <param name="sources">The sequence whose element type should be inspected.</param>
    /// <returns>The runtime type of the first element, or <c>typeof(<typeparamref name="TSource"/>)</c> when the sequence is empty.</returns>
    public static Type GetSourceType<TSource>(this IEnumerable<TSource> sources) => sources.FirstOrDefault()?.GetType() ?? typeof(TSource);

    /// <summary>
    /// Iterates over <paramref name="source"/> launching <paramref name="asyncAction"/> for every item,
    /// while limiting concurrency through a <see cref="SemaphoreSlim"/> bounded by <paramref name="maxDegreeOfParallelism"/>.
    /// Items are dispatched in source order, but their actual completion order is non-deterministic
    /// because they execute in parallel. Exceptions raised inside <paramref name="asyncAction"/> are
    /// logged to <see cref="Console"/> and swallowed so the iteration continues; cancellation is not
    /// observed by this overload.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="source">The sequence to iterate. Must not be <see langword="null"/>.</param>
    /// <param name="asyncAction">The asynchronous delegate invoked for each item.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of <paramref name="asyncAction"/> invocations allowed to run concurrently. Must be greater than zero.</param>
    /// <param name="logger"></param>
    /// <returns>A task that completes when every dispatched <paramref name="asyncAction"/> has finished.</returns>
    public static async Task ForeachAsync<T>(this IEnumerable<T> source, Func<T, Task> asyncAction, int maxDegreeOfParallelism, ILogger logger)
    {
        using SemaphoreSlim semaphoreSlim = new(maxDegreeOfParallelism, maxDegreeOfParallelism);

        List<Task> tasks = [];
        foreach (T item in source)
        {
            await semaphoreSlim.WaitAsync().ConfigureAwait(false);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await asyncAction(item);
                }
                catch (Exception ex)
                {
                    logger.Error("Failed on async loop.", ex, new Dictionary<string, object?> { { "Item", item } });
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Iterates over <paramref name="source"/> launching <paramref name="asyncAction"/> for every item,
    /// while limiting concurrency through a <see cref="SemaphoreSlim"/> bounded by <paramref name="maxDegreeOfParallelism"/>.
    /// Items are dispatched in source order, but their actual completion order is non-deterministic
    /// because they execute in parallel. Exceptions raised inside <paramref name="asyncAction"/> are
    /// logged to <see cref="Console"/> and swallowed so the iteration continues; cancellation is not
    /// observed by this overload.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="source">The sequence to iterate. Must not be <see langword="null"/>.</param>
    /// <param name="asyncAction">The asynchronous delegate invoked for each item.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of <paramref name="asyncAction"/> invocations allowed to run concurrently. Must be greater than zero.</param>
    /// <param name="logger"></param>
    /// <returns>A task that completes when every dispatched <paramref name="asyncAction"/> has finished.</returns>
    public static async Task ForeachAsync<T>(this IEnumerable<T> source, Func<T, Task> asyncAction, int maxDegreeOfParallelism)
    {
        using SemaphoreSlim semaphoreSlim = new(maxDegreeOfParallelism, maxDegreeOfParallelism);

        List<Task> tasks = [];
        foreach (T item in source)
        {
            await semaphoreSlim.WaitAsync().ConfigureAwait(false);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await asyncAction(item);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Iterates over <paramref name="source"/> dispatching <paramref name="action"/> for every item on
    /// the thread pool, while limiting concurrency through a <see cref="SemaphoreSlim"/> bounded by
    /// <paramref name="maxDegreeOfParallelism"/>. Items are scheduled in source order but completion
    /// order is non-deterministic. Exceptions raised inside <paramref name="action"/> are logged to
    /// <see cref="Console"/> and swallowed so the iteration continues; cancellation is not observed
    /// by this overload.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="source">The sequence to iterate. Must not be <see langword="null"/>.</param>
    /// <param name="action">The synchronous delegate invoked for each item on the thread pool.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of <paramref name="action"/> invocations allowed to run concurrently. Must be greater than zero.</param>
    /// <param name="logger"></param>
    /// <returns>A task that completes when every scheduled invocation of <paramref name="action"/> has finished.</returns>
    public static async Task ForeachAsync<T>(this IEnumerable<T> source, Action<T> action, int maxDegreeOfParallelism, ILogger logger)
    {
        using SemaphoreSlim semaphoreSlim = new(maxDegreeOfParallelism, maxDegreeOfParallelism);

        List<Task> tasks = [];
        foreach (T item in source)
        {
            await semaphoreSlim.WaitAsync().ConfigureAwait(false);

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    action(item);
                }
                catch (Exception ex)
                {
                    logger.Error("Failed on async loop.", ex, new Dictionary<string, object?> { { "Item", item } });
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Iterates over <paramref name="source"/> dispatching <paramref name="action"/> for every item on
    /// the thread pool, while limiting concurrency through a <see cref="SemaphoreSlim"/> bounded by
    /// <paramref name="maxDegreeOfParallelism"/>. Items are scheduled in source order but completion
    /// order is non-deterministic. Exceptions raised inside <paramref name="action"/> are logged to
    /// <see cref="Console"/> and swallowed so the iteration continues; cancellation is not observed
    /// by this overload.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="source">The sequence to iterate. Must not be <see langword="null"/>.</param>
    /// <param name="action">The synchronous delegate invoked for each item on the thread pool.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of <paramref name="action"/> invocations allowed to run concurrently. Must be greater than zero.</param>
    /// <returns>A task that completes when every scheduled invocation of <paramref name="action"/> has finished.</returns>
    public static async Task ForeachAsync<T>(this IEnumerable<T> source, Action<T> action, int maxDegreeOfParallelism)
    {
        using SemaphoreSlim semaphoreSlim = new(maxDegreeOfParallelism, maxDegreeOfParallelism);

        List<Task> tasks = [];
        foreach (T item in source)
        {
            await semaphoreSlim.WaitAsync().ConfigureAwait(false);

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    action(item);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Iterates synchronously over <paramref name="source"/> invoking <paramref name="action"/> for each item.
    /// Exceptions thrown by <paramref name="action"/> are logged and swallowed so the iteration continues.
    /// </summary>
    public static void Foreach<T>(this IEnumerable<T> source, Action<T> action, ILogger logger)
    {
        foreach (T item in source)
        {
            try
            {
                action(item);
            }
            catch (Exception ex)
            {
                logger.Error("Failed on loop.", ex, new Dictionary<string, object?> { { "Item", item } });
            }
        }
    }

    /// <summary>
    /// Iterates synchronously over <paramref name="source"/> invoking <paramref name="action"/> for each item.
    /// Exceptions thrown by <paramref name="action"/> are logged and swallowed so the iteration continues.
    /// </summary>
    public static void Foreach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Builds the Cartesian product of the supplied lists, yielding every possible combination
    /// formed by picking exactly one element from each list, in list order. The number of
    /// combinations produced equals the product of the sizes of the inner lists.
    /// </summary>
    /// <param name="lists">The collection of lists to combine. Each emitted array has one element per inner list, ordered to match the order of <paramref name="lists"/>.</param>
    /// <returns>A lazy sequence of arrays where each array is one combination drawn from <paramref name="lists"/>.</returns>
    public static IEnumerable<T[]> GetCombinations<T>(this IEnumerable<List<T>> lists)
    {
        var materialized = lists.ToList();
        return GetCombinationsRecursive(materialized, new T[materialized.Count]);
    }

    /// <summary>
    /// Recursive worker that walks <paramref name="lists"/> depth-first, fixing one element of
    /// <paramref name="current"/> per level until <paramref name="depth"/> reaches the number of
    /// lists, at which point a clone of <paramref name="current"/> is yielded.
    /// </summary>
    /// <param name="lists">The lists being combined.</param>
    /// <param name="current">The reusable buffer holding the partial combination at the current recursion level.</param>
    /// <param name="depth">The zero-based index of the list currently being expanded; recursion stops when it equals <c>lists.Count</c>.</param>
    /// <returns>A lazy sequence of arrays, each one a complete combination drawn from <paramref name="lists"/>.</returns>
    private static IEnumerable<T[]> GetCombinationsRecursive<T>(List<List<T>> lists, T[] current, int depth = 0)
    {
        if (depth == lists.Count)
        {
            yield return (T[])current.Clone();
            yield break;
        }

        List<T> currentValues = lists[depth];
        for (int i = 0; i < currentValues.Count; i++)
        {
            current[depth] = currentValues[i];
            foreach (var combination in GetCombinationsRecursive(lists, current, depth + 1))
            {
                yield return combination;
            }
        }
    }
}