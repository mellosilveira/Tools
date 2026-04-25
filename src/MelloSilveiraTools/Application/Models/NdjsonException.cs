namespace MelloSilveiraTools.Application.Models;

/// <summary>
/// Exception raised when an error occurs while writing a newline-delimited JSON stream.
/// </summary>
public class NdjsonException : AggregateException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NdjsonException"/> class.
    /// </summary>
    public NdjsonException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NdjsonException"/> class with a specific message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">The original exception captured during streaming.</param>
    public NdjsonException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NdjsonException"/> class with a default message and the provided inner exception.
    /// </summary>
    /// <param name="innerException">The original exception captured during streaming.</param>
    public NdjsonException(Exception innerException) : this("An error occurred while streaming data.", innerException) { }
}
