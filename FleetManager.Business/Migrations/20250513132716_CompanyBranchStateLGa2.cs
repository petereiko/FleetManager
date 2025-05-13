using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class CompanyBranchStateLGa2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LGAs_States_StateId",
                table: "LGAs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "States");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "States");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "States");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "States");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "States");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LGAs");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LGAs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LGAs");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "LGAs");

            migrationBuilder.AlterColumn<long>(
                name: "StateId",
                table: "LGAs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "LGAs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_LGAs_States_StateId",
                table: "LGAs",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LGAs_States_StateId",
                table: "LGAs");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "States",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "States",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "States",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "States",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "States",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "StateId",
                table: "LGAs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "LGAs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LGAs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LGAs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LGAs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "LGAs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LGAs_States_StateId",
                table: "LGAs",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
