using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class InitContactDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleToCompanyRentals_Companies_CompanyId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleToCompanyRentals_Vehicles_VehicleId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.DropIndex(
                name: "IX_VehicleToCompanyRentals_CompanyId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.RenameColumn(
                name: "VehicleId",
                table: "VehicleToCompanyRentals",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_VehicleToCompanyRentals_VehicleId",
                table: "VehicleToCompanyRentals",
                newName: "IX_VehicleToCompanyRentals_VendorId");

            migrationBuilder.AddColumn<long>(
                name: "CompanyBranchId",
                table: "VehicleToCompanyRentals",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyBranhcId",
                table: "VehicleToCompanyRentals",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactDirectories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyBranchId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    Services = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactDirectories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactDirectories_CompanyBranches_CompanyBranchId",
                        column: x => x.CompanyBranchId,
                        principalTable: "CompanyBranches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactDirectories_VendorCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "VendorCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleToCompanyRentals_CompanyBranchId",
                table: "VehicleToCompanyRentals",
                column: "CompanyBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactDirectories_CategoryId",
                table: "ContactDirectories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactDirectories_CompanyBranchId",
                table: "ContactDirectories",
                column: "CompanyBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleToCompanyRentals_CompanyBranches_CompanyBranchId",
                table: "VehicleToCompanyRentals",
                column: "CompanyBranchId",
                principalTable: "CompanyBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleToCompanyRentals_Vendors_VendorId",
                table: "VehicleToCompanyRentals",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleToCompanyRentals_CompanyBranches_CompanyBranchId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleToCompanyRentals_Vendors_VendorId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.DropTable(
                name: "ContactDirectories");

            migrationBuilder.DropIndex(
                name: "IX_VehicleToCompanyRentals_CompanyBranchId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.DropColumn(
                name: "CompanyBranchId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.DropColumn(
                name: "CompanyBranhcId",
                table: "VehicleToCompanyRentals");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "VehicleToCompanyRentals",
                newName: "VehicleId");

            migrationBuilder.RenameIndex(
                name: "IX_VehicleToCompanyRentals_VendorId",
                table: "VehicleToCompanyRentals",
                newName: "IX_VehicleToCompanyRentals_VehicleId");

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "VehicleToCompanyRentals",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleToCompanyRentals_CompanyId",
                table: "VehicleToCompanyRentals",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleToCompanyRentals_Companies_CompanyId",
                table: "VehicleToCompanyRentals",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleToCompanyRentals_Vehicles_VehicleId",
                table: "VehicleToCompanyRentals",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
