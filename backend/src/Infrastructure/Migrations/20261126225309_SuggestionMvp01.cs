using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KaizenBoard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SuggestionMvp01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreationKey",
                table: "Suggestions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreationPayloadHash",
                table: "Suggestions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionCount",
                table: "Suggestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_CreatedBy_CreationKey",
                table: "Suggestions",
                columns: new[] { "CreatedBy", "CreationKey" },
                unique: true,
                filter: "\"CreationKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suggestions_CreatedBy_CreationKey",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "CreationKey",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "CreationPayloadHash",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "SubmissionCount",
                table: "Suggestions");
        }
    }
}
