using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SparkUp.Business.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWorkerandTasktype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bước 1: Thêm cột mới nhưng cho phép NULL
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "WorkerProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskTypeId",
                table: "WorkerProfiles",
                type: "int",
                nullable: true);

            // Bước 2: Cập nhật dữ liệu từ bảng WorkerTaskTypes sang WorkerProfiles
            // Lấy thông tin TaskTypeId đầu tiên cho mỗi WorkerProfile
            migrationBuilder.Sql(@"
                UPDATE wp 
                SET wp.TaskTypeId = wtt.TaskTypeId,
                    wp.HourlyRate = wtt.HourlyRate
                FROM WorkerProfiles wp
                INNER JOIN (
                    SELECT WorkerProfileId, TaskTypeId, HourlyRate,
                           ROW_NUMBER() OVER (PARTITION BY WorkerProfileId ORDER BY Id) as RowNum
                    FROM WorkerTaskTypes
                ) wtt ON wp.Id = wtt.WorkerProfileId AND wtt.RowNum = 1
            ");

            // Bước 3: Đặt giá trị mặc định cho các bản ghi chưa có dữ liệu (nếu có)
            migrationBuilder.Sql(@"
                UPDATE WorkerProfiles
                SET TaskTypeId = (SELECT TOP 1 Id FROM TaskTypes),
                    HourlyRate = 0
                WHERE TaskTypeId IS NULL
            ");

            // Bước 4: Thay đổi cột thành NOT NULL sau khi đã có dữ liệu
            migrationBuilder.AlterColumn<int>(
                name: "TaskTypeId",
                table: "WorkerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRate",
                table: "WorkerProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            // Bước 5: Tạo khóa ngoại và chỉ mục
            migrationBuilder.CreateIndex(
                name: "IX_WorkerProfiles_TaskTypeId",
                table: "WorkerProfiles",
                column: "TaskTypeId");

            // Thay đổi từ CASCADE sang NO ACTION để tránh multiple cascade paths
            migrationBuilder.AddForeignKey(
                name: "FK_WorkerProfiles_TaskTypes_TaskTypeId",
                table: "WorkerProfiles",
                column: "TaskTypeId",
                principalTable: "TaskTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);  // Thay đổi từ Cascade sang NoAction

            // Bước 6: Xóa bảng cũ sau khi dữ liệu đã được di chuyển
            migrationBuilder.DropTable(
                name: "WorkerTaskTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tạo lại bảng WorkerTaskTypes
            migrationBuilder.CreateTable(
                name: "WorkerTaskTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskTypeId = table.Column<int>(type: "int", nullable: false),
                    WorkerProfileId = table.Column<int>(type: "int", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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

            // Di chuyển dữ liệu từ WorkerProfiles sang WorkerTaskTypes
            migrationBuilder.Sql(@"
                INSERT INTO WorkerTaskTypes (TaskTypeId, WorkerProfileId, HourlyRate)
                SELECT TaskTypeId, Id, HourlyRate
                FROM WorkerProfiles
            ");

            // Xóa khóa ngoại và cột
            migrationBuilder.DropForeignKey(
                name: "FK_WorkerProfiles_TaskTypes_TaskTypeId",
                table: "WorkerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_WorkerProfiles_TaskTypeId",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "TaskTypeId",
                table: "WorkerProfiles");

            // Tạo chỉ mục cho bảng WorkerTaskTypes
            migrationBuilder.CreateIndex(
                name: "IX_WorkerTaskTypes_TaskTypeId",
                table: "WorkerTaskTypes",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerTaskTypes_WorkerProfileId",
                table: "WorkerTaskTypes",
                column: "WorkerProfileId");
        }
    }
}