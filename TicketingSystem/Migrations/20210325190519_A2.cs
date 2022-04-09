using Microsoft.EntityFrameworkCore.Migrations;

namespace TicketingSystem.Migrations
{
    public partial class A2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Service",
                table: "Tickets");

            migrationBuilder.AddColumn<int>(
                name: "LocationID",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceID",
                table: "Tickets",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationID",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ServiceID",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "Service",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
