using System.Collections;

namespace MelloSilveiraTools.Core.Pipelines.Models;

/// <summary>
/// Represents a collection of objects that have a common key, implementing <see cref="IGrouping{TKey, TElement}"/> and <see cref="IReadOnlyList{TElement}"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TElement">The type of elements in the group.</typeparam>
/// <param name="key">The key of the group.</param>
/// <param name="elements">The elements contained in the group.</param>
public sealed class Grouping<TKey, TElement>(TKey key, TElement[] elements) : IGrouping<TKey, TElement>, IReadOnlyList<TElement>
{
    /// <inheritdoc/>
    public TKey Key => key;

    /// <inheritdoc/>
    public int Count => elements.Length;

    /// <inheritdoc/>
    public TElement this[int index] => elements[index];

    /// <inheritdoc/>
    public IEnumerator<TElement> GetEnumerator() => ((IEnumerable<TElement>)elements).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();
}
