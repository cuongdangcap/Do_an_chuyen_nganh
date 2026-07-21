using Admissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Persistence;

public sealed class AdmissionsDbContext(DbContextOptions<AdmissionsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ParentProfile> ParentProfiles => Set<ParentProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AdmissionCycle> AdmissionCycles => Set<AdmissionCycle>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Major> Majors => Set<Major>();
    public DbSet<AcademicProgram> Programs => Set<AcademicProgram>();
    public DbSet<SubjectCombination> SubjectCombinations => Set<SubjectCombination>();
    public DbSet<ProgramSubjectCombination> ProgramSubjectCombinations => Set<ProgramSubjectCombination>();
    public DbSet<AdmissionMethod> AdmissionMethods => Set<AdmissionMethod>();
    public DbSet<CutoffScore> CutoffScores => Set<CutoffScore>();
    public DbSet<TuitionFee> TuitionFees => Set<TuitionFee>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatMessageSource> ChatMessageSources => Set<ChatMessageSource>();
    public DbSet<ChatFeedback> ChatFeedback => Set<ChatFeedback>();
    public DbSet<EvaluationQuestion> EvaluationQuestions => Set<EvaluationQuestion>();
    public DbSet<EvaluationRun> EvaluationRuns => Set<EvaluationRun>();
    public DbSet<EvaluationResult> EvaluationResults => Set<EvaluationResult>();
    public DbSet<HandoffTicket> HandoffTickets => Set<HandoffTicket>();
    public DbSet<HandoffMessage> HandoffMessages => Set<HandoffMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("student_profiles");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.HighSchool).HasMaxLength(255);
            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.ExpectedScore).HasPrecision(5, 2);
            entity.Property(x => x.ExamScore).HasPrecision(5, 2);
            entity.Property(x => x.InterestedSubjectGroup).HasMaxLength(50);
            entity.HasOne(x => x.User).WithOne(x => x.StudentProfile).HasForeignKey<StudentProfile>(x => x.UserId);
        });

        modelBuilder.Entity<ParentProfile>(entity =>
        {
            entity.ToTable("parent_profiles");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Occupation).HasMaxLength(255);
            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.ContactPreference).HasMaxLength(50);
            entity.HasOne(x => x.User).WithOne(x => x.ParentProfile).HasForeignKey<ParentProfile>(x => x.UserId);
        });

        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.ToTable("staff_profiles");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Department).HasMaxLength(255);
            entity.Property(x => x.Position).HasMaxLength(255);
            entity.HasOne(x => x.User).WithOne(x => x.StaffProfile).HasForeignKey<StaffProfile>(x => x.UserId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.TokenHash).IsRequired();
            entity.Property(x => x.CreatedByIp).HasMaxLength(50);
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<AdmissionCycle>(entity =>
        {
            entity.ToTable("admission_cycles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Year).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("faculties");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Major>(entity =>
        {
            entity.ToTable("majors");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.FacultyId);
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Faculty).WithMany(x => x.Majors).HasForeignKey(x => x.FacultyId);
        });

        modelBuilder.Entity<AcademicProgram>(entity =>
        {
            entity.ToTable("programs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.MajorId);
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.DegreeType).HasMaxLength(100);
            entity.Property(x => x.Language).HasMaxLength(100);
            entity.Property(x => x.Campus).HasMaxLength(255);
            entity.Property(x => x.DurationYears).HasPrecision(3, 1);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Major).WithMany(x => x.Programs).HasForeignKey(x => x.MajorId);
        });

        modelBuilder.Entity<SubjectCombination>(entity =>
        {
            entity.ToTable("subject_combinations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Subjects).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<ProgramSubjectCombination>(entity =>
        {
            entity.ToTable("program_subject_combinations");
            entity.HasKey(x => new { x.ProgramId, x.SubjectCombinationId });
            entity.HasOne(x => x.Program).WithMany(x => x.SubjectCombinations).HasForeignKey(x => x.ProgramId);
            entity.HasOne(x => x.SubjectCombination).WithMany(x => x.Programs).HasForeignKey(x => x.SubjectCombinationId);
        });

        modelBuilder.Entity<AdmissionMethod>(entity =>
        {
            entity.ToTable("admission_methods");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<CutoffScore>(entity =>
        {
            entity.ToTable("cutoff_scores");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProgramId, x.AdmissionCycleId, x.AdmissionMethodId, x.SubjectCombinationId });
            entity.Property(x => x.Score).HasPrecision(5, 2);
            entity.HasOne(x => x.Program).WithMany(x => x.CutoffScores).HasForeignKey(x => x.ProgramId);
            entity.HasOne(x => x.AdmissionCycle).WithMany(x => x.CutoffScores).HasForeignKey(x => x.AdmissionCycleId);
            entity.HasOne(x => x.AdmissionMethod).WithMany(x => x.CutoffScores).HasForeignKey(x => x.AdmissionMethodId);
            entity.HasOne(x => x.SubjectCombination).WithMany().HasForeignKey(x => x.SubjectCombinationId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TuitionFee>(entity =>
        {
            entity.ToTable("tuition_fees");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProgramId, x.AcademicYear });
            entity.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AmountMin).HasPrecision(18, 2);
            entity.Property(x => x.AmountMax).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Program).WithMany(x => x.TuitionFees).HasForeignKey(x => x.ProgramId);
        });

        modelBuilder.Entity<FaqItem>(entity =>
        {
            entity.ToTable("faqs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.Question).IsRequired();
            entity.Property(x => x.Answer).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.ToTable("knowledge_documents");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.UploadedBy);
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(500);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DocumentId);
            entity.HasIndex(x => x.ProcessingStatus);
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.FileType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.Checksum).HasMaxLength(128);
            entity.Property(x => x.ProcessingStatus).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Document)
                .WithMany(x => x.Versions)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DocumentVersionId, x.ChunkIndex });
            entity.HasIndex(x => x.QdrantPointId);
            entity.Property(x => x.SectionTitle).HasMaxLength(255);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.QdrantCollection).HasMaxLength(100).IsRequired();
            entity.Property(x => x.QdrantPointId).HasMaxLength(100);
            entity.HasOne(x => x.DocumentVersion)
                .WithMany(x => x.Chunks)
                .HasForeignKey(x => x.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IngestionJob>(entity =>
        {
            entity.ToTable("ingestion_jobs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DocumentVersionId);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.JobType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.DocumentVersion)
                .WithMany(x => x.IngestionJobs)
                .HasForeignKey(x => x.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.ToTable("chat_conversations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.ClientSessionId);
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.ClientSessionId).HasMaxLength(100);
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ConversationId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Role);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.RetrievalBackend).HasMaxLength(50);
            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ChatMessageSource>(entity =>
        {
            entity.ToTable("chat_message_sources");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MessageId);
            entity.HasIndex(x => x.PointId);
            entity.Property(x => x.PointId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.DocumentType).HasMaxLength(50);
            entity.Property(x => x.SectionTitle).HasMaxLength(255);
            entity.HasOne(x => x.Message)
                .WithMany(x => x.Sources)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatFeedback>(entity =>
        {
            entity.ToTable("chat_feedback");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MessageId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Rating);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.Rating).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.HasOne(x => x.Message)
                .WithMany(x => x.Feedback)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<EvaluationQuestion>(entity =>
        {
            entity.ToTable("evaluation_questions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Category);
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Question).IsRequired();
            entity.Property(x => x.ExpectedAnswer).IsRequired();
            entity.Property(x => x.ExpectedKeywordsJson).IsRequired();
            entity.Property(x => x.ExpectedSourceTitle).HasMaxLength(255);
            entity.Property(x => x.ExpectedDocumentType).HasMaxLength(50);
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<EvaluationRun>(entity =>
        {
            entity.ToTable("evaluation_runs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.StartedAt);
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<EvaluationResult>(entity =>
        {
            entity.ToTable("evaluation_results");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EvaluationRunId);
            entity.HasIndex(x => x.EvaluationQuestionId);
            entity.HasIndex(x => x.IsCorrect);
            entity.Property(x => x.RetrievalBackend).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AnswerPreview).IsRequired();
            entity.Property(x => x.MatchedKeywordsJson).IsRequired();
            entity.Property(x => x.SourcesJson).IsRequired();
            entity.HasOne(x => x.EvaluationRun)
                .WithMany(x => x.Results)
                .HasForeignKey(x => x.EvaluationRunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.EvaluationQuestion)
                .WithMany(x => x.Results)
                .HasForeignKey(x => x.EvaluationQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HandoffTicket>(entity =>
        {
            entity.ToTable("handoff_tickets");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ConversationId);
            entity.HasIndex(x => x.SourceMessageId);
            entity.HasIndex(x => x.FeedbackId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Question).IsRequired();
            entity.Property(x => x.AiAnswer).IsRequired();
            entity.Property(x => x.StaffReplyPreview).HasMaxLength(1000);
            entity.HasOne(x => x.Conversation)
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.SourceMessage)
                .WithMany()
                .HasForeignKey(x => x.SourceMessageId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Feedback)
                .WithMany()
                .HasForeignKey(x => x.FeedbackId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.AssignedToUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<HandoffMessage>(entity =>
        {
            entity.ToTable("handoff_messages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TicketId);
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.SenderRole).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SenderUser)
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
