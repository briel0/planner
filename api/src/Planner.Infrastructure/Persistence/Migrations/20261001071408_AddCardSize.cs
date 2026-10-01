using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "height",
                table: "cards",
                type: "double precision",
                nullable: false,
                defaultValue: 64.0);

            migrationBuilder.AddColumn<double>(
                name: "width",
                table: "cards",
                type: "double precision",
                nullable: false,
                defaultValue: 208.0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cards_size",
                table: "cards",
                sql: "width BETWEEN 120 AND 1200 AND height BETWEEN 48 AND 900");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cards_size",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "height",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "width",
                table: "cards");
        }
    }
}
