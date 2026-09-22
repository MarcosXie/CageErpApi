using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalCageOutIdSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cage_out_id_sequence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NextSequenceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cage_out_id_sequence_pkey", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
INSERT INTO cage_out_id_sequence (Id, NextSequenceNumber)
SELECT 1,
       COALESCE(
           MAX(
               CASE
                   WHEN LOWER(Identifier) REGEXP '^cageid_[0-9]+$'
                       THEN CAST(SUBSTRING(Identifier, 8) AS UNSIGNED)
                   ELSE NULL
               END
           ),
           0
       ) + 1
FROM cage_out_id;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cage_out_id_sequence");
        }
    }
}
