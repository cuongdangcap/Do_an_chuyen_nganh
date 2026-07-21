namespace Admissions.Domain.Constants;

public static class RoleCodes
{
    public const string Student = "student";
    public const string Parent = "parent";
    public const string Staff = "staff";
    public const string Admin = "admin";

    public static readonly string[] All = [Student, Parent, Staff, Admin];
}
