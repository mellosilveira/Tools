namespace MelloSilveiraTools.Application.Models;

public class NdjsonException : AggregateException
{
    public NdjsonException() { }
    public NdjsonException(string message, Exception innerException) : base(message, innerException) { }
    public NdjsonException(Exception innerException) : this("An error occurred while streaming data.", innerException) { }
}
