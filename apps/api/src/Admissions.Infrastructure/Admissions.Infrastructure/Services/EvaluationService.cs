using System.Diagnostics;
using System.Text.Json;
using Admissions.Application.Evaluation;
using Admissions.Application.Rag;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Services;

public sealed class EvaluationService(
    IRagService ragService,
    AdmissionsDbContext dbContext) : IEvaluationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<EvaluationQuestionDto>> ListQuestionsAsync(bool activeOnly, string? category, CancellationToken cancellationToken)
    {
        var query = dbContext.EvaluationQuestions.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim().ToLowerInvariant();
            query = query.Where(x => x.Category == normalized);
        }

        var questions = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return questions.Select(ToQuestionDto).ToList();
    }

    public async Task<EvaluationQuestionDto> CreateQuestionAsync(CreateEvaluationQuestionRequest request, CancellationToken cancellationToken)
    {
        var question = new EvaluationQuestion
        {
            Code = NormalizeCode(request.Code),
            Question = RequireText(request.Question, "Question"),
            ExpectedAnswer = RequireText(request.ExpectedAnswer, "Expected answer"),
            ExpectedKeywordsJson = SerializeKeywords(request.ExpectedKeywords),
            ExpectedSourceTitle = NormalizeOptional(request.ExpectedSourceTitle),
            ExpectedDocumentType = NormalizeOptional(request.ExpectedDocumentType)?.ToLowerInvariant(),
            Category = NormalizeOptional(request.Category)?.ToLowerInvariant() ?? "general",
            IsActive = request.IsActive,
        };

        dbContext.EvaluationQuestions.Add(question);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToQuestionDto(question);
    }

    public async Task<IReadOnlyCollection<EvaluationQuestionDto>> SeedDefaultQuestionsAsync(CancellationToken cancellationToken)
    {
        var defaults = DefaultQuestions();
        var defaultCodes = defaults.Select(q => q.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var retiredDemoCodes = new[] { "HOSO_XETTUYEN", "HOCPHI_CNTT", "OCR_HOSO_2026", "UU_TIEN" };
        var trackedCodes = defaultCodes.Concat(retiredDemoCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existingQuestions = await dbContext.EvaluationQuestions
            .Where(x => trackedCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        foreach (var question in existingQuestions.Where(x => !defaultCodes.Contains(x.Code)))
        {
            question.IsActive = false;
            question.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var question in defaults)
        {
            var existing = existingQuestions.FirstOrDefault(x => string.Equals(x.Code, question.Code, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                dbContext.EvaluationQuestions.Add(question);
                continue;
            }

            existing.Question = question.Question;
            existing.ExpectedAnswer = question.ExpectedAnswer;
            existing.ExpectedKeywordsJson = question.ExpectedKeywordsJson;
            existing.ExpectedSourceTitle = question.ExpectedSourceTitle;
            existing.ExpectedDocumentType = question.ExpectedDocumentType;
            existing.Category = question.Category;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await ListQuestionsAsync(activeOnly: true, category: null, cancellationToken);
    }

    public async Task<EvaluationRunDto> RunAsync(RunEvaluationRequest request, CancellationToken cancellationToken)
    {
        var topK = Math.Clamp(request.TopK, 1, 20);
        var questionsQuery = dbContext.EvaluationQuestions.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim().ToLowerInvariant();
            questionsQuery = questionsQuery.Where(x => x.Category == category);
        }

        var questions = await questionsQuery.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("No active golden questions found.");
        }

        var run = new EvaluationRun
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"Đánh giá RAG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC" : request.Name.Trim(),
            TopK = topK,
            TotalQuestions = questions.Count,
            Status = "running",
            StartedAt = DateTime.UtcNow,
        };
        dbContext.EvaluationRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        var results = new List<EvaluationResult>();
        foreach (var question in questions)
        {
            results.Add(await EvaluateQuestionAsync(run.Id, question, topK, cancellationToken));
        }

        dbContext.EvaluationResults.AddRange(results);
        ApplySummary(run, results);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRunAsync(run.Id, cancellationToken) ?? throw new InvalidOperationException("Evaluation run was not saved.");
    }

    public async Task<EvaluationRunListResponse> ListRunsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.EvaluationRuns
            .Include(x => x.Results)
            .ThenInclude(x => x.EvaluationQuestion)
            .AsQueryable();
        var total = await query.CountAsync(cancellationToken);
        var runs = await query
            .OrderByDescending(x => x.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new EvaluationRunListResponse(runs.Select(ToRunDto).ToList(), total);
    }

    public async Task<EvaluationRunDto?> GetRunAsync(Guid id, CancellationToken cancellationToken)
    {
        var run = await dbContext.EvaluationRuns
            .Include(x => x.Results)
            .ThenInclude(x => x.EvaluationQuestion)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return run is null ? null : ToRunDto(run);
    }

    private async Task<EvaluationResult> EvaluateQuestionAsync(Guid runId, EvaluationQuestion question, int topK, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var search = await ragService.SearchAsync(new RagSearchRequest(question.Question, topK), cancellationToken);
            stopwatch.Stop();
            var sources = search.Results.Take(topK).ToList();
            var expectedKeywords = DeserializeStringList(question.ExpectedKeywordsJson);
            var answerPreview = string.Join(" ", sources.Select(x => x.Content)).Trim();
            var matchedKeywords = expectedKeywords
                .Where(keyword => ContainsNormalized(answerPreview, keyword))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var keywordHitRate = expectedKeywords.Count == 0 ? 1 : (double)matchedKeywords.Count / expectedKeywords.Count;
            var hitAtK = ComputeSourceHit(question, sources);

            return new EvaluationResult
            {
                EvaluationRunId = runId,
                EvaluationQuestionId = question.Id,
                RetrievalBackend = search.Backend,
                TopK = topK,
                TopScore = sources.FirstOrDefault()?.Score ?? 0,
                HitAtK = hitAtK,
                KeywordHitRate = keywordHitRate,
                IsCorrect = hitAtK && keywordHitRate >= 0.5,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                AnswerPreview = Trim(answerPreview, 1000),
                MatchedKeywordsJson = JsonSerializer.Serialize(matchedKeywords, JsonOptions),
                SourcesJson = JsonSerializer.Serialize(sources.Select(ToSourceDto).ToList(), JsonOptions),
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new EvaluationResult
            {
                EvaluationRunId = runId,
                EvaluationQuestionId = question.Id,
                RetrievalBackend = "error",
                TopK = topK,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                AnswerPreview = string.Empty,
                MatchedKeywordsJson = "[]",
                SourcesJson = "[]",
                ErrorMessage = ex.Message,
            };
        }
    }

    private static void ApplySummary(EvaluationRun run, IReadOnlyCollection<EvaluationResult> results)
    {
        run.TotalQuestions = results.Count;
        run.CorrectQuestions = results.Count(x => x.IsCorrect);
        run.HitRateAtK = results.Count == 0 ? 0 : (double)results.Count(x => x.HitAtK) / results.Count;
        run.AverageKeywordHitRate = results.Count == 0 ? 0 : results.Average(x => x.KeywordHitRate);
        run.AverageTopScore = results.Count == 0 ? 0 : results.Average(x => x.TopScore);
        run.AverageLatencyMs = results.Count == 0 ? 0 : results.Average(x => x.LatencyMs);
        run.Status = results.Any(x => x.ErrorMessage is not null) ? "completed_with_errors" : "completed";
        run.FinishedAt = DateTime.UtcNow;
    }

    private static bool ComputeSourceHit(EvaluationQuestion question, IReadOnlyCollection<RagSearchResult> sources)
    {
        if (sources.Count == 0)
        {
            return false;
        }

        var titleHit = string.IsNullOrWhiteSpace(question.ExpectedSourceTitle)
            || sources.Any(x => ContainsNormalized(x.Title ?? string.Empty, question.ExpectedSourceTitle));
        var typeHit = string.IsNullOrWhiteSpace(question.ExpectedDocumentType)
            || sources.Any(x => string.Equals(x.DocumentType, question.ExpectedDocumentType, StringComparison.OrdinalIgnoreCase));
        return titleHit && typeHit;
    }

    private static EvaluationRunDto ToRunDto(EvaluationRun run)
    {
        return new EvaluationRunDto(
            run.Id,
            run.Name,
            run.Status,
            run.TopK,
            run.TotalQuestions,
            run.CorrectQuestions,
            run.HitRateAtK,
            run.AverageKeywordHitRate,
            run.AverageTopScore,
            run.AverageLatencyMs,
            run.StartedAt,
            run.FinishedAt,
            run.ErrorMessage,
            run.Results.OrderBy(x => x.EvaluationQuestion.Code).Select(ToResultDto).ToList());
    }

    private static EvaluationResultDto ToResultDto(EvaluationResult result)
    {
        return new EvaluationResultDto(
            result.Id,
            result.EvaluationQuestionId,
            result.EvaluationQuestion.Code,
            result.EvaluationQuestion.Question,
            result.RetrievalBackend,
            result.TopK,
            result.TopScore,
            result.HitAtK,
            result.KeywordHitRate,
            result.IsCorrect,
            result.LatencyMs,
            result.AnswerPreview,
            DeserializeStringList(result.MatchedKeywordsJson),
            DeserializeSources(result.SourcesJson),
            result.ErrorMessage);
    }

    private static EvaluationQuestionDto ToQuestionDto(EvaluationQuestion question)
    {
        return new EvaluationQuestionDto(
            question.Id,
            question.Code,
            question.Question,
            question.ExpectedAnswer,
            DeserializeStringList(question.ExpectedKeywordsJson),
            question.ExpectedSourceTitle,
            question.ExpectedDocumentType,
            question.Category,
            question.IsActive,
            question.CreatedAt);
    }

    private static EvaluationSourceDto ToSourceDto(RagSearchResult source)
    {
        return new EvaluationSourceDto(
            source.PointId,
            source.Score,
            source.Content,
            source.Title,
            source.DocumentType,
            source.PageNumber);
    }

    private static IReadOnlyCollection<string> DeserializeStringList(string json)
    {
        return JsonSerializer.Deserialize<IReadOnlyCollection<string>>(json, JsonOptions) ?? [];
    }

    private static IReadOnlyCollection<EvaluationSourceDto> DeserializeSources(string json)
    {
        return JsonSerializer.Deserialize<IReadOnlyCollection<EvaluationSourceDto>>(json, JsonOptions) ?? [];
    }

    private static string SerializeKeywords(IReadOnlyCollection<string> keywords)
    {
        var clean = keywords
            .Select(NormalizeOptional)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (clean.Count == 0)
        {
            throw new InvalidOperationException("Expected keywords are required.");
        }

        return JsonSerializer.Serialize(clean, JsonOptions);
    }

    private static string NormalizeCode(string code)
    {
        var normalized = RequireText(code, "Code").Trim().ToUpperInvariant().Replace(" ", "_");
        return normalized.Length <= 80 ? normalized : throw new InvalidOperationException("Code is too long.");
    }

    private static string RequireText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{field} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool ContainsNormalized(string text, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return text.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Trim(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }

    private static IReadOnlyCollection<EvaluationQuestion> DefaultQuestions()
    {
        return
        [
            NewQuestion(
                "CMCU_HOSO_2026",
                "Hồ sơ xét tuyển trực tuyến Đại học CMC gồm những gì?",
                "Hồ sơ xét tuyển trực tuyến gồm ảnh hoặc PDF kết quả học tập, căn cước công dân, chứng chỉ ngoại ngữ nếu có, bằng tốt nghiệp nếu đã tốt nghiệp trước năm 2026 và minh chứng thành tích nếu xét tuyển thẳng.",
                ["PDF", "kết quả học tập", "căn cước công dân", "chứng chỉ ngoại ngữ", "xét tuyển thẳng"],
                "Nguồn tuyển sinh CMCU 2026",
                "admission_notice",
                "cmcu_admission_profile"),
            NewQuestion(
                "CMCU_HOCPHI_AI_2026",
                "Học phí ngành Trí tuệ Nhân tạo Đại học CMC năm 2026 là bao nhiêu?",
                "Ngành Trí tuệ Nhân tạo thuộc nhóm công nghệ, học phí dự kiến theo kỳ là 14.742.000 VNĐ cho học kỳ 1-3, 18.018.000 VNĐ cho học kỳ 4-6 và 21.840.000 VNĐ cho học kỳ 7-9.",
                ["Trí tuệ Nhân tạo", "14.742.000", "18.018.000", "21.840.000", "học kỳ"],
                "Nguồn tuyển sinh CMCU 2026",
                "admission_notice",
                "cmcu_tuition"),
            NewQuestion(
                "CMCU_PHUONGTHUC_2026",
                "Đại học CMC năm 2026 có những phương thức xét tuyển nào?",
                "Các phương thức gồm CMC401 xét kết quả kỳ thi CMC-TEST, CMC200 xét kết quả học tập bậc THPT, CMC100 xét kết quả thi tốt nghiệp THPT và CMC303 xét tuyển thẳng theo quy chế.",
                ["CMC401", "CMC200", "CMC100", "CMC303", "CMC-TEST"],
                "Nguồn tuyển sinh CMCU 2026",
                "admission_notice",
                "cmcu_methods"),
            NewQuestion(
                "CMCU_CHITIEU_2026",
                "Chỉ tiêu tuyển sinh Đại học CMC năm 2026 là bao nhiêu?",
                "Năm 2026, Đại học CMC công bố 1.800 chỉ tiêu tại Hà Nội và 800 chỉ tiêu tại TP.HCM, tổng 2.600 chỉ tiêu.",
                ["1.800", "800", "2.600", "Hà Nội", "TP.HCM"],
                "Nguồn tuyển sinh CMCU 2026",
                "admission_notice",
                "cmcu_quota"),
            NewQuestion(
                "CMCU_DIEMCHUAN_2026",
                "Điểm chuẩn Đại học CMC năm 2026 là bao nhiêu?",
                "Hệ thống không tự bịa điểm chuẩn năm 2026. Khi Trường Đại học CMC công bố điểm trúng tuyển chính thức, quản trị viên cập nhật trong cổng quản trị.",
                ["không tự bịa", "điểm chuẩn", "công bố", "quản trị viên"],
                "Nguồn tuyển sinh CMCU 2026",
                "admission_notice",
                "cmcu_cutoff"),
        ];
    }

    private static EvaluationQuestion NewQuestion(
        string code,
        string question,
        string expectedAnswer,
        IReadOnlyCollection<string> keywords,
        string expectedSourceTitle,
        string documentType,
        string category)
    {
        return new EvaluationQuestion
        {
            Code = code,
            Question = question,
            ExpectedAnswer = expectedAnswer,
            ExpectedKeywordsJson = JsonSerializer.Serialize(keywords, JsonOptions),
            ExpectedSourceTitle = expectedSourceTitle,
            ExpectedDocumentType = documentType,
            Category = category,
            IsActive = true,
        };
    }
}
