using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Business.Migrations
{
    /// <inheritdoc />
    public partial class InitPriorityH : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "MaintenanceTickets");
        }
    }
}
