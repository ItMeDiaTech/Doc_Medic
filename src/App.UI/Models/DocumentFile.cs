using System.ComponentModel;
using System.IO;
using Prism.Mvvm;

namespace App.UI.Models;

/// <summary>
/// Represents a document file in the processing queue with status tracking.
/// </summary>
public class DocumentFile : BindableBase
{
    private ProcessingStatus _status = ProcessingStatus.Pending;
    private string _statusMessage = string.Empty;
    private int _progress;
    private int _changes;

    public DocumentFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        
        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            Size = fileInfo.Length;
            LastModified = fileInfo.LastWriteTime;
        }
    }

    /// <summary>
    /// Gets the full file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the file name without directory path.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Gets the formatted file size as a human-readable string.
    /// </summary>
    public string SizeFormatted => FormatFileSize(Size);

    /// <summary>
    /// Gets the last modified date.
    /// </summary>
    public DateTime LastModified { get; }

    /// <summary>
    /// Gets or sets the processing status.
    /// </summary>
    public ProcessingStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets the processing progress (0-100).
    /// </summary>
    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, Math.Clamp(value, 0, 100));
    }

    /// <summary>
    /// Gets or sets the number of changes made to the document.
    /// </summary>
    public int Changes
    {
        get => _changes;
        set => SetProperty(ref _changes, value);
    }

    /// <summary>
    /// Gets whether the file exists on disk.
    /// </summary>
    public bool Exists => File.Exists(FilePath);

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        
        if (bytes == 0)
            return "0 B";

        var i = 0;
        decimal dValue = bytes;

        while (Math.Round(dValue, 1) >= 1000 && i < suffixes.Length - 1)
        {
            dValue /= 1024;
            i++;
        }

        return $"{dValue:N1} {suffixes[i]}";
    }
}

/// <summary>
/// Processing status enumeration for document files.
/// </summary>
public enum ProcessingStatus
{
    [Description("Pending")]
    Pending,
    
    [Description("Processing")]
    Processing,
    
    [Description("Completed")]
    Completed,
    
    [Description("Error")]
    Error,
    
    [Description("Skipped")]
    Skipped
}