using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class InitVendorTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendors_VendorServicesOffered_VendorServiceOfferedId",
                table: "Vendors");

            migrationBuilder.DropTable(
                name: "VendorServicesOffered");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_VendorServiceOfferedId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "VendorServiceOfferedId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "VendorServicesOfferedId",
                table: "Vendors");

            migrationBuilder.AddColumn<string>(
                name: "VendorServiceOffered",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VendorServiceOffered",
                table: "Vendors");

            migrationBuilder.AddColumn<int>(
                name: "VendorServiceOfferedId",
                table: "Vendors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorServicesOfferedId",
                table: "Vendors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VendorServicesOffered",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorServicesOffered", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorServiceOfferedId",
                table: "Vendors",
                column: "VendorServiceOfferedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendors_VendorServicesOffered_VendorServiceOfferedId",
                table: "Vendors",
                column: "VendorServiceOfferedId",
                principalTable: "VendorServicesOffered",
                principalColumn: "Id");
        }
    }
}
