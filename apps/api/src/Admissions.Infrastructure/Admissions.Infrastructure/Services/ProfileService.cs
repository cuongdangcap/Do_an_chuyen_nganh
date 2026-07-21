using Admissions.Application.Auth;
using Admissions.Application.Profiles;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Services;

public sealed class ProfileService(AdmissionsDbContext dbContext) : IProfileService
{
    public async Task<ProfileResponse?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(userId, cancellationToken) ?? throw new KeyNotFoundException("User not found.");
        var roleCodes = user.UserRoles.Select(x => x.Role.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isStudentOnly = roleCodes.Contains("student")
            && !roleCodes.Contains("parent")
            && !roleCodes.Contains("staff")
            && !roleCodes.Contains("admin");

        if (!isStudentOnly && !string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        if (!isStudentOnly && request.StudentProfile is not null)
        {
            user.StudentProfile ??= new StudentProfile { UserId = user.Id };
            user.StudentProfile.HighSchool = request.StudentProfile.HighSchool;
            user.StudentProfile.Province = request.StudentProfile.Province;
            user.StudentProfile.GraduationYear = request.StudentProfile.GraduationYear;
            user.StudentProfile.ExpectedScore = request.StudentProfile.ExpectedScore;
            user.StudentProfile.ExamScore = request.StudentProfile.ExamScore;
            user.StudentProfile.InterestedSubjectGroup = request.StudentProfile.InterestedSubjectGroup;
            user.StudentProfile.Notes = request.StudentProfile.Notes;
            user.StudentProfile.UpdatedAt = DateTime.UtcNow;
        }

        if (request.ParentProfile is not null)
        {
            user.ParentProfile ??= new ParentProfile { UserId = user.Id };
            user.ParentProfile.Occupation = request.ParentProfile.Occupation;
            user.ParentProfile.Province = request.ParentProfile.Province;
            user.ParentProfile.ContactPreference = request.ParentProfile.ContactPreference;
            user.ParentProfile.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    private Task<User?> LoadUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.StudentProfile)
            .Include(x => x.ParentProfile)
            .Include(x => x.StaffProfile)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    private static ProfileResponse ToResponse(User user)
    {
        var userSummary = new UserSummary(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            user.Status,
            user.UserRoles.Select(x => x.Role.Code).OrderBy(x => x).ToArray());

        return new ProfileResponse(
            userSummary,
            user.StudentProfile is null ? null : new StudentProfileDto
            {
                HighSchool = user.StudentProfile.HighSchool,
                Province = user.StudentProfile.Province,
                GraduationYear = user.StudentProfile.GraduationYear,
                ExpectedScore = user.StudentProfile.ExpectedScore,
                ExamScore = user.StudentProfile.ExamScore,
                InterestedSubjectGroup = user.StudentProfile.InterestedSubjectGroup,
                Notes = user.StudentProfile.Notes,
            },
            user.ParentProfile is null ? null : new ParentProfileDto
            {
                Occupation = user.ParentProfile.Occupation,
                Province = user.ParentProfile.Province,
                ContactPreference = user.ParentProfile.ContactPreference,
            },
            user.StaffProfile is null ? null : new StaffProfileDto(
                user.StaffProfile.Department,
                user.StaffProfile.Position,
                user.StaffProfile.CanManageDocuments,
                user.StaffProfile.CanReplyChat));
    }
}
