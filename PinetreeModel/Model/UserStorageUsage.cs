using System.ComponentModel.DataAnnotations;

namespace Pinetree.Shared.Model;

public class UserStorageUsage
{
    public int Id { get; set; }
    [MaxLength(450)]
    public string UserId { get; set; } = "";
    public long TotalSizeInBytes { get; set; }
    public long QuotaInBytes { get; set; }
    public DateTime LastUpdated { get; set; }
}