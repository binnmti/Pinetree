namespace Pinetree.Common;

public static class Roles
{
    public const string Free = "Free";
    public const string Professional = "Professional";
    public const string Enterprise = "Enterprise";
    public static readonly string[] RoleNames = { Free, Professional, Enterprise };

    public static bool IsFree(string role) => role == Free;
}
