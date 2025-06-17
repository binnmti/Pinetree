namespace Pinetree.Shared.ViewModels;

public class UserStorageUsageViewModel
{
    public string UserName { get; set; } = "";
    public long TotalSizeInBytes { get; set; }
    public long QuotaInBytes { get; set; }
    public DateTime LastUpdated { get; set; }
    public double UsagePercentage => QuotaInBytes > 0 ? (double)TotalSizeInBytes / QuotaInBytes * 100 : 0;
    public long RemainingBytes => Math.Max(0, QuotaInBytes - TotalSizeInBytes);
    public bool IsOverQuota => TotalSizeInBytes > QuotaInBytes;
}

public class UserStorageUsageUpdateRequest
{
    public string UserName { get; set; } = "";
    public long TotalSizeInBytes { get; set; }
    public long QuotaInBytes { get; set; }
}
