using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizMasterInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Quizzes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CreatedById",
                table: "Quizzes",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_AspNetUsers_CreatedById",
                table: "Quizzes",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_AspNetUsers_CreatedById",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_CreatedById",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Quizzes");
        }
    }
}
