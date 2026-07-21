using Admissions.Api.Common;
using Admissions.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/profiles")]
[Authorize]
public sealed class ProfilesController(IProfileService profileService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("AUTH_INVALID_TOKEN", "Invalid token.", HttpContext.TraceIdentifier));
        }

        var profile = await profileService.GetAsync(userId.Value, cancellationToken);
        return profile is null
            ? NotFound(ApiResponse<object>.Fail("USER_NOT_FOUND", "User not found.", HttpContext.TraceIdentifier))
            : Ok(ApiResponse<ProfileResponse>.Ok(profile, "OK", HttpContext.TraceIdentifier));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("AUTH_INVALID_TOKEN", "Invalid token.", HttpContext.TraceIdentifier));
        }

        using var body = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
        var root = body.RootElement;
        if ((root.TryGetProperty("request", out var requestElement) || root.TryGetProperty("Request", out requestElement))
            && requestElement.ValueKind == JsonValueKind.Object)
        {
            root = requestElement;
        }

        var request = ParseUpdateProfileRequest(root);

        var profile = await profileService.UpdateAsync(userId.Value, request, cancellationToken);
        return Ok(ApiResponse<ProfileResponse>.Ok(profile, "Profile updated.", HttpContext.TraceIdentifier));
    }

    private static UpdateProfileRequest ParseUpdateProfileRequest(JsonElement root)
    {
        return new UpdateProfileRequest
        {
            FullName = ReadString(root, "fullName", "FullName"),
            Phone = ReadString(root, "phone", "Phone"),
            StudentProfile = TryReadElement(root, out var student, "studentProfile", "StudentProfile")
                ? new StudentProfileDto
                {
                    HighSchool = ReadString(student, "highSchool", "HighSchool"),
                    Province = ReadString(student, "province", "Province"),
                    GraduationYear = ReadInt(student, "graduationYear", "GraduationYear"),
                    ExpectedScore = ReadDecimal(student, "expectedScore", "ExpectedScore"),
                    ExamScore = ReadDecimal(student, "examScore", "ExamScore"),
                    InterestedSubjectGroup = ReadString(student, "interestedSubjectGroup", "InterestedSubjectGroup"),
                    Notes = ReadString(student, "notes", "Notes"),
                }
                : null,
            ParentProfile = TryReadElement(root, out var parent, "parentProfile", "ParentProfile")
                ? new ParentProfileDto
                {
                    Occupation = ReadString(parent, "occupation", "Occupation"),
                    Province = ReadString(parent, "province", "Province"),
                    ContactPreference = ReadString(parent, "contactPreference", "ContactPreference"),
                }
                : null,
        };
    }

    private static bool TryReadElement(JsonElement root, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? ReadString(JsonElement root, params string[] names)
    {
        return TryReadElement(root, out var value, names) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement root, params string[] names)
    {
        return TryReadElement(root, out var value, names) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result)
            ? result
            : null;
    }

    private static decimal? ReadDecimal(JsonElement root, params string[] names)
    {
        return TryReadElement(root, out var value, names) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var result)
            ? result
            : null;
    }
}
