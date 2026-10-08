namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Contract for structured file, report, and export generation on disk.
/// Standardizes document naming by appending chronological timestamps (date and time),
/// preventing unintended overwriting of mission-critical files and preserving an auditable operational trail.
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// Generates the absolute file path combining target directory, business prefix, and generation timestamp.
    /// </summary>
    /// <param name="fileUri">Destination directory for saving the document.</param>
    /// <param name="filePrefix">Business identifier prefix (e.g. "SalesReport", "DataExport").</param>
    /// <param name="fileExtension">Target file extension (e.g. ".csv", ".json", ".txt").</param>
    /// <returns>The complete absolute file path.</returns>
    string BuildTimebasedFullName(string fileUri, string filePrefix, string fileExtension);

    /// <summary>
    /// Builds organizational file metadata (directory and timestamped name) to simplify tracking.
    /// </summary>
    /// <param name="fileUri">Destination directory.</param>
    /// <param name="filePrefix">Business identifier prefix.</param>
    /// <param name="fileExtension">Target file extension.</param>
    /// <returns>A structured file data record.</returns>
    FileData BuildTimebasedFile(string fileUri, string filePrefix, string fileExtension);

    /// <summary>
    /// Prepares a physical file handle and guarantees destination directory existence, creating it automatically if missing.
    /// </summary>
    /// <param name="fileUri">Destination directory.</param>
    /// <param name="filePrefix">Business identifier prefix.</param>
    /// <param name="fileExtension">Target file extension.</param>
    /// <returns>A <see cref="FileInfo"/> ready for safe disk writing with verified directory path.</returns>
    FileInfo BuildTimebasedFileInfo(string fileUri, string filePrefix, string fileExtension);
}