namespace Admissions.Infrastructure.Options;

public sealed class DocumentStorageOptions
{
    public string DocumentsPath { get; set; } = "storage/documents";
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
}
