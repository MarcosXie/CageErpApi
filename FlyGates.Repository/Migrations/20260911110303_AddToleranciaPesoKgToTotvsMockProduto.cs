using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddToleranciaPesoKgToTotvsMockProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ToleranciaPesoKg",
                table: "totvs_mock_produto",
                type: "decimal(18,3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToleranciaPesoKg",
                table: "totvs_mock_produto");
        }
    }
}
