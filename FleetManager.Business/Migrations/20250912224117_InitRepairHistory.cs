using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class InitRepairHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repairs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchId = table.Column<long>(type: "bigint", nullable: true),
                    VehicleId = table.Column<long>(type: "bigint", nullable: false),
                    DriverId = table.Column<long>(type: "bigint", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Repairs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Repairs_CompanyBranches_CompanyBranchId",
                        column: x => x.CompanyBranchId,
                        principalTable: "CompanyBranches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Repairs_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Repairs_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairInvoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyBranchId = table.Column<long>(type: "bigint", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairInvoices_CompanyBranches_CompanyBranchId",
                        column: x => x.CompanyBranchId,
                        principalTable: "CompanyBranches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepairInvoices_Repairs_RepairId",
                        column: x => x.RepairId,
                        principalTable: "Repairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairId = table.Column<long>(type: "bigint", nullable: false),
                    VehiclePartCategoryId = table.Column<int>(type: "int", nullable: true),
                    VehiclePartId = table.Column<int>(type: "int", nullable: true),
                    CustomPartDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairItems_Repairs_RepairId",
                        column: x => x.RepairId,
                        principalTable: "Repairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepairItems_VehiclePartCategories_VehiclePartCategoryId",
                        column: x => x.VehiclePartCategoryId,
                        principalTable: "VehiclePartCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepairItems_VehicleParts_VehiclePartId",
                        column: x => x.VehiclePartId,
                        principalTable: "VehicleParts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RepairInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    VehiclePartCategoryId = table.Column<int>(type: "int", nullable: true),
                    VehiclePartId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairInvoiceItems_RepairInvoices_RepairInvoiceId",
                        column: x => x.RepairInvoiceId,
                        principalTable: "RepairInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepairInvoiceItems_VehiclePartCategories_VehiclePartCategoryId",
                        column: x => x.VehiclePartCategoryId,
                        principalTable: "VehiclePartCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RepairInvoiceItems_VehicleParts_VehiclePartId",
                        column: x => x.VehiclePartId,
                        principalTable: "VehicleParts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepairInvoiceItems_RepairInvoiceId",
                table: "RepairInvoiceItems",
                column: "RepairInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairInvoiceItems_VehiclePartCategoryId",
                table: "RepairInvoiceItems",
                column: "VehiclePartCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairInvoiceItems_VehiclePartId",
                table: "RepairInvoiceItems",
                column: "VehiclePartId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairInvoices_CompanyBranchId",
                table: "RepairInvoices",
                column: "CompanyBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairInvoices_RepairId",
                table: "RepairInvoices",
                column: "RepairId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_RepairId",
                table: "RepairItems",
                column: "RepairId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_VehiclePartCategoryId",
                table: "RepairItems",
                column: "VehiclePartCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_VehiclePartId",
                table: "RepairItems",
                column: "VehiclePartId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_CompanyBranchId",
                table: "Repairs",
                column: "CompanyBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_CompanyId",
                table: "Repairs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_DriverId",
                table: "Repairs",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_VehicleId",
                table: "Repairs",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepairInvoiceItems");

            migrationBuilder.DropTable(
                name: "RepairItems");

            migrationBuilder.DropTable(
                name: "RepairInvoices");

            migrationBuilder.DropTable(
                name: "Repairs");
        }
    }
}
