using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KaizenBoard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdeaHubServiceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "Suggestions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ServiceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    SupportEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AnonymousEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BoardEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    NotifyAuthor = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestionActivities_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionSupports",
                columns: table => new
                {
                    SuggestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionSupports", x => new { x.SuggestionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SuggestionSupports_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuggestionSupports_Suggestions_SuggestionId",
                        column: x => x.SuggestionId,
                        principalTable: "Suggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Templates",
                columns: new[] { "Id", "Created", "CreatedBy", "DepartmentId", "ImprovementObject", "IsActive", "LastModified", "LastModifiedBy", "ProblemHint", "SolutionHint", "Title", "Type" },
                values: new object[,]
                {
                    { new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0101"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Оборудование или инструмент", true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Какое оборудование или инструмент мешает работе?", "Что нужно изменить или заменить?", "Модернизация", 0 },
                    { new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0102"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Рабочий процесс", true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "На каком шаге теряется время?", "Какие шаги можно убрать или упростить?", "Сокращение времени", 0 },
                    { new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0103"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Безопасность", true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Где возникает риск и кто с ним сталкивается?", "Как устранить риск или снизить его?", "Безопасность", 0 },
                    { new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0104"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Операция контроля качества", true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "В каком месте процесса возникает дефект?", "Как предупредить или раньше обнаружить дефект?", "Качество", 0 },
                    { new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0105"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Расход материалов или энергии", true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Где возникает лишний расход?", "Как уменьшить расход без потери качества?", "Экономия ресурсов", 0 },
                    { new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0106"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "Рабочее место или процесс", true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Что сейчас мешает выполнить работу?", "Какое изменение поможет?", "Другое", 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionActivities_SuggestionId_OccurredAt",
                table: "SuggestionActivities",
                columns: new[] { "SuggestionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionSupports_UserId",
                table: "SuggestionSupports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceSettings");

            migrationBuilder.DropTable(
                name: "SuggestionActivities");

            migrationBuilder.DropTable(
                name: "SuggestionSupports");

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0101"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0102"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0103"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0104"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0105"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("da28c801-5d1f-4dfa-88ed-028b00eb0106"));

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "Suggestions");
        }
    }
}
