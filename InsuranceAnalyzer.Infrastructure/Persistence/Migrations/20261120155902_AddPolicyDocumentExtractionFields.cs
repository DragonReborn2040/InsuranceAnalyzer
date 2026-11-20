using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceAnalyzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyDocumentExtractionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PolicyDocuments_InsurancePolicyId",
                table: "PolicyDocuments");

            migrationBuilder.AddColumn<string>(
                name: "ExtractedText",
                table: "PolicyDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PageCount",
                table: "PolicyDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                table: "PolicyDocuments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_InsurancePolicyId_Sha256",
                table: "PolicyDocuments",
                columns: new[] { "InsurancePolicyId", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_Sha256",
                table: "PolicyDocuments",
                column: "Sha256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PolicyDocuments_InsurancePolicyId_Sha256",
                table: "PolicyDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PolicyDocuments_Sha256",
                table: "PolicyDocuments");

            migrationBuilder.DropColumn(
                name: "ExtractedText",
                table: "PolicyDocuments");

            migrationBuilder.DropColumn(
                name: "PageCount",
                table: "PolicyDocuments");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                table: "PolicyDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_InsurancePolicyId",
                table: "PolicyDocuments",
                column: "InsurancePolicyId");
        }
    }
}
