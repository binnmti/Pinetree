using System.ComponentModel.DataAnnotations;

namespace Pinetree.Shared.Model;

public class UserBlobInfo
{
    public int Id { get; set; }
    [MaxLength(450)]
    public string UserId { get; set; } = "";
    public string BlobUrl { get; set; } = "";
    public string BlobName { get; set; } = "";
    public long SizeInBytes { get; set; }
    [MaxLength(50)]
    public string ContentType { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}