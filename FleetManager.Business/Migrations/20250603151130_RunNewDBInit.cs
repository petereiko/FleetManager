using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class RunNewDBInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "Vehicles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyBranchId",
                table: "EmailLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "EmailLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LicenseCategory",
                table: "Drivers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "Drivers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Drivers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "CompanyBranchId",
                table: "ActivityLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "ActivityLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<long>(type: "bigint", nullable: true),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverDocuments_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DriverDutyOfCares",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<long>(type: "bigint", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VehicleId = table.Column<long>(type: "bigint", nullable: true),
                    VehiclePreCheckCompleted = table.Column<bool>(type: "bit", nullable: false),
                    VehicleConditionNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFitToDrive = table.Column<bool>(type: "bit", nullable: false),
                    HealthDeclarationNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasValidLicense = table.Column<bool>(type: "bit", nullable: false),
                    IsAwareOfCompanyPolicies = table.Column<bool>(type: "bit", nullable: false),
                    HasReviewedDrivingHours = table.Column<bool>(type: "bit", nullable: false),
                    LastRestPeriod = table.Column<TimeSpan>(type: "time", nullable: false),
                    ReportsFatigue = table.Column<bool>(type: "bit", nullable: false),
                    ReportsVehicleIssues = table.Column<bool>(type: "bit", nullable: false),
                    ReportedIssuesDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmsAccuracyOfInfo = table.Column<bool>(type: "bit", nullable: false),
                    DeclarationTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DutyOfCareRecordType = table.Column<int>(type: "int", nullable: false),
                    DutyOfCareStatus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDutyOfCares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverDutyOfCares_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DriverDutyOfCares_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CompanyId",
                table: "Vehicles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_CompanyBranchId",
                table: "EmailLogs",
                column: "CompanyBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_CompanyId",
                table: "EmailLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CompanyId",
                table: "Drivers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CompanyBranchId",
                table: "ActivityLogs",
                column: "CompanyBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CompanyId",
                table: "ActivityLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverDocuments_DriverId",
                table: "DriverDocuments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverDutyOfCares_DriverId",
                table: "DriverDutyOfCares",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverDutyOfCares_VehicleId",
                table: "DriverDutyOfCares",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Companies_CompanyId",
                table: "ActivityLogs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_CompanyBranches_CompanyBranchId",
                table: "ActivityLogs",
                column: "CompanyBranchId",
                principalTable: "CompanyBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_Companies_CompanyId",
                table: "EmailLogs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_CompanyBranches_CompanyBranchId",
                table: "EmailLogs",
                column: "CompanyBranchId",
                principalTable: "CompanyBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Companies_CompanyId",
                table: "Vehicles",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Companies_CompanyId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_CompanyBranches_CompanyBranchId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Companies_CompanyId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_Companies_CompanyId",
                table: "EmailLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_CompanyBranches_CompanyBranchId",
                table: "EmailLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Companies_CompanyId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "DriverDocuments");

            migrationBuilder.DropTable(
                name: "DriverDutyOfCares");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CompanyId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_CompanyBranchId",
                table: "EmailLogs");

            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_CompanyId",
                table: "EmailLogs");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_CompanyId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_CompanyBranchId",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_CompanyId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CompanyBranchId",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CompanyBranchId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ActivityLogs");

            migrationBuilder.AlterColumn<string>(
                name: "LicenseCategory",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
