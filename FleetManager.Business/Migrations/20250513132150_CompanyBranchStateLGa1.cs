using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class CompanyBranchStateLGa1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "VehicleDocuments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "States");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "NextOfKins");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "LGAs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DriverVehicles");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DriverVehicleLocations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ActivityLogs");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "Vehicles",
                newName: "CompanyBranchId");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "Drivers",
                newName: "CompanyBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CompanyBranchId",
                table: "Vehicles",
                column: "CompanyBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_CompanyBranches_CompanyBranchId",
                table: "Vehicles",
                column: "CompanyBranchId",
                principalTable: "CompanyBranches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_CompanyBranches_CompanyBranchId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CompanyBranchId",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "CompanyBranchId",
                table: "Vehicles",
                newName: "CompanyId");

            migrationBuilder.RenameColumn(
                name: "CompanyBranchId",
                table: "Drivers",
                newName: "CompanyId");

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "VehicleDocuments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "States",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "NextOfKins",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "LGAs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "EmailLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "DriverVehicles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "DriverVehicleLocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "Companies",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "ActivityLogs",
                type: "bigint",
                nullable: true);
        }
    }
}
