using System.ComponentModel.DataAnnotations;

namespace Pinetree.Model;

public class TryUser
{
    [Key]
    public string Id { get; set; } = "";

    public required int CreateCount { get; set; }
    public required int FileCount { get; set; }

    [MaxLength(16)]
    public required string Ip { get; set; }

    public required bool IsRegister { get; set; }
    public required bool IsConfirmed { get; set; }
    public required DateTime Create { get; set; } = DateTime.UtcNow;
    public required DateTime Update { get; set; } = DateTime.UtcNow;
}
