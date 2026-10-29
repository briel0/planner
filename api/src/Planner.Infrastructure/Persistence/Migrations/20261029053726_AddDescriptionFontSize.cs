using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionFontSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "description_font_size",
                table: "cards",
                type: "integer",
                nullable: false,
                defaultValue: 14);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cards_description_font_size",
                table: "cards",
                sql: "description_font_size BETWEEN 10 AND 32");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cards_description_font_size",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "description_font_size",
                table: "cards");
        }
    }
}
