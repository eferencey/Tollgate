using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tollgate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedEvidence",
                columns: table => new
                {
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceJson = table.Column<string>(type: "TEXT", nullable: false),
                    ScannedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedEvidence", x => x.Url);
                });

            migrationBuilder.CreateTable(
                name: "ScanHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Verdict = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<string>(type: "TEXT", nullable: false),
                    ScannedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedEvidence_ScannedAt",
                table: "CachedEvidence",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScanHistory_Host",
                table: "ScanHistory",
                column: "Host");

            migrationBuilder.CreateIndex(
                name: "IX_ScanHistory_ScannedAt",
                table: "ScanHistory",
                column: "ScannedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedEvidence");

            migrationBuilder.DropTable(
                name: "ScanHistory");
        }
    }
}
