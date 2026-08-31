using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmailFromUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "cage_out_unit");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "cage_out_unit",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "idx_cage_out_unit_client_id",
                table: "cage_out_unit",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_cage_out_unit_client_id",
                table: "cage_out_unit");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "cage_out_unit");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "cage_out_unit",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
