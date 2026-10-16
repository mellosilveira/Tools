namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Storage-agnostic abstraction for file and directory management, location resolution, and stream handling.
/// Encapsulates knowledge of internal canonical directory layouts ("storage/{context}/{identifier}/")
/// independent of the underlying storage technology (e.g., Local File System, Blob Storage, Distributed File System).
/// Implementations are registered in dependency injection as keyed services by <see cref="StorageProvider"/>.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Resolves the canonical directory path or container URI for a given domain context and unique identifier.
    /// Default structure: "{BaseDirectory}/{context}/{identifier}/".
    /// </summary>
    /// <param name="context">Business domain context (e.g., "ExperimentalData", "SensitivityAnalysis").</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <returns>The resolved directory path or container URI.</returns>
    string GetDirectory(string context, string identifier);

    /// <summary>
    /// Resolves the full path or URI for a specific file within a domain context and identifier partition.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns>The resolved full location or URI.</returns>
    string ResolvePath(string context, string identifier, string fileName);

    /// <summary>
    /// Resolves a timestamped full path or URI within a domain context and identifier partition,
    /// formatted as "{filePrefix}_{timestamp}{fileExtension}".
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="filePrefix">Business identifier prefix.</param>
    /// <param name="fileExtension">File extension including dot (e.g., ".csv").</param>
    /// <returns>The resolved timestamped full location or URI.</returns>
    string ResolveTimebasedPath(string context, string identifier, string filePrefix, string fileExtension);

    /// <summary>
    /// Opens a stream for writing to the specified location, automatically creating parent directories or containers if needed.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <returns>A writable <see cref="Stream"/>.</returns>
    Stream OpenWrite(string location);

    /// <summary>
    /// Opens a stream for writing to a contextually resolved file location.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns>A writable <see cref="Stream"/>.</returns>
    Stream OpenWrite(string context, string identifier, string fileName);

    /// <summary>
    /// Opens a stream for reading from the specified location.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <returns>A readable <see cref="Stream"/>.</returns>
    Stream OpenRead(string location);

    /// <summary>
    /// Opens a stream for reading from a contextually resolved file location.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns>A readable <see cref="Stream"/>.</returns>
    Stream OpenRead(string context, string identifier, string fileName);

    /// <summary>
    /// Checks whether the file exists at the specified location.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    bool Exists(string location);

    /// <summary>
    /// Checks whether the file exists within the specified context and identifier partition.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    bool Exists(string context, string identifier, string fileName);

    /// <summary>
    /// Retrieves file metadata (file name, directory, full path, byte size, timestamps) for the specified location.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <returns>A populated <see cref="FileMetadata"/> instance.</returns>
    FileMetadata GetMetadata(string location);

    /// <summary>
    /// Retrieves file metadata for a contextually resolved file location.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns>A populated <see cref="FileMetadata"/> instance.</returns>
    FileMetadata GetMetadata(string context, string identifier, string fileName);

    /// <summary>
    /// Deletes the file at the specified location if it exists.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <returns><c>true</c> if the file was deleted; otherwise, <c>false</c>.</returns>
    bool Delete(string location);

    /// <summary>
    /// Deletes the file within the specified context and identifier partition if it exists.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns><c>true</c> if the file was deleted; otherwise, <c>false</c>.</returns>
    bool Delete(string context, string identifier, string fileName);

    /// <summary>
    /// Lists all file metadata for files residing within the specified context and identifier partition.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <returns>A collection of <see cref="FileMetadata"/> items.</returns>
    IReadOnlyList<FileMetadata> ListFiles(string context, string identifier);
}
