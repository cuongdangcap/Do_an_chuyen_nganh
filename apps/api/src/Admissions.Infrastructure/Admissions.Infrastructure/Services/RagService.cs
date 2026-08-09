using Admissions.Application.Rag;
using Admissions.Application.Handoff;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Admissions.Infrastructure.Services;

public sealed class RagService(
    DocumentIngestionClient ingestionClient,
    AdmissionsDbContext dbContext,
    IHandoffService handoffService,
    LlmAnswerService llmAnswerService) : IRagService
{
    public async Task<RagSearchResponse> SearchAsync(RagSearchRequest request, CancellationToken cancellationToken)
    {
        var topK = request.TopK <= 0 ? 5 : request.TopK;
        var candidateK = Math.Clamp(topK * 4, topK, 20);
        var result = await ingestionClient.SearchAsync(request.Query, candidateK, cancellationToken);
        var reranked = RerankResults(request.Query, result.Results.Select(ToSearchResult).ToList())
            .Take(topK)
            .ToList();
        return new RagSearchResponse(result.Backend, reranked);
    }

    public async Task<RagChatResponse> ChatAsync(RagChatRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var immediate = TryBuildImmediateStructuredAnswer(request.Question);
        if (immediate is not null)
        {
            stopwatch.Stop();
            var immediateStored = await StoreChatAsync(request, userId, immediate.Answer, "structured", immediate.Sources, (int)stopwatch.ElapsedMilliseconds, cancellationToken);
            return new RagChatResponse(
                immediate.Answer,
                "structured",
                immediate.Sources,
                immediateStored.ConversationId,
                immediateStored.UserMessageId,
                immediateStored.AssistantMessageId,
                (int)stopwatch.ElapsedMilliseconds);
        }

        var structured = await TryBuildStructuredAnswerAsync(request.Question, cancellationToken);
        if (structured is not null)
        {
            stopwatch.Stop();
            var structuredStored = await StoreChatAsync(request, userId, structured.Answer, "structured", structured.Sources, (int)stopwatch.ElapsedMilliseconds, cancellationToken);
            return new RagChatResponse(
                structured.Answer,
                "structured",
                structured.Sources,
                structuredStored.ConversationId,
                structuredStored.UserMessageId,
                structuredStored.AssistantMessageId,
                (int)stopwatch.ElapsedMilliseconds);
        }

        var search = await SearchAsync(new RagSearchRequest(request.Question, request.TopK <= 0 ? 5 : request.TopK), cancellationToken);
        var strongSources = search.Results.Where(x => x.Score >= 0.18).Take(4).ToList();
        string answer;
        IReadOnlyCollection<RagSearchResult> selectedSources;

        if (strongSources.Count == 0)
        {
            answer = "Hiện hệ thống chưa có đủ nguồn tài liệu để trả lời chắc chắn. Bạn có thể tải thêm quy chế, học phí hoặc thông báo tuyển sinh chính thức.";
            selectedSources = search.Results;
        }
        else
        {
            selectedSources = strongSources.Select(source => FocusSourceContent(request.Question, source)).ToList();
            answer = await llmAnswerService.TryGenerateAsync(request.Question, selectedSources, cancellationToken)
                ?? BuildExtractiveAnswer(selectedSources);
        }

        stopwatch.Stop();
        var stored = await StoreChatAsync(request, userId, answer, search.Backend, selectedSources, (int)stopwatch.ElapsedMilliseconds, cancellationToken);
        return new RagChatResponse(
            answer,
            search.Backend,
            selectedSources,
            stored.ConversationId,
            stored.UserMessageId,
            stored.AssistantMessageId,
            (int)stopwatch.ElapsedMilliseconds);
    }

    public async Task<ChatFeedbackDto> CreateFeedbackAsync(Guid assistantMessageId, CreateChatFeedbackRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var message = await dbContext.ChatMessages
            .Include(x => x.Conversation)
            .ThenInclude(x => x.Messages)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == assistantMessageId && x.Role == "assistant", cancellationToken)
            ?? throw new KeyNotFoundException("Assistant message not found.");

        var rating = NormalizeRating(request.Rating);
        var feedback = new ChatFeedback
        {
            MessageId = message.Id,
            UserId = userId,
            Rating = rating,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };

        dbContext.ChatFeedback.Add(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);

        Guid? handoffTicketId = null;
        if (rating == "negative")
        {
            var ticket = await handoffService.CreateFromNegativeFeedbackAsync(feedback.Id, cancellationToken);
            handoffTicketId = ticket.Id;
        }

        return ToFeedbackDto(feedback, message, handoffTicketId);
    }

    public async Task<ChatFeedbackListResponse> ListFeedbackAsync(string? rating, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.ChatFeedback
            .Include(x => x.User)
            .Include(x => x.Message)
            .ThenInclude(x => x.Conversation)
            .ThenInclude(x => x.Messages)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(rating))
        {
            var normalized = NormalizeRating(rating);
            query = query.Where(x => x.Rating == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var feedbackIds = items.Select(x => x.Id).ToList();
        var ticketMap = await dbContext.HandoffTickets
            .Where(x => x.FeedbackId != null && feedbackIds.Contains(x.FeedbackId.Value))
            .ToDictionaryAsync(x => x.FeedbackId!.Value, x => x.Id, cancellationToken);

        return new ChatFeedbackListResponse(items.Select(x => ToFeedbackDto(
            x,
            x.Message,
            ticketMap.TryGetValue(x.Id, out var ticketId) ? ticketId : null)).ToList(), total);
    }

    private static RagSearchResult ToSearchResult(AiSearchResult result)
    {
        return new RagSearchResult(
            result.PointId,
            result.Score,
            result.Content,
            GetString(result.Metadata, "title"),
            GetString(result.Metadata, "document_type"),
            GetInt(result.Metadata, "page_number"),
            GetString(result.Metadata, "section_title"));
    }

    private static IReadOnlyCollection<RagSearchResult> RerankResults(string query, IReadOnlyCollection<RagSearchResult> results)
    {
        if (results.Count <= 1)
        {
            return results;
        }

        var normalizedQuery = NormalizeForSearch(query);
        var tokens = ExtractSearchTokens(normalizedQuery);
        var intentPhrases = DetectIntentPhrases(normalizedQuery);

        return results
            .Select(source =>
            {
                var searchableText = NormalizeForSearch($"{source.Title} {source.SectionTitle} {source.DocumentType} {source.Content}");
                var tokenHits = tokens.Count == 0 ? 0 : tokens.Count(token => searchableText.Contains(token, StringComparison.OrdinalIgnoreCase));
                var tokenScore = tokens.Count == 0 ? 0 : (double)tokenHits / tokens.Count;
                var phraseHits = intentPhrases.Count(phrase => searchableText.Contains(phrase, StringComparison.OrdinalIgnoreCase));
                var phraseScore = intentPhrases.Count == 0 ? 0 : (double)phraseHits / intentPhrases.Count;
                var headingBoost = source.SectionTitle is not null && NormalizeForSearch(source.SectionTitle).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(part => normalizedQuery.Contains(part, StringComparison.OrdinalIgnoreCase))
                    ? 0.08
                    : 0;
                var combinedScore = source.Score + (tokenScore * 0.2) + (phraseScore * 0.55) + headingBoost;
                return source with { Score = combinedScore };
            })
            .OrderByDescending(source => source.Score)
            .ToList();
    }

    private static IReadOnlyCollection<string> ExtractSearchTokens(string normalizedQuery)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "la", "co", "cua", "cho", "ve", "va", "nam", "bao", "nhieu", "gi", "nhung", "nao",
            "dai", "hoc", "cmc", "cmcu", "truc", "tuyen", "thi", "can", "gom",
        };

        return normalizedQuery
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2 && !stopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyCollection<string> DetectIntentPhrases(string normalizedQuery)
    {
        if (normalizedQuery.Contains("ho so", StringComparison.OrdinalIgnoreCase))
        {
            return ["ho so xet tuyen", "ket qua hoc tap", "can cuoc cong dan", "chung chi ngoai ngu", "bang tot nghiep", "xet tuyen thang"];
        }

        if (normalizedQuery.Contains("hoc phi", StringComparison.OrdinalIgnoreCase))
        {
            return ["hoc phi", "14.742.000", "18.018.000", "21.840.000", "hoc ky"];
        }

        if (normalizedQuery.Contains("phuong thuc", StringComparison.OrdinalIgnoreCase) || normalizedQuery.Contains("xet tuyen", StringComparison.OrdinalIgnoreCase))
        {
            return ["cmc401", "cmc200", "cmc100", "cmc303", "cmc-test"];
        }

        if (normalizedQuery.Contains("chi tieu", StringComparison.OrdinalIgnoreCase))
        {
            return ["chi tieu", "2.315", "ha noi", "tp.hcm"];
        }

        if (normalizedQuery.Contains("diem chuan", StringComparison.OrdinalIgnoreCase))
        {
            return ["diem chuan", "khong tu bia", "cong bo", "quan tri vien"];
        }

        return [];
    }

    private static string NormalizeForSearch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var formD = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var character in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character switch
                {
                    'đ' => 'd',
                    _ => char.IsLetterOrDigit(character) || character is '.' or '-' ? character : ' ',
                });
            }
        }

        var normalized = string.Join(' ', builder.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized
            .Replace("hoc fi", "hoc phi", StringComparison.OrdinalIgnoreCase)
            .Replace("hoc phii", "hoc phi", StringComparison.OrdinalIgnoreCase)
            .Replace("tuyen sin", "tuyen sinh", StringComparison.OrdinalIgnoreCase)
            .Replace("xet tuen", "xet tuyen", StringComparison.OrdinalIgnoreCase)
            .Replace("nghanh", "nganh", StringComparison.OrdinalIgnoreCase)
            .Replace("ngah ", "nganh ", StringComparison.OrdinalIgnoreCase)
            .Replace("nganhh", "nganh", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetString(Dictionary<string, object?> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static int? GetInt(Dictionary<string, object?> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static string TrimForAnswer(string content)
    {
        var normalized = RestoreCommonVietnameseTerms(content.ReplaceLineEndings(" ").Trim());
        return TrimToSentenceBoundary(normalized, 360);
    }

    private async Task<StructuredAnswer?> TryBuildStructuredAnswerAsync(string question, CancellationToken cancellationToken)
    {
        var normalizedQuestion = NormalizeForSearch(question);
        if (IsSchoolHistoryQuestion(normalizedQuestion))
        {
            const string answer = "Theo trang giới thiệu chính thức của Trường Đại học CMC, ngày 26/7/2022 Trường Đại học CMC chính thức được đổi tên theo Quyết định số 895/QĐ-TTg của Thủ tướng Chính phủ. Vì vậy, nếu hỏi theo mốc Trường Đại học CMC hiện nay, hệ thống lấy mốc năm 2022. Trường cũng công bố chiến lược chuyển đổi “AI University” vào ngày 22/7/2024.";
            return new StructuredAnswer(answer, [BuildStructuredSource("Giới thiệu về Trường Đại học CMC", "https://cmcu.edu.vn/dai-hoc-cmc/", answer)]);
        }

        if (IsSubjectiveQualityQuestion(normalizedQuestion))
        {
            const string answer = "Mình chưa có đủ dữ liệu khách quan để kết luận Trường Đại học CMC dạy tốt hay không. Kho dữ liệu hiện có chủ yếu là thông tin tuyển sinh, học phí, ngành học, học bổng, hồ sơ xét tuyển và thông tin chính thức của trường. Để đánh giá chất lượng đào tạo, bạn nên đối chiếu thêm kiểm định chất lượng, chương trình đào tạo chi tiết, đội ngũ giảng viên, cơ sở vật chất, phản hồi sinh viên/cựu sinh viên và tỷ lệ việc làm sau tốt nghiệp.";
            return new StructuredAnswer(answer, []);
        }

        if (normalizedQuestion.Contains("hoc phi", StringComparison.OrdinalIgnoreCase))
        {
            var answer = await TryBuildTuitionAnswerAsync(normalizedQuestion, cancellationToken);
            return answer is null
                ? null
                : new StructuredAnswer(answer, [BuildStructuredSource("Dữ liệu học phí CMCU 2026", "database:tuition_fees", answer)]);
        }

        return await TryBuildProgramOverviewAnswerAsync(normalizedQuestion, cancellationToken);
    }

    private static StructuredAnswer? TryBuildImmediateStructuredAnswer(string question)
    {
        var normalizedQuestion = NormalizeForSearch(question);
        if (IsGreeting(normalizedQuestion))
        {
            const string answer = "Chào bạn! Mình là trợ lý tuyển sinh Trường Đại học CMC. Bạn có thể hỏi mình về ngành học, học phí, học bổng, phương thức xét tuyển, hồ sơ, chỉ tiêu, cơ sở đào tạo hoặc thông tin liên hệ.";
            return new StructuredAnswer(answer, []);
        }

        if (!IsAdmissionsDomainQuestion(normalizedQuestion))
        {
            const string answer = "Mình chỉ hỗ trợ thông tin tuyển sinh và học tập tại Trường Đại học CMC nên chưa thể trả lời chắc chắn câu hỏi này. Bạn có thể hỏi về ngành học, học phí, học bổng, phương thức xét tuyển, hồ sơ hoặc cơ sở của trường.";
            return new StructuredAnswer(answer, []);
        }

        if (IsQuotaQuestion(normalizedQuestion))
        {
            const string answer = "Bảng ngành/chương trình tuyển sinh 2026 trên trang chính thức của Trường Đại học CMC ghi tổng chỉ tiêu là 2.315. Trên cùng trang vẫn có một ô tổng quan hiển thị 2.300, vì vậy nếu cần dùng con số cho hồ sơ chính thức, bạn nên xác nhận lại với Phòng Tuyển sinh qua 024 7102 9999 hoặc tuyensinh@cmcu.edu.vn.";
            return new StructuredAnswer(answer, [BuildStructuredSource("Thông tin tuyển sinh Đại học CMC năm 2026", "https://tuyensinh.cmcu.edu.vn/", answer)]);
        }

        if (IsSchoolHistoryQuestion(normalizedQuestion))
        {
            const string answer = "Theo trang giới thiệu chính thức của Trường Đại học CMC, ngày 26/7/2022 Trường Đại học CMC chính thức được đổi tên theo Quyết định số 895/QĐ-TTg của Thủ tướng Chính phủ. Vì vậy, nếu hỏi theo mốc Trường Đại học CMC hiện nay, hệ thống lấy mốc năm 2022. Trường cũng công bố chiến lược chuyển đổi “AI University” vào ngày 22/7/2024.";
            return new StructuredAnswer(answer, [BuildStructuredSource("Giới thiệu về Trường Đại học CMC", "https://cmcu.edu.vn/dai-hoc-cmc/", answer)]);
        }

        if (IsFacultyStaffQuestion(normalizedQuestion))
        {
            var answer = BuildFacultyStaffAnswer();
            return new StructuredAnswer(answer, [BuildStructuredSource("Đội ngũ giảng viên và chuyên gia tiêu biểu - Trường Đại học CMC", "https://cmcu.edu.vn/giang-vien/", answer)]);
        }

        if (IsContactQuestion(normalizedQuestion))
        {
            var answer = BuildContactAnswer();
            return new StructuredAnswer(answer, [BuildStructuredSource("Liên hệ và cơ sở Trường Đại học CMC", "https://cmcu.edu.vn/lien-he/", answer)]);
        }

        return null;
    }

    private static bool IsGreeting(string normalizedQuestion)
    {
        var words = normalizedQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 8 && (normalizedQuestion.Contains("xin chao", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion is "chao" or "hello" or "hi" or "hey"
            || normalizedQuestion.StartsWith("chao ban", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAdmissionsDomainQuestion(string normalizedQuestion)
    {
        string[] domainSignals =
        [
            "cmc", "cmcu", "tuyen sinh", "xet tuyen", "nganh", "chuong trinh", "hoc phi",
            "hoc bong", "hoc ba", "diem chuan", "diem san", "ho so", "chi tieu", "giang vien", "giao vien",
            "sinh vien", "phu huynh", "cmc-test", "thpt", "to hop", "nguyen vong", "tin chi", "ielts",
            "ky tuc xa", "thuc tap", "viec lam", "co so", "campus", "dia chi", "lien he", "nhap hoc",
            "thanh lap", "doi ten", "hieu truong", "day tot", "chat luong", "uy tin",
        ];
        return domainSignals.Any(signal => normalizedQuestion.Contains(signal, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsQuotaQuestion(string normalizedQuestion)
    {
        return normalizedQuestion.Contains("chi tieu", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("tuyen bao nhieu", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFacultyStaffQuestion(string normalizedQuestion)
    {
        var asksPeople = normalizedQuestion.Contains("giang vien", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("giao vien", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("thay co", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("doi ngu", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("chuyen gia", StringComparison.OrdinalIgnoreCase);
        var asksList = normalizedQuestion.Contains("co nhung ai", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("gom ai", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("danh sach", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("nhung ai", StringComparison.OrdinalIgnoreCase);
        return asksPeople && (asksList || normalizedQuestion.Contains("truong", StringComparison.OrdinalIgnoreCase) || normalizedQuestion.Contains("cmc", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildFacultyStaffAnswer()
    {
        return string.Join(Environment.NewLine, [
            "Theo trang “Đội ngũ giảng viên và chuyên gia tiêu biểu” của Trường Đại học CMC, trường công bố đội ngũ giảng viên theo nhiều khoa/đơn vị. Một số giảng viên, chuyên gia tiêu biểu gồm:",
            "",
            "- Khoa Công nghệ Thông tin & Truyền thông: PGS. TS. Nguyễn Thanh Tùng; PGS. TS. Nguyễn Hữu Quỳnh; PGS. TS. Vũ Việt Vũ; PGS. TS. Trương Anh Hoàng; TS. Phạm Thị Anh Lê; TS. Hoàng Tiểu Bình; TS. Ngô Minh Thành; TS. Nguyễn Ngọc Tân.",
            "- Khoa Kinh doanh & Quản lý: TS. Lê Tiến Trung; PGS. TS. Vũ Trí Dũng; TS. Nguyễn Trà My; TS. Phạm Thị Hà; TS. Phạm Đình Thưởng; TS. Ngô Trí Trung.",
            "- Khoa Mỹ thuật và Thiết kế: NCS. ThS. Nguyễn Minh Kiên; TS. Nguyễn Thị Hà Châu; ThS. Bùi Quỳnh Giang; ThS. Nguyễn Khánh Vân; ThS. Trần Ngọc Anh.",
            "- Khoa Ngôn ngữ: PGS. TS. Nguyễn Thị Việt Thanh; TS. Nguyễn Ngọc Long; TS. Hoàng Thị Yến; TS. Trình Thị Phương Thảo; TS. Hoàng Thị Huế; TS. Nguyễn Phương Thúy.",
            "- Khoa Vi điện tử & Viễn thông: TS. Đặng Minh Tuấn; TS. Ngô Văn Huấn; TS. Nguyễn Thị Thu Hằng; TS. Nguyễn Trung Đô; TS. Lê Hữu Tôn.",
            "- Khoa Đại cương và Trung tâm NN&KN: PGS. TS. Nguyễn Việt Dũng; PGS. TS. Trần Thị Minh Châu; PGS. TS. Nguyễn Thị Minh Tâm; TS. Nguyễn Quang Trưởng; ThS. Lưu Thị Mai Thanh; ThS. Đinh Hồng Ngọc Linh.",
            "",
            "Đây là danh sách tiêu biểu, không phải cam kết là toàn bộ nhân sự đang giảng dạy tại mọi thời điểm. Nếu cần danh sách đầy đủ nhất, nên đối chiếu trực tiếp trang giảng viên chính thức của Trường Đại học CMC."
        ]);
    }

    private static bool IsContactQuestion(string normalizedQuestion)
    {
        var asksContact = normalizedQuestion.Contains("dia chi", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("co so", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("lien he", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("so dien thoai", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("email", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("o dau", StringComparison.OrdinalIgnoreCase);
        var asksSchool = normalizedQuestion.Contains("truong", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("cmc", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("cmcu", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("tuyen sinh", StringComparison.OrdinalIgnoreCase);
        return asksContact && asksSchool;
    }

    private static bool IsSubjectiveQualityQuestion(string normalizedQuestion)
    {
        var asksQuality = normalizedQuestion.Contains("day tot", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("co tot", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("chat luong", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("danh gia", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("review", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("uy tin", StringComparison.OrdinalIgnoreCase);
        var asksSchool = normalizedQuestion.Contains("truong", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("cmc", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("cmcu", StringComparison.OrdinalIgnoreCase);
        return asksQuality && asksSchool;
    }

    private static string BuildContactAnswer()
    {
        return string.Join(Environment.NewLine, [
            "Thông tin liên hệ và cơ sở chính thức của Trường Đại học CMC:",
            "",
            "- Tuyển sinh: tuyensinh@cmcu.edu.vn, 024 7102 9999.",
            "- Nhân sự: recruitment@cmcu.edu.vn, 024 7101 9999.",
            "- Trụ sở chính: CMC Tower, số 11 Duy Tân, Cầu Giấy, Hà Nội.",
            "- Cơ sở 1: số 84C, đường Nguyễn Thanh Bình, Hà Đông, Hà Nội.",
            "- Cơ sở 2: Vạn Phúc Building, đường Tố Hữu, Hà Đông, Hà Nội.",
            "- Cơ sở 3: Tây Mỗ, Xuân Phương, Hà Nội.",
            "- Cơ sở Tân Thuận: Tòa nhà CMC Creative Space, đường số 19, Khu chế xuất Tân Thuận, phường Tân Thuận, TP. Hồ Chí Minh."
        ]);
    }

    private static RagSearchResult BuildStructuredSource(string title, string sourceUrl, string content)
    {
        return new RagSearchResult(
            $"structured-{Math.Abs(HashCode.Combine(title, sourceUrl))}",
            1,
            content,
            title,
            "official_web",
            null,
            sourceUrl);
    }

    private static bool IsSchoolHistoryQuestion(string normalizedQuestion)
    {
        var asksHistory = normalizedQuestion.Contains("thanh lap", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("doi ten", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("ra doi", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("nam nao", StringComparison.OrdinalIgnoreCase);
        var asksSchool = normalizedQuestion.Contains("truong", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("cmc", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("cmcu", StringComparison.OrdinalIgnoreCase);
        return asksHistory && asksSchool;
    }

    private async Task<string?> TryBuildTuitionAnswerAsync(string normalizedQuestion, CancellationToken cancellationToken)
    {
        var programs = await dbContext.Programs
            .Include(program => program.Major)
            .Include(program => program.TuitionFees)
            .Where(program => program.Status == "active" && program.TuitionFees.Any())
            .ToListAsync(cancellationToken);

        var matchedProgram = programs
            .Select(program => new { Program = program, Score = ComputeProgramMatchScore(normalizedQuestion, program) })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .Select(match => match.Program)
            .FirstOrDefault();

        if (matchedProgram is null)
        {
            return null;
        }

        var fees = matchedProgram.TuitionFees
            .Where(fee => fee.AcademicYear.Contains("2026", StringComparison.OrdinalIgnoreCase))
            .OrderBy(fee => fee.AcademicYear)
            .ToList();
        if (fees.Count == 0)
        {
            fees = matchedProgram.TuitionFees.OrderBy(fee => fee.AcademicYear).ToList();
        }

        if (fees.Count == 0)
        {
            return null;
        }

        var lines = new List<string>
        {
            $"Theo dữ liệu tuyển sinh CMCU đã cấu hình trong hệ thống, học phí ngành {matchedProgram.Name} ({matchedProgram.Code}) năm 2026 là:",
        };
        lines.AddRange(fees.Select(fee => $"- {fee.AcademicYear}: {FormatMoney(fee.AmountMin ?? fee.AmountMax)} / {NormalizeTuitionUnit(fee.Unit)}."));
        lines.Add("Các mức trên là học phí theo từng học kỳ; thí sinh nên đối chiếu lại với thông báo học phí chính thức mới nhất của Trường Đại học CMC khi làm thủ tục nhập học.");
        return string.Join(Environment.NewLine, lines);
    }

    private async Task<StructuredAnswer?> TryBuildProgramOverviewAnswerAsync(string normalizedQuestion, CancellationToken cancellationToken)
    {
        var asksProgram = normalizedQuestion.Contains("nganh", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("khoa hoc", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("chuong trinh", StringComparison.OrdinalIgnoreCase);
        var asksOutcome = normalizedQuestion.Contains("ra lam", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("lam nghe", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("nghe gi", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("viec lam", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("co hoi", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("hoc nhu the nao", StringComparison.OrdinalIgnoreCase)
            || normalizedQuestion.Contains("dao tao", StringComparison.OrdinalIgnoreCase);
        if (!asksProgram || !asksOutcome)
        {
            return null;
        }

        var programs = await dbContext.Programs
            .Include(program => program.Major)
            .ThenInclude(major => major.Faculty)
            .Include(program => program.SubjectCombinations)
            .ThenInclude(link => link.SubjectCombination)
            .Include(program => program.TuitionFees)
            .Where(program => program.Status == "active")
            .ToListAsync(cancellationToken);

        var matchedProgram = programs
            .Select(program => new { Program = program, Score = ComputeProgramMatchScore(normalizedQuestion, program) })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .Select(match => match.Program)
            .FirstOrDefault();
        if (matchedProgram is null)
        {
            return null;
        }

        var subjectCombinations = matchedProgram.SubjectCombinations
            .Select(link => $"{link.SubjectCombination.Code}: {link.SubjectCombination.Subjects}")
            .OrderBy(value => value)
            .ToList();
        var tuitionFees = matchedProgram.TuitionFees
            .Where(fee => fee.AcademicYear.Contains("2026", StringComparison.OrdinalIgnoreCase))
            .OrderBy(fee => fee.AcademicYear)
            .ToList();
        if (tuitionFees.Count == 0)
        {
            tuitionFees = matchedProgram.TuitionFees.OrderBy(fee => fee.AcademicYear).ToList();
        }

        var lines = new List<string>
        {
            $"Ngành {matchedProgram.Name} ({matchedProgram.Code}) của Trường Đại học CMC thuộc {matchedProgram.Major.Faculty.Name}.",
            matchedProgram.Major.Description ?? matchedProgram.Description ?? "Hệ thống chưa có mô tả chi tiết cho ngành này.",
            "",
            "Định hướng học tập:",
            ProgramLearningFocus(matchedProgram.Code),
            "",
            "Cơ hội nghề nghiệp:",
            ProgramCareerOutcomes(matchedProgram.Code, matchedProgram.Major.CareerOutcomes),
        };

        if (subjectCombinations.Count > 0)
        {
            lines.Add("");
            lines.Add("Tổ hợp xét tuyển:");
            lines.AddRange(subjectCombinations.Select(value => $"- {value}."));
        }

        if (tuitionFees.Count > 0)
        {
            lines.Add("");
            lines.Add("Học phí tham khảo năm 2026:");
            lines.AddRange(tuitionFees.Select(fee => $"- {fee.AcademicYear}: {FormatMoney(fee.AmountMin ?? fee.AmountMax)} / {NormalizeTuitionUnit(fee.Unit)}."));
        }

        var answer = string.Join(Environment.NewLine, lines);
        return new StructuredAnswer(answer, [BuildStructuredSource($"Dữ liệu ngành {matchedProgram.Name}", "database:academic_programs", answer)]);
    }

    private static string ProgramLearningFocus(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "CS" => "Sinh viên học nền tảng khoa học máy tính, lập trình, cấu trúc dữ liệu, thuật toán, hệ thống máy tính, cơ sở dữ liệu và các hướng ứng dụng như dữ liệu, trí tuệ nhân tạo hoặc phát triển phần mềm.",
            "IT" => "Sinh viên học lập trình, cơ sở dữ liệu, hệ thống thông tin, mạng máy tính, phát triển ứng dụng và vận hành hệ thống công nghệ thông tin.",
            "AI" => "Sinh viên học lập trình, toán cho AI, học máy, khai phá dữ liệu, xử lý dữ liệu và xây dựng mô hình trí tuệ nhân tạo.",
            "SE" => "Sinh viên học quy trình phát triển phần mềm, phân tích yêu cầu, thiết kế hệ thống, kiểm thử, quản trị dự án và triển khai sản phẩm phần mềm.",
            "NS" => "Sinh viên học nền tảng mạng, hệ thống, an toàn thông tin, phòng thủ mạng, kiểm thử xâm nhập và quản trị rủi ro bảo mật.",
            _ => "Sinh viên học kiến thức nền tảng của nhóm ngành, kỹ năng chuyên môn, kỹ năng thực hành và các học phần định hướng nghề nghiệp theo chương trình đào tạo.",
        };
    }

    private static string ProgramCareerOutcomes(string code, string? fallback)
    {
        return code.ToUpperInvariant() switch
        {
            "CS" => "Kỹ sư phần mềm, lập trình viên, chuyên viên dữ liệu, chuyên viên AI/ML, kỹ sư hệ thống, chuyên viên tư vấn công nghệ hoặc nghiên cứu viên trong các nhóm sản phẩm công nghệ.",
            "IT" => "Lập trình viên, kỹ sư hệ thống, chuyên viên quản trị cơ sở dữ liệu, chuyên viên vận hành hệ thống, chuyên viên phân tích nghiệp vụ hoặc tư vấn giải pháp CNTT.",
            "AI" => "Kỹ sư AI, kỹ sư học máy, chuyên viên dữ liệu, kỹ sư xử lý dữ liệu, chuyên viên phân tích dữ liệu hoặc nghiên cứu viên ứng dụng AI.",
            "SE" => "Kỹ sư phần mềm, lập trình viên backend/frontend/mobile, kiểm thử phần mềm, kỹ sư DevOps, phân tích nghiệp vụ hoặc quản lý dự án phần mềm.",
            "NS" => "Chuyên viên an toàn thông tin, chuyên viên SOC, chuyên viên kiểm thử xâm nhập, kỹ sư bảo mật hệ thống, kỹ sư mạng hoặc tư vấn bảo mật.",
            _ => fallback ?? "Vị trí nghề nghiệp phụ thuộc định hướng chuyên ngành, năng lực cá nhân và nhu cầu tuyển dụng tại thời điểm tốt nghiệp.",
        };
    }

    private static int ComputeProgramMatchScore(string normalizedQuestion, AcademicProgram program)
    {
        var score = 0;
        var code = NormalizeForSearch(program.Code);
        var programName = NormalizeForSearch(program.Name);
        var majorName = NormalizeForSearch(program.Major.Name);
        if (normalizedQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            score += 3;
        }

        if (normalizedQuestion.Contains(programName, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        if (normalizedQuestion.Contains(majorName, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        foreach (var alias in ProgramAliases(program.Code))
        {
            if (normalizedQuestion.Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                score += 4;
            }
        }

        return score;
    }

    private static IReadOnlyCollection<string> ProgramAliases(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "AI" => ["tri tue nhan tao", "artificial intelligence"],
            "IT" => ["cong nghe thong tin", "cntt", "information technology"],
            "CS" => ["khoa hoc may tinh", "computer science"],
            "SE" => ["ky thuat phan mem", "software engineering"],
            "NS" => ["an ninh mang", "cyber security"],
            "EC" => ["dien tu vien thong", "thiet ke vi mach", "ban dan"],
            _ => [],
        };
    }

    private static string FormatMoney(decimal? amount)
    {
        return amount is null ? "chưa công bố" : $"{amount.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} VNĐ";
    }

    private static string NormalizeTuitionUnit(string unit)
    {
        return NormalizeForSearch(unit) switch
        {
            "hoc ky" or "semester" => "học kỳ",
            "nam" or "year" => "năm",
            _ => string.IsNullOrWhiteSpace(unit) ? "học kỳ" : unit,
        };
    }

    private static RagSearchResult FocusSourceContent(string question, RagSearchResult source)
    {
        var needles = GetFocusNeedles(question);
        foreach (var needle in needles)
        {
            var index = source.Content.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var start = FindReadableStart(source.Content, Math.Max(0, index - 80));
            var length = Math.Min(source.Content.Length - start, 1200);
            var end = FindReadableEnd(source.Content, start + length);
            var snippet = RestoreCommonVietnameseTerms(source.Content[start..end].Trim());

            return source with { Content = snippet };
        }

        return source;
    }

    private static int FindReadableStart(string content, int desiredStart)
    {
        if (desiredStart <= 0)
        {
            return 0;
        }

        var newline = content.LastIndexOf('\n', desiredStart);
        if (newline >= 0 && desiredStart - newline <= 160)
        {
            return newline + 1;
        }

        var sentence = content.LastIndexOfAny(['.', '!', '?'], desiredStart);
        return sentence >= 0 && desiredStart - sentence <= 160 ? sentence + 1 : desiredStart;
    }

    private static int FindReadableEnd(string content, int desiredEnd)
    {
        if (desiredEnd >= content.Length)
        {
            return content.Length;
        }

        var sentence = content.LastIndexOfAny(['.', '!', '?', '\n'], Math.Min(desiredEnd, content.Length - 1));
        return sentence > 0 && sentence > desiredEnd - 320 ? sentence + 1 : desiredEnd;
    }

    private static string TrimToSentenceBoundary(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        var end = value.LastIndexOfAny(['.', '!', '?'], Math.Min(maxLength, value.Length - 1));
        if (end >= 120)
        {
            return value[..(end + 1)].Trim();
        }

        return value[..maxLength].Trim();
    }

    private static IReadOnlyCollection<string> GetFocusNeedles(string question)
    {
        var normalizedQuestion = NormalizeForSearch(question);
        if (normalizedQuestion.Contains("ho so", StringComparison.OrdinalIgnoreCase))
        {
            return ["## Hồ sơ xét tuyển", "Hồ sơ xét tuyển trực tuyến", "Hồ sơ xét tuyển"];
        }

        if (normalizedQuestion.Contains("hoc phi", StringComparison.OrdinalIgnoreCase))
        {
            return ["## Học phí", "Nhóm Máy tính và Công nghệ thông tin", "Học kỳ 1-3"];
        }

        if (normalizedQuestion.Contains("phuong thuc", StringComparison.OrdinalIgnoreCase))
        {
            return ["## Phương thức tuyển sinh", "CMC401"];
        }

        if (normalizedQuestion.Contains("chi tieu", StringComparison.OrdinalIgnoreCase))
        {
            return ["## Chỉ tiêu tuyển sinh", "2.315", "2.300"];
        }

        if (normalizedQuestion.Contains("diem chuan", StringComparison.OrdinalIgnoreCase))
        {
            return ["## Điểm chuẩn", "không tự bịa điểm chuẩn"];
        }

        return [];
    }

    private static string RestoreCommonVietnameseTerms(string value)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ho so xet tuyen"] = "Hồ sơ xét tuyển",
            ["phieu dang ky xet tuyen"] = "phiếu đăng ký xét tuyển",
            ["hoc ba"] = "học bạ",
            ["bang diem"] = "bảng điểm",
            ["can cuoc cong dan"] = "căn cước công dân",
            ["giay chung nhan"] = "giấy chứng nhận",
            ["tot nghiep"] = "tốt nghiệp",
            ["tam thoi"] = "tạm thời",
            ["le phi"] = "lệ phí",
            ["thi sinh"] = "thí sinh",
            ["tuyen sinh"] = "tuyển sinh",
            ["hoc phi"] = "học phí",
            ["quy che"] = "quy chế",
            ["thong bao"] = "thông báo",
            ["dai hoc"] = "đại học",
        };

        foreach (var replacement in replacements)
        {
            value = value.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    private static string BuildExtractiveAnswer(IReadOnlyCollection<RagSearchResult> strongSources)
    {
        var answerLines = new List<string>
        {
            "Dựa trên các tài liệu đã nạp vào kho RAG, có thể tóm tắt như sau:",
        };
        foreach (var source in strongSources)
        {
            answerLines.Add($"- {TrimForAnswer(source.Content)} [Nguồn: {source.Title ?? "Tài liệu"}, đoạn {source.PointId[..Math.Min(8, source.PointId.Length)]}]");
        }

        return string.Join(Environment.NewLine, answerLines);
    }

    private async Task<StoredChatIds> StoreChatAsync(
        RagChatRequest request,
        Guid? userId,
        string answer,
        string backend,
        IReadOnlyCollection<RagSearchResult> sources,
        int latencyMs,
        CancellationToken cancellationToken)
    {
        var conversation = await ResolveConversationAsync(request, userId, cancellationToken);
        var now = DateTime.UtcNow;
        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            UserId = userId,
            Role = "user",
            Content = request.Question.Trim(),
            CreatedAt = now,
        };
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = answer,
            RetrievalBackend = backend,
            LatencyMs = latencyMs,
            CreatedAt = now.AddMilliseconds(1),
            Sources = sources.Select(source => new ChatMessageSource
            {
                PointId = source.PointId,
                Score = source.Score,
                Content = source.Content,
                Title = source.Title,
                DocumentType = source.DocumentType,
                PageNumber = source.PageNumber,
                SectionTitle = source.SectionTitle,
            }).ToList(),
        };

        conversation.UpdatedAt = now;
        dbContext.ChatMessages.Add(userMessage);
        dbContext.ChatMessages.Add(assistantMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new StoredChatIds(conversation.Id, userMessage.Id, assistantMessage.Id);
    }

    private async Task<ChatConversation> ResolveConversationAsync(RagChatRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        ChatConversation? conversation = null;
        if (request.ConversationId is { } conversationId)
        {
            var trimmedSessionId = string.IsNullOrWhiteSpace(request.ClientSessionId) ? null : request.ClientSessionId.Trim();
            conversation = await dbContext.ChatConversations
                .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

            if (conversation is not null)
            {
                var allowed = userId is not null
                    ? conversation.UserId == userId
                    : conversation.UserId is null
                        && trimmedSessionId is not null
                        && conversation.ClientSessionId == trimmedSessionId;

                if (!allowed)
                {
                    conversation = null;
                }
            }
        }

        if (conversation is not null)
        {
            return conversation;
        }

        conversation = new ChatConversation
        {
            UserId = userId,
            ClientSessionId = string.IsNullOrWhiteSpace(request.ClientSessionId) ? null : request.ClientSessionId.Trim(),
            Title = BuildConversationTitle(request.Question),
        };
        dbContext.ChatConversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private static string BuildConversationTitle(string question)
    {
        var normalized = question.ReplaceLineEndings(" ").Trim();
        if (normalized.Length == 0)
        {
            return "Cuộc trò chuyện mới";
        }

        return normalized.Length <= 80 ? normalized : normalized[..80] + "...";
    }

    private static string NormalizeRating(string rating)
    {
        var normalized = rating.Trim().ToLowerInvariant();
        return normalized switch
        {
            "positive" or "helpful" or "like" => "positive",
            "negative" or "not_helpful" or "dislike" => "negative",
            _ => throw new InvalidOperationException("Rating must be positive or negative."),
        };
    }

    private static ChatFeedbackDto ToFeedbackDto(ChatFeedback feedback, ChatMessage assistantMessage, Guid? handoffTicketId)
    {
        var question = assistantMessage.Conversation.Messages
            .Where(x => x.Role == "user" && x.CreatedAt <= assistantMessage.CreatedAt)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Content)
            .FirstOrDefault() ?? string.Empty;

        return new ChatFeedbackDto(
            feedback.Id,
            feedback.MessageId,
            feedback.UserId,
            feedback.User?.Email,
            feedback.Rating,
            feedback.Note,
            assistantMessage.Content,
            question,
            feedback.CreatedAt,
            handoffTicketId);
    }

    private sealed record StructuredAnswer(string Answer, IReadOnlyCollection<RagSearchResult> Sources);

    private sealed record StoredChatIds(Guid ConversationId, Guid UserMessageId, Guid AssistantMessageId);
}
