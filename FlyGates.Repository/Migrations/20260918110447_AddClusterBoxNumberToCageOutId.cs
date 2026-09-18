using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlyGates.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddClusterBoxNumberToCageOutId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClusterBoxNumber",
                table: "cage_out_id",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE `cage_out_id` target
JOIN (
    SELECT `Id`,
           ROW_NUMBER() OVER (
               PARTITION BY `CageClusterId`
               ORDER BY `Identifier`
           ) AS `Seq`
    FROM `cage_out_id`
    WHERE `CageClusterId` IS NOT NULL
) ranked ON ranked.`Id` = target.`Id`
SET target.`ClusterBoxNumber` = ranked.`Seq`
WHERE target.`CageClusterId` IS NOT NULL;
");

            migrationBuilder.CreateIndex(
                name: "idx_cage_out_id_cluster_box_number",
                table: "cage_out_id",
                columns: new[] { "CageClusterId", "ClusterBoxNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_cage_out_id_cluster_box_number",
                table: "cage_out_id");

            migrationBuilder.DropColumn(
                name: "ClusterBoxNumber",
                table: "cage_out_id");
        }
    }
}
