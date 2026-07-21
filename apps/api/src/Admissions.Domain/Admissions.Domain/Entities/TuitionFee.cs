namespace Admissions.Domain.Entities;

public sealed class TuitionFee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProgramId { get; set; }
    public AcademicProgram Program { get; set; } = null!;

    public string AcademicYear { get; set; } = string.Empty;
    public decimal? AmountMin { get; set; }
    public decimal? AmountMax { get; set; }
    public string Currency { get; set; } = "VND";
    public string Unit { get; set; } = "year";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
