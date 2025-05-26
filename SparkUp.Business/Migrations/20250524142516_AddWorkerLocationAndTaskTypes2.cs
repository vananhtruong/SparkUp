using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SparkUp.Business.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerLocationAndTaskTypes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "WorkerTaskTypes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "WorkerTaskTypes");
        }
    }
}
