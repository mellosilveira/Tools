using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;
using System.IO.Compression;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Serialization;

/// <summary>
/// Provides high-performance Brotli compression and decompression routines for serializing
/// step input and output payloads into compact PostgreSQL <c>bytea</c> columns.
/// </summary>
public static class StepPayloadCompressor
{
    /// <summary>
    /// Serializes and compresses the supplied payload into a Brotli-compressed byte array using 7 significant figures JSON formatting.
    /// </summary>
    /// <typeparam name="T">The payload type to serialize.</typeparam>
    /// <param name="payload">The object instance to compress.</param>
    /// <returns>A compressed byte array suitable for storage in a PostgreSQL <c>bytea</c> column.</returns>
    public static byte[] Compress<T>(T payload)
    {
        if (payload is null)
        {
            return [];
        }

        using MemoryStream memoryStream = new();
        using (BrotliStream brotliStream = new(memoryStream, CompressionLevel.Fastest, leaveOpen: true))
        {
            JsonSerializer.Serialize(brotliStream, payload, payload.GetType(), OptimizationJsonOptions.SignificantFigures7);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Decompresses and deserializes a Brotli-compressed byte array into an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected target payload type.</typeparam>
    /// <param name="compressedData">The compressed byte array retrieved from storage or database.</param>
    /// <returns>The deserialized object instance, or null if the payload was empty.</returns>
    public static T? Decompress<T>(byte[]? compressedData)
    {
        if (compressedData is null || compressedData.Length == 0)
        {
            return default;
        }

        using MemoryStream memoryStream = new(compressedData);
        using BrotliStream brotliStream = new(memoryStream, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<T>(brotliStream, OptimizationJsonOptions.SignificantFigures7);
    }

    /// <summary>
    /// Decompresses a Brotli-compressed byte array into an inspected <see cref="JsonDocument"/> representation.
    /// </summary>
    /// <param name="compressedData">The compressed byte array retrieved from storage or database.</param>
    /// <returns>The parsed JSON document, or null if the payload was empty.</returns>
    public static JsonDocument? DecompressToDocument(byte[]? compressedData)
    {
        if (compressedData is null || compressedData.Length == 0)
        {
            return null;
        }

        using MemoryStream memoryStream = new(compressedData);
        using BrotliStream brotliStream = new(memoryStream, CompressionMode.Decompress);
        return JsonDocument.Parse(brotliStream);
    }
}
