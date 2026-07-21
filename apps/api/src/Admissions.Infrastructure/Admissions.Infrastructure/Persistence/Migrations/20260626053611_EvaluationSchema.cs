using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EvaluationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evaluation_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedKeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedSourceTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExpectedDocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TopK = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectQuestions = table.Column<int>(type: "int", nullable: false),
                    HitRateAtK = table.Column<double>(type: "float", nullable: false),
                    AverageKeywordHitRate = table.Column<double>(type: "float", nullable: false),
                    AverageTopScore = table.Column<double>(type: "float", nullable: false),
                    AverageLatencyMs = table.Column<double>(type: "float", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RetrievalBackend = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TopK = table.Column<int>(type: "int", nullable: false),
                    TopScore = table.Column<double>(type: "float", nullable: false),
                    HitAtK = table.Column<bool>(type: "bit", nullable: false),
                    KeywordHitRate = table.Column<double>(type: "float", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: false),
                    AnswerPreview = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchedKeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourcesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_results_evaluation_questions_EvaluationQuestionId",
                        column: x => x.EvaluationQuestionId,
                        principalTable: "evaluation_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluation_results_evaluation_runs_EvaluationRunId",
                        column: x => x.EvaluationRunId,
                        principalTable: "evaluation_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_questions_Category",
                table: "evaluation_questions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_questions_Code",
                table: "evaluation_questions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_questions_IsActive",
                table: "evaluation_questions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_results_EvaluationQuestionId",
                table: "evaluation_results",
                column: "EvaluationQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_results_EvaluationRunId",
                table: "evaluation_results",
                column: "EvaluationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_results_IsCorrect",
                table: "evaluation_results",
                column: "IsCorrect");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_runs_StartedAt",
                table: "evaluation_runs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_runs_Status",
                table: "evaluation_runs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evaluation_results");

            migrationBuilder.DropTable(
                name: "evaluation_questions");

            migrationBuilder.DropTable(
                name: "evaluation_runs");
        }
    }
}
