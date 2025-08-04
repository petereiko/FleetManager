using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class InitTimeOffCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CompanyBranchId",
                table: "TimeOffCategories",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeOffCategories_CompanyBranchId",
                table: "TimeOffCategories",
                column: "CompanyBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeOffCategories_CompanyBranches_CompanyBranchId",
                table: "TimeOffCategories",
                column: "CompanyBranchId",
                principalTable: "CompanyBranches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeOffCategories_CompanyBranches_CompanyBranchId",
                table: "TimeOffCategories");

            migrationBuilder.DropIndex(
                name: "IX_TimeOffCategories_CompanyBranchId",
                table: "TimeOffCategories");

            migrationBuilder.DropColumn(
                name: "CompanyBranchId",
                table: "TimeOffCategories");
        }
    }
}
