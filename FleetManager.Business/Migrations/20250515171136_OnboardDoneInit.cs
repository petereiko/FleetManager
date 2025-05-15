using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class OnboardDoneInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAdmins_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompanyAdmins_CompanyBranches_CompanyBranchId",
                        column: x => x.CompanyBranchId,
                        principalTable: "CompanyBranches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranches_LgaId",
                table: "CompanyBranches",
                column: "LgaId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranches_StateId",
                table: "CompanyBranches",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAdmins_CompanyBranchId",
                table: "CompanyAdmins",
                column: "CompanyBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAdmins_CompanyId",
                table: "CompanyAdmins",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyBranches_LGAs_LgaId",
                table: "CompanyBranches",
                column: "LgaId",
                principalTable: "LGAs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyBranches_States_StateId",
                table: "CompanyBranches",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyBranches_LGAs_LgaId",
                table: "CompanyBranches");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyBranches_States_StateId",
                table: "CompanyBranches");

            migrationBuilder.DropTable(
                name: "CompanyAdmins");

            migrationBuilder.DropIndex(
                name: "IX_CompanyBranches_LgaId",
                table: "CompanyBranches");

            migrationBuilder.DropIndex(
                name: "IX_CompanyBranches_StateId",
                table: "CompanyBranches");
        }
    }
}
