namespace Pinetree.Shared;

public static class Roles
{
    public const string Free = "Free";
    public const string Professional = "Professional";
    public const string Enterprise = "Enterprise";
    public const string Admin = "Admin";
    public static readonly string[] RoleNames = { Free, Professional, Enterprise, Admin };

    public static bool IsFree(string role) => role == Free;
    public static bool IsProfessional(string role) => role == Professional;
    public static bool IsAdmin(string role) => role == Admin;
}
