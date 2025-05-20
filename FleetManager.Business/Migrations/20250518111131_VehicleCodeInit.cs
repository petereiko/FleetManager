using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class VehicleCodeInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "VehicleDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "VehicleDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "NextOfKins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "EmailLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "DriverVehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "DriverVehicleLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CompanyBranches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CompanyAdmins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "VehicleDocuments");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "VehicleDocuments");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "NextOfKins");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "DriverVehicles");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "DriverVehicleLocations");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CompanyBranches");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CompanyAdmins");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ActivityLogs");
        }
    }
}
