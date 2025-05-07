using System.ComponentModel.DataAnnotations;

namespace Pinetree.Shared.Model;

public class UserStorageUsage
{
    public int Id { get; set; }
    [MaxLength(256)]
    public string UserName { get; set; } = "";
    public long TotalSizeInBytes { get; set; }
    public long QuotaInBytes { get; set; }
    public DateTime LastUpdated { get; set; }
}