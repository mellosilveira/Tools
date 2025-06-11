namespace MelloSilveiraTools.ExtensionMethods;

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
    /// Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="sources" />.</typeparam>
    /// <param name="sources">An <see cref="IEnumerable{T}" /> to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The first element in <paramref name="sources" /> that passes the test specified by <paramref name="predicate" />.</returns>
    public static TSource FirstWithoutValidate<TSource>(this IEnumerable<TSource> sources, Func<TSource, bool> predicate)
    {
        foreach (TSource element in sources)
        {
            if (predicate(element))
                return element;
        }

        throw new InvalidOperationException("No elements were match with predicate.");
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
    public static bool IsNullOrEmpty<TSource>(this IEnumerable<TSource> sources) => sources is null || sources.IsEmpty();

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
    /// Gets the <see cref="Type"/> of <typeparamref name="TSource"/>.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="sources"></param>
    /// <returns></returns>
    public static Type GetSourceType<TSource>(this IEnumerable<TSource> sources)
    {
        return sources.FirstOrDefault()?.GetType() ?? typeof(TSource);
    }

    /// <summary>
    /// Performs an async iteration with <see cref="SemaphoreSlim"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="asyncAction"></param>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <returns></returns>
    public static async Task SemaphoreSlimForeachAsync<T>(this IEnumerable<T> source, Func<T, Task> asyncAction, int maxDegreeOfParallelism)
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
                    Console.WriteLine("Failed on async loop. " + ex.Message);
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
    /// Performs an async iteration with <see cref="SemaphoreSlim"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <returns></returns>
    public static async Task SemaphoreSlimForeachAsync<T>(this IEnumerable<T> source, Action<T> action, int maxDegreeOfParallelism)
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
                    Console.WriteLine("Failed on async loop. " + ex.Message);
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
    /// Gets all possible combinations from an <see cref="IEnumerable{T}"/> of list.
    /// </summary>
    /// <param name="lists"></param>
    /// <returns>List of double array with all possible combinations.</returns>
    public static IEnumerable<T[]> GetCombinations<T>(this IEnumerable<List<T>> lists) => GetCombinationsRecursive(lists.ToList(), new T[lists.Count()]);

    /// <summary>
    /// Gets all possible combinations from a list using recursion.
    /// </summary>
    /// <param name="lists"></param>
    /// <param name="current"></param>
    /// <param name="depth"></param>
    /// <returns>List of double array with all possible combinations.</returns>
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