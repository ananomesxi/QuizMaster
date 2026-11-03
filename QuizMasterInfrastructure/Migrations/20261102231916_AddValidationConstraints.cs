using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMasterInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_StudentAnswers_QuestionId",
                table: "StudentAnswers");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Quizzes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Questions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "AnswerOptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_QuestionId_QuizAttemptId_AnswerOptionId",
                table: "StudentAnswers",
                columns: new[] { "QuestionId", "QuizAttemptId", "AnswerOptionId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quiz_TimeLimitMinutes_Positive",
                table: "Quizzes",
                sql: "[TimeLimitMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizResults_Score_Within_Maximum",
                table: "QuizResults",
                sql: "[Score] >= 0 AND [MaxScore] >= 0 AND [Score] <= [MaxScore]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizAttempts_FinishedAt_After_StartedAt",
                table: "QuizAttempts",
                sql: "[FinishedAt] IS NULL OR [FinishedAt] >= [StartedAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Question_Points_Positive",
                table: "Questions",
                sql: "[Points] > 0");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_StudentAnswers_QuestionId_QuizAttemptId_AnswerOptionId",
                table: "StudentAnswers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Quiz_TimeLimitMinutes_Positive",
                table: "Quizzes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizResults_Score_Within_Maximum",
                table: "QuizResults");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizAttempts_FinishedAt_After_StartedAt",
                table: "QuizAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Question_Points_Positive",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Quizzes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "AnswerOptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_QuestionId",
                table: "StudentAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Quizzes_QuizId",
                table: "QuizAttempts",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
