using System.Buffers;

namespace MelloSilveiraTools.Domain.Models;

/// <summary>
/// Provides a high-performance, allocation-free string builder using <see cref="Span{T}"/>
/// with memory pooling support. This ref struct is stack-allocated and should not be boxed.
/// </summary>
/// <remarks>
/// <para>
/// Designed for scenarios requiring minimal memory allocation and maximum performance.
/// Rents buffers from <see cref="ArrayPool{T}"/> to reduce GC pressure. Must be disposed
/// after use to return buffers to the pool.
/// </para>
/// <para>
/// ⚠️ Important: As a ref struct, this type has stack-only semantics and cannot be:
/// <list type="bullet">
/// <item>Used as a field in non-ref structs or classes</item>
/// <item>Boxed or stored in heap-allocated collections</item>
/// <item>Used in async methods across await boundaries</item>
/// </list>
/// </para>
/// </remarks>
public ref struct SpanStringBuilder : IDisposable
{
    private int _bufferPosition;
    private char[]? _arrayFromPool;
    private Span<char> _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanStringBuilder"/>.
    /// Rents an initial buffer of 256 characters from the shared array pool.
    /// </summary>
    public SpanStringBuilder()
    {
        _bufferPosition = 0;
        _arrayFromPool = ArrayPool<char>.Shared.Rent(256);
        _buffer = _arrayFromPool;
    }

    /// <summary>
    /// Gets a reference to the character at the specified position in the buffer.
    /// </summary>
    /// <param name="index">The zero-based index of the character</param>
    /// <returns>A reference to the character at position <paramref name="index"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the current buffer bounds.
    /// </exception>
    public ref char this[int index] => ref _buffer[index];

    /// <summary>
    /// Appends a single character to the buffer.
    /// </summary>
    /// <param name="value">The character to append</param>
    /// <returns>A reference to this instance for method chaining</returns>
    public SpanStringBuilder Append(char value)
    {
        if (_bufferPosition >= _buffer.Length)
            Grow(_buffer.Length * 2);

        _buffer[_bufferPosition] = value;
        _bufferPosition++;
        return this;
    }

    /// <summary>
    /// Appends a sequence of characters to the buffer.
    /// </summary>
    /// <param name="value">The characters to append</param>
    /// <returns>A reference to this instance for method chaining</returns>
    public SpanStringBuilder Append(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return this;

        if (_bufferPosition + value.Length > _buffer.Length)
            Grow(_bufferPosition + value.Length);

        value.CopyTo(_buffer[_bufferPosition..]);
        _bufferPosition += value.Length;
        return this;
    }

    /// <summary>
    /// Appends a sequence of characters followed by the default line terminator.
    /// </summary>
    /// <param name="value">The characters to append</param>
    /// <returns>A reference to this instance for method chaining</returns>
    public SpanStringBuilder AppendLine(ReadOnlySpan<char> value) => Append(value).Append(Environment.NewLine);

    /// <summary>
    /// Converts the current buffer contents to a new string.
    /// </summary>
    /// <returns>A string representing the characters in the buffer</returns>
    public override readonly string ToString() => new(_buffer[.._bufferPosition]);

    /// <summary>
    /// Returns the rented buffer to the array pool and resets the builder.
    /// </summary>
    /// <remarks>
    /// This method should be called when the builder is no longer needed.
    /// Failing to dispose will cause memory leaks in the array pool.
    /// </remarks>
    public void Dispose()
    {
        if (_arrayFromPool is not null)
        {
            ArrayPool<char>.Shared.Return(_arrayFromPool);
            _arrayFromPool = null;
            _buffer = default;
            _bufferPosition = 0;
        }
    }

    /// <summary>
    /// Expands the buffer capacity when needed
    /// </summary>
    /// <param name="requiredCapacity">Minimum required capacity after growth</param>
    private void Grow(int requiredCapacity)
    {
        int newCapacity = Math.Max(requiredCapacity, _buffer.Length * 2);

        // Rent new buffer from pool
        char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);

        // Copy existing content
        _buffer[.._bufferPosition].CopyTo(newArray);

        // Store reference to old buffer for return
        char[]? oldArray = _arrayFromPool;

        // Update references
        _arrayFromPool = newArray;
        _buffer = newArray;

        // Return old buffer to pool
        if (oldArray is not null)
        {
            ArrayPool<char>.Shared.Return(oldArray);
        }
    }
}