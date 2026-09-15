using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCageClusterEntityAndRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CageClusterId",
                table: "cage_out_id",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "cage_cluster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnitId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cage_cluster_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "fk_cage_cluster_unit",
                        column: x => x.UnitId,
                        principalTable: "cage_out_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_cage_out_id_cage_cluster_id",
                table: "cage_out_id",
                column: "CageClusterId");

            migrationBuilder.CreateIndex(
                name: "idx_cage_cluster_unit_code_unique",
                table: "cage_cluster",
                columns: new[] { "UnitId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_cage_cluster_unit_id",
                table: "cage_cluster",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "fk_cage_out_id_cage_cluster",
                table: "cage_out_id",
                column: "CageClusterId",
                principalTable: "cage_cluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cage_out_id_cage_cluster",
                table: "cage_out_id");

            migrationBuilder.DropTable(
                name: "cage_cluster");

            migrationBuilder.DropIndex(
                name: "idx_cage_out_id_cage_cluster_id",
                table: "cage_out_id");

            migrationBuilder.DropColumn(
                name: "CageClusterId",
                table: "cage_out_id");
        }
    }
}
