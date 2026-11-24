using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceAnalyzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyDocumentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyDocumentPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyDocumentId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    ExtractionSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CharacterCount = table.Column<int>(type: "int", nullable: false),
                    AlphanumericCount = table.Column<int>(type: "int", nullable: false),
                    SuspiciousCharacterCount = table.Column<int>(type: "int", nullable: false),
                    SuspiciousCharacterRatio = table.Column<double>(type: "float", nullable: false),
                    QualityStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QualityWarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcrAttempted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDocumentPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyDocumentPages_PolicyDocuments_PolicyDocumentId",
                        column: x => x.PolicyDocumentId,
                        principalTable: "PolicyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocumentPages_PolicyDocumentId_PageNumber",
                table: "PolicyDocumentPages",
                columns: new[] { "PolicyDocumentId", "PageNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyDocumentPages");
        }
    }
}
