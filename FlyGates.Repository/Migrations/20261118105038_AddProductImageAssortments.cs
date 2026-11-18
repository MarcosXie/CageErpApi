using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageAssortments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductImageAssortments",
                columns: table => new
                {
                    Store = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Generation = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImageAssortments", x => x.Store);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductImageAssortmentSkus",
                columns: table => new
                {
                    Store = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "ascii_general_ci")
                        .Annotation("MySql:CharSet", "ascii")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImageAssortmentSkus", x => new { x.Store, x.Sku });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductImageCatalog",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "ascii_general_ci")
                        .Annotation("MySql:CharSet", "ascii"),
                    FileName = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false, collation: "ascii_general_ci")
                        .Annotation("MySql:CharSet", "ascii"),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    ModifiedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImageCatalog", x => new { x.Sku, x.FileName });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductImageRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImageRevisions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ProductImageRevisions",
                columns: new[] { "Id", "Value" },
                values: new object[] { 1, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImageCatalog_Revision",
                table: "ProductImageCatalog",
                column: "Revision",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductImageAssortments");

            migrationBuilder.DropTable(
                name: "ProductImageAssortmentSkus");

            migrationBuilder.DropTable(
                name: "ProductImageCatalog");

            migrationBuilder.DropTable(
                name: "ProductImageRevisions");
        }
    }
}
