using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Polls.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    friendly_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    xml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    modified_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_polls", x => x.id);
                    table.CheckConstraint("ck_polls_title_not_empty", "LEN([title]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "threshold_warnings",
                columns: table => new
                {
                    resource = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_threshold_warnings", x => x.resource);
                    table.CheckConstraint("ck_threshold_warnings_resource", "[resource] IN ('users', 'polls')");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_name_not_empty", "LEN([name]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_poll = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    modified_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questions", x => x.id);
                    table.CheckConstraint("ck_questions_question_not_empty", "LEN([question]) > 0");
                    table.ForeignKey(
                        name: "fk_questions_polls",
                        column: x => x.id_poll,
                        principalTable: "polls",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_poll",
                columns: table => new
                {
                    id_poll = table.Column<int>(type: "int", nullable: false),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    is_owner = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_poll", x => new { x.id_poll, x.id_user });
                    table.ForeignKey(
                        name: "fk_user_poll_polls",
                        column: x => x.id_poll,
                        principalTable: "polls",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_poll_users",
                        column: x => x.id_user,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "answers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_question = table.Column<int>(type: "int", nullable: false),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    modified_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answers", x => x.id);
                    table.CheckConstraint("ck_answers_answer_not_empty", "LEN([answer]) > 0");
                    table.ForeignKey(
                        name: "fk_answers_questions",
                        column: x => x.id_question,
                        principalTable: "questions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_answers_users",
                        column: x => x.id_user,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_answers_user",
                table: "answers",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "ux_answers_question_user",
                table: "answers",
                columns: new[] { "id_question", "id_user" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_questions_poll_sort_order",
                table: "questions",
                columns: new[] { "id_poll", "sort_order" },
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_poll_user",
                table: "user_poll",
                column: "id_user");

            migrationBuilder.CreateIndex(
                name: "ux_user_poll_owner",
                table: "user_poll",
                column: "id_poll",
                unique: true,
                filter: "[is_owner] = 1");

            migrationBuilder.CreateIndex(
                name: "ux_users_name",
                table: "users",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answers");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "threshold_warnings");

            migrationBuilder.DropTable(
                name: "user_poll");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "polls");
        }
    }
}
