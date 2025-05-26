using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SparkUp.Business.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerLocationAndTaskTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "WorkerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "WorkerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "WorkerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "WorkerTaskTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerProfileId = table.Column<int>(type: "int", nullable: false),
                    TaskTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerTaskTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerTaskTypes_TaskTypes_TaskTypeId",
                        column: x => x.TaskTypeId,
                        principalTable: "TaskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkerTaskTypes_WorkerProfiles_WorkerProfileId",
                        column: x => x.WorkerProfileId,
                        principalTable: "WorkerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerTaskTypes_TaskTypeId",
                table: "WorkerTaskTypes",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerTaskTypes_WorkerProfileId",
                table: "WorkerTaskTypes",
                column: "WorkerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerTaskTypes");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "District",
                table: "WorkerProfiles");
        }
    }
}
