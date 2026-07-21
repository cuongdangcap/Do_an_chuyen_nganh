using Admissions.Application.Admissions;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Services;

public sealed class AdmissionsService(AdmissionsDbContext dbContext) : IAdmissionsService
{
    public async Task<PagedResponse<MajorListItem>> ListMajorsAsync(MajorQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var majors = dbContext.Majors
            .Include(x => x.Faculty)
            .Include(x => x.Programs)
            .ThenInclude(x => x.CutoffScores)
            .ThenInclude(x => x.AdmissionCycle)
            .Include(x => x.Programs)
            .ThenInclude(x => x.TuitionFees)
            .Include(x => x.Programs)
            .ThenInclude(x => x.SubjectCombinations)
            .ThenInclude(x => x.SubjectCombination)
            .Where(x => x.Status == "active")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLowerInvariant();
            majors = majors.Where(x =>
                x.Code.ToLower().Contains(keyword) ||
                x.Name.ToLower().Contains(keyword) ||
                (x.Description != null && x.Description.ToLower().Contains(keyword)));
        }

        if (query.FacultyId is not null)
        {
            majors = majors.Where(x => x.FacultyId == query.FacultyId);
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectCombinationCode))
        {
            var code = query.SubjectCombinationCode.Trim().ToUpperInvariant();
            majors = majors.Where(x => x.Programs.Any(program =>
                program.SubjectCombinations.Any(subject => subject.SubjectCombination.Code == code)));
        }

        if (query.MaxTuition is not null)
        {
            majors = majors.Where(x => x.Programs.Any(program =>
                program.TuitionFees.Any(fee => fee.AmountMin <= query.MaxTuition || fee.AmountMax <= query.MaxTuition)));
        }

        if (!string.IsNullOrWhiteSpace(query.Campus))
        {
            var campus = query.Campus.Trim().ToLowerInvariant();
            majors = majors.Where(x => x.Programs.Any(program => program.Campus != null && program.Campus.ToLower().Contains(campus)));
        }

        if (query.MinScore is not null || query.MaxScore is not null)
        {
            majors = majors.Where(x => x.Programs.Any(program => program.CutoffScores.Any(score =>
                (query.MinScore == null || score.Score >= query.MinScore) &&
                (query.MaxScore == null || score.Score <= query.MaxScore))));
        }

        var total = await majors.CountAsync(cancellationToken);
        var pageItems = await majors
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = pageItems.Select(ToMajorListItem).ToList();

        return new PagedResponse<MajorListItem>(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<MajorDetailDto?> GetMajorAsync(Guid id, CancellationToken cancellationToken)
    {
        var major = await LoadMajorAsync(id, cancellationToken);
        return major is null ? null : ToMajorDetail(major);
    }

    public async Task<IReadOnlyCollection<AdmissionCycleDto>> ListAdmissionCyclesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AdmissionCycles
            .Where(x => x.Status == "active")
            .OrderByDescending(x => x.Year)
            .Select(x => new AdmissionCycleDto(
                x.Id,
                x.Year,
                x.Name,
                x.StartDate,
                x.EndDate,
                x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FacultyDto>> ListFacultiesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Faculties
            .Where(x => x.Status == "active")
            .OrderBy(x => x.Name)
            .Select(x => new FacultyDto(x.Id, x.Code, x.Name, x.Description, x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SubjectCombinationDto>> ListSubjectCombinationsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SubjectCombinations
            .OrderBy(x => x.Code)
            .Select(x => new SubjectCombinationDto(x.Id, x.Code, x.Subjects, x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdmissionMethodDto>> ListAdmissionMethodsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AdmissionMethods
            .OrderBy(x => x.Code)
            .Select(x => new AdmissionMethodDto(x.Id, x.Code, x.Name, x.Description, x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<FaqDto>> ListFaqsAsync(string? category, CancellationToken cancellationToken)
    {
        var query = dbContext.FaqItems.Where(x => x.Status == "active");
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(x => x.Category != null && x.Category.ToLower() == normalizedCategory);
        }

        return await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Question)
            .Select(x => new FaqDto(x.Id, x.Category, x.Question, x.Answer, x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProgramComparisonResponse> CompareProgramsAsync(CompareProgramsRequest request, CancellationToken cancellationToken)
    {
        var programs = await dbContext.Programs
            .Include(x => x.SubjectCombinations)
            .ThenInclude(x => x.SubjectCombination)
            .Include(x => x.CutoffScores)
            .ThenInclude(x => x.AdmissionCycle)
            .Include(x => x.CutoffScores)
            .ThenInclude(x => x.AdmissionMethod)
            .Include(x => x.CutoffScores)
            .ThenInclude(x => x.SubjectCombination)
            .Include(x => x.TuitionFees)
            .Where(x => request.ProgramIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var items = programs.Select(ToProgramDetail).ToList();
        var summary = items.Count < 2
            ? "Cần chọn ít nhất hai chương trình để so sánh."
            : "Bảng so sánh dựa trên điểm chuẩn, học phí, tổ hợp môn và thông tin chương trình hiện có.";

        return new ProgramComparisonResponse(items, summary);
    }

    public async Task<AdmissionCycleDto> CreateAdmissionCycleAsync(CreateAdmissionCycleRequest request, CancellationToken cancellationToken)
    {
        var cycle = new AdmissionCycle
        {
            Year = request.Year,
            Name = request.Name.Trim(),
            StartDate = request.ApplicationStartDate,
            EndDate = request.ApplicationEndDate,
            Status = NormalizeStatus(request.Status),
        };

        dbContext.AdmissionCycles.Add(cycle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdmissionCycleDto(
            cycle.Id,
            cycle.Year,
            cycle.Name,
            cycle.StartDate,
            cycle.EndDate,
            cycle.Status);
    }

    public async Task<FacultyDto> CreateFacultyAsync(CreateFacultyRequest request, CancellationToken cancellationToken)
    {
        var faculty = new Faculty
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Status = NormalizeStatus(request.Status),
        };

        dbContext.Faculties.Add(faculty);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new FacultyDto(faculty.Id, faculty.Code, faculty.Name, faculty.Description, faculty.Status);
    }

    public async Task<SubjectCombinationDto> CreateSubjectCombinationAsync(CreateSubjectCombinationRequest request, CancellationToken cancellationToken)
    {
        var subjectCombination = new SubjectCombination
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Subjects = request.Subjects.Trim(),
            Description = request.Description,
        };

        dbContext.SubjectCombinations.Add(subjectCombination);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SubjectCombinationDto(
            subjectCombination.Id,
            subjectCombination.Code,
            subjectCombination.Subjects,
            subjectCombination.Description);
    }

    public async Task<AdmissionMethodDto> CreateAdmissionMethodAsync(CreateAdmissionMethodRequest request, CancellationToken cancellationToken)
    {
        var admissionMethod = new AdmissionMethod
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Status = NormalizeStatus(request.Status),
        };

        dbContext.AdmissionMethods.Add(admissionMethod);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdmissionMethodDto(
            admissionMethod.Id,
            admissionMethod.Code,
            admissionMethod.Name,
            admissionMethod.Description,
            admissionMethod.Status);
    }

    public async Task<MajorDetailDto> CreateMajorAsync(CreateMajorRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Faculties.AnyAsync(x => x.Id == request.FacultyId, cancellationToken))
        {
            throw new KeyNotFoundException("Faculty not found.");
        }

        var major = new Major
        {
            FacultyId = request.FacultyId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            CareerOutcomes = request.CareerOutcomes,
            Status = NormalizeStatus(request.Status),
        };

        dbContext.Majors.Add(major);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToMajorDetail((await LoadMajorAsync(major.Id, cancellationToken))!);
    }

    public async Task<MajorDetailDto> UpdateMajorAsync(Guid id, CreateMajorRequest request, CancellationToken cancellationToken)
    {
        var major = await dbContext.Majors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Major not found.");
        if (!await dbContext.Faculties.AnyAsync(x => x.Id == request.FacultyId, cancellationToken))
        {
            throw new KeyNotFoundException("Faculty not found.");
        }

        major.FacultyId = request.FacultyId;
        major.Code = request.Code.Trim();
        major.Name = request.Name.Trim();
        major.Description = request.Description;
        major.CareerOutcomes = request.CareerOutcomes;
        major.Status = NormalizeStatus(request.Status);
        major.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToMajorDetail((await LoadMajorAsync(major.Id, cancellationToken))!);
    }

    public async Task DeleteMajorAsync(Guid id, CancellationToken cancellationToken)
    {
        var major = await dbContext.Majors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Major not found.");
        major.Status = "inactive";
        major.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProgramDetailDto> CreateProgramAsync(CreateProgramRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Majors.AnyAsync(x => x.Id == request.MajorId, cancellationToken))
        {
            throw new KeyNotFoundException("Major not found.");
        }

        var subjectIds = request.SubjectCombinationIds.Distinct().ToList();
        var existingSubjectCount = await dbContext.SubjectCombinations
            .CountAsync(x => subjectIds.Contains(x.Id), cancellationToken);
        if (existingSubjectCount != subjectIds.Count)
        {
            throw new KeyNotFoundException("One or more subject combinations were not found.");
        }

        var program = new AcademicProgram
        {
            MajorId = request.MajorId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            DegreeType = request.DegreeType,
            Language = request.Language,
            Campus = request.Campus,
            DurationYears = request.DurationYears,
            Description = request.Description,
            Status = NormalizeStatus(request.Status),
        };

        foreach (var subjectId in subjectIds)
        {
            program.SubjectCombinations.Add(new ProgramSubjectCombination
            {
                ProgramId = program.Id,
                SubjectCombinationId = subjectId,
            });
        }

        dbContext.Programs.Add(program);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToProgramDetail((await LoadProgramAsync(program.Id, cancellationToken))!);
    }

    public async Task<CutoffScoreDto> CreateCutoffScoreAsync(CreateCutoffScoreRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Programs.AnyAsync(x => x.Id == request.ProgramId, cancellationToken))
        {
            throw new KeyNotFoundException("Program not found.");
        }

        if (!await dbContext.AdmissionCycles.AnyAsync(x => x.Id == request.AdmissionCycleId, cancellationToken))
        {
            throw new KeyNotFoundException("Admission cycle not found.");
        }

        if (!await dbContext.AdmissionMethods.AnyAsync(x => x.Id == request.AdmissionMethodId, cancellationToken))
        {
            throw new KeyNotFoundException("Admission method not found.");
        }

        if (request.SubjectCombinationId is not null &&
            !await dbContext.SubjectCombinations.AnyAsync(x => x.Id == request.SubjectCombinationId, cancellationToken))
        {
            throw new KeyNotFoundException("Subject combination not found.");
        }

        var cutoffScore = new CutoffScore
        {
            ProgramId = request.ProgramId,
            AdmissionCycleId = request.AdmissionCycleId,
            AdmissionMethodId = request.AdmissionMethodId,
            SubjectCombinationId = request.SubjectCombinationId,
            Score = request.Score,
            Note = request.Note,
        };

        dbContext.CutoffScores.Add(cutoffScore);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCutoffScoreDto((await LoadCutoffScoreAsync(cutoffScore.Id, cancellationToken))!);
    }

    public async Task<TuitionFeeDto> CreateTuitionFeeAsync(CreateTuitionFeeRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Programs.AnyAsync(x => x.Id == request.ProgramId, cancellationToken))
        {
            throw new KeyNotFoundException("Program not found.");
        }

        var tuitionFee = new TuitionFee
        {
            ProgramId = request.ProgramId,
            AcademicYear = request.AcademicYear.Trim(),
            AmountMin = request.AmountMin,
            AmountMax = request.AmountMax,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "VND" : request.Currency.Trim().ToUpperInvariant(),
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "year" : request.Unit.Trim().ToLowerInvariant(),
            Note = request.Note,
        };

        dbContext.TuitionFees.Add(tuitionFee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTuitionFeeDto(tuitionFee);
    }

    public async Task<FaqDto> CreateFaqAsync(CreateFaqRequest request, CancellationToken cancellationToken)
    {
        var faq = new FaqItem
        {
            Category = request.Category,
            Question = request.Question.Trim(),
            Answer = request.Answer.Trim(),
            Status = NormalizeStatus(request.Status),
        };

        dbContext.FaqItems.Add(faq);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new FaqDto(faq.Id, faq.Category, faq.Question, faq.Answer, faq.Status);
    }

    private Task<Major?> LoadMajorAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Majors
            .Include(x => x.Faculty)
            .Include(x => x.Programs)
            .ThenInclude(x => x.SubjectCombinations)
            .ThenInclude(x => x.SubjectCombination)
            .Include(x => x.Programs)
            .ThenInclude(x => x.CutoffScores)
            .ThenInclude(x => x.AdmissionCycle)
            .Include(x => x.Programs)
            .ThenInclude(x => x.CutoffScores)
            .ThenInclude(x => x.AdmissionMethod)
            .Include(x => x.Programs)
            .ThenInclude(x => x.CutoffScores)
            .ThenInclude(x => x.SubjectCombination)
            .Include(x => x.Programs)
            .ThenInclude(x => x.TuitionFees)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private Task<AcademicProgram?> LoadProgramAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Programs
            .Include(x => x.SubjectCombinations)
            .ThenInclude(x => x.SubjectCombination)
            .Include(x => x.CutoffScores)
            .ThenInclude(x => x.AdmissionCycle)
            .Include(x => x.CutoffScores)
            .ThenInclude(x => x.AdmissionMethod)
            .Include(x => x.CutoffScores)
            .ThenInclude(x => x.SubjectCombination)
            .Include(x => x.TuitionFees)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private Task<CutoffScore?> LoadCutoffScoreAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.CutoffScores
            .Include(x => x.AdmissionCycle)
            .Include(x => x.AdmissionMethod)
            .Include(x => x.SubjectCombination)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static MajorListItem ToMajorListItem(Major major)
    {
        return new MajorListItem(
            major.Id,
            major.Code,
            major.Name,
            major.Faculty.Name,
            major.Programs.Where(x => x.Status == "active").Select(ToProgramSummary).ToList());
    }

    private static MajorDetailDto ToMajorDetail(Major major)
    {
        return new MajorDetailDto(
            major.Id,
            major.Code,
            major.Name,
            major.Description,
            major.CareerOutcomes,
            major.Status,
            new FacultyDto(major.Faculty.Id, major.Faculty.Code, major.Faculty.Name, major.Faculty.Description, major.Faculty.Status),
            major.Programs.OrderBy(x => x.Name).Select(ToProgramDetail).ToList());
    }

    private static ProgramSummaryDto ToProgramSummary(AcademicProgram program)
    {
        var latestScore = program.CutoffScores
            .OrderByDescending(x => x.AdmissionCycle.Year)
            .Select(x => (decimal?)x.Score)
            .FirstOrDefault();

        var latestTuition = program.TuitionFees.OrderByDescending(x => x.AcademicYear).FirstOrDefault();

        return new ProgramSummaryDto(
            program.Id,
            program.Code,
            program.Name,
            program.Campus,
            latestScore,
            latestTuition is null ? null : FormatTuition(latestTuition));
    }

    private static ProgramDetailDto ToProgramDetail(AcademicProgram program)
    {
        return new ProgramDetailDto(
            program.Id,
            program.Code,
            program.Name,
            program.DegreeType,
            program.Language,
            program.Campus,
            program.DurationYears,
            program.Description,
            program.Status,
            program.SubjectCombinations.Select(x => new SubjectCombinationDto(
                x.SubjectCombination.Id,
                x.SubjectCombination.Code,
                x.SubjectCombination.Subjects,
                x.SubjectCombination.Description)).OrderBy(x => x.Code).ToList(),
            program.CutoffScores.Select(ToCutoffScoreDto).OrderByDescending(x => x.Year).ToList(),
            program.TuitionFees.Select(ToTuitionFeeDto).OrderByDescending(x => x.AcademicYear).ToList());
    }

    private static CutoffScoreDto ToCutoffScoreDto(CutoffScore score)
    {
        return new CutoffScoreDto(
            score.Id,
            score.AdmissionCycle.Year,
            score.AdmissionMethod.Code,
            score.AdmissionMethod.Name,
            score.SubjectCombination?.Code,
            score.Score,
            score.Note);
    }

    private static TuitionFeeDto ToTuitionFeeDto(TuitionFee fee)
    {
        return new TuitionFeeDto(fee.Id, fee.AcademicYear, fee.AmountMin, fee.AmountMax, fee.Currency, fee.Unit, fee.Note);
    }

    private static string FormatTuition(TuitionFee fee)
    {
        return fee.AmountMin == fee.AmountMax
            ? $"{fee.AmountMin:n0} {fee.Currency}/{NormalizeUnit(fee.Unit)}"
            : $"{fee.AmountMin:n0} - {fee.AmountMax:n0} {fee.Currency}/{NormalizeUnit(fee.Unit)}";
    }

    private static string NormalizeUnit(string unit)
    {
        return unit.Trim().ToLowerInvariant() switch
        {
            "year" => "năm",
            "semester" => "học kỳ",
            _ => unit,
        };
    }

    private static string NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();
    }
}
