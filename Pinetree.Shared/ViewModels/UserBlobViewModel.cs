namespace Pinetree.Shared.ViewModels;

public class UserBlobViewModel
{
    public int Id { get; set; }
    public string BlobUrl { get; set; } = "";
    public string FileName { get; set; } = "";
    public long SizeInBytes { get; set; }
    public string ContentType { get; set; } = "";
    public Guid PineconeGuid { get; set; }
    public string PineconeTitle { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

public class UserBlobUploadRequest
{
    public string Extension { get; set; } = "";
    public Guid PineconeGuid { get; set; }
    public string Base64Data { get; set; } = "";
}

public class UserBlobUploadResponse
{
    public string Url { get; set; } = "";
    public int Id { get; set; }
    public long SizeInBytes { get; set; }
}
