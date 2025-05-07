using System.ComponentModel.DataAnnotations;

namespace Pinetree.Shared.Model;

public class UserBlobInfo
{
    public int Id { get; set; }
    [MaxLength(256)]
    public string UserName { get; set; } = "";
    public string BlobName { get; set; } = "";
    public long SizeInBytes { get; set; }
    [MaxLength(50)]
    public string ContentType { get; set; } = "";
    public Guid PineconeGuid { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime UploadedAt { get; set; }
    public DateTime DeletedAt { get; set; }
}