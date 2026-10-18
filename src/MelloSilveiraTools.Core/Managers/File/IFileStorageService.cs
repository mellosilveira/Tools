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
    /// Creates or overwrites a file at the contextually resolved location and opens a stream for writing, returning both stream and initial metadata.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns>A tuple containing the writable <see cref="Stream"/> and the resolved <see cref="FileMetadata"/>.</returns>
    (Stream Stream, FileMetadata Metadata) Create(string context, string identifier, string fileName);

    /// <summary>
    /// Creates a timestamped file at the contextually resolved location and opens a stream for writing, returning both stream and initial metadata.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileExtension">File extension including dot (e.g., ".csv").</param>
    /// <param name="filePrefix">Optional business identifier prefix.</param>
    /// <returns>A tuple containing the writable <see cref="Stream"/> and the resolved <see cref="FileMetadata"/>.</returns>
    (Stream Stream, FileMetadata Metadata) CreateTimebased(string context, string identifier, string fileExtension, string? filePrefix = null);

    /// <summary>
    /// Opens a stream for reading from the specified physical path or storage URI.
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
    /// Checks whether the file exists at the specified physical path or storage URI.
    /// </summary>
    /// <param name="location">Physical path or storage URI.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    bool Exists(string location);

    // TODO: ADICIONAR MÉTODO GetMetadata A PARTIR DE "string location".

    /// <summary>
    /// Retrieves file metadata for a contextually resolved file location.
    /// </summary>
    /// <param name="context">Business domain context.</param>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="fileName">Target file name including extension.</param>
    /// <returns>A populated <see cref="FileMetadata"/> instance.</returns>
    FileMetadata GetMetadata(string context, string identifier, string fileName);

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
