using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddLastSeenAtToCageOutId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "cage_out_id",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "cage_out_id");
        }
    }
}
