using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admissions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HandoffSupportSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "handoff_tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FeedbackId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StaffReplyPreview = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handoff_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handoff_tickets_chat_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "chat_conversations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_handoff_tickets_chat_feedback_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "chat_feedback",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_handoff_tickets_chat_messages_SourceMessageId",
                        column: x => x.SourceMessageId,
                        principalTable: "chat_messages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_handoff_tickets_users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_handoff_tickets_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "handoff_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SenderRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handoff_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handoff_messages_handoff_tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "handoff_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_handoff_messages_users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_handoff_messages_CreatedAt",
                table: "handoff_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_messages_SenderUserId",
                table: "handoff_messages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_messages_TicketId",
                table: "handoff_messages",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_tickets_AssignedToUserId",
                table: "handoff_tickets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_tickets_ConversationId",
                table: "handoff_tickets",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_tickets_CreatedAt",
                table: "handoff_tickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_tickets_CreatedByUserId",
                table: "handoff_tickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_tickets_FeedbackId",
                table: "handoff_tickets",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_tickets_SourceMessageId",
                table: "handoff_tickets",
                column: "SourceMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_handoff_tickets_Status",
                table: "handoff_tickets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "handoff_messages");

            migrationBuilder.DropTable(
                name: "handoff_tickets");
        }
    }
}
