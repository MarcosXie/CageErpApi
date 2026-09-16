using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalStatusToCageOutId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentMode",
                table: "cage_out_id",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "OperationalStatus",
                table: "cage_out_id",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusUpdatedAt",
                table: "cage_out_id",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentMode",
                table: "cage_out_id");

            migrationBuilder.DropColumn(
                name: "OperationalStatus",
                table: "cage_out_id");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedAt",
                table: "cage_out_id");
        }
    }
}
