using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewDeviceInfoColumnInAttendancelogstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "AttendanceLogs",
                newName: "Version");

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "AttendanceLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "AttendanceLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OS",
                table: "AttendanceLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Browser",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "AttendanceLogs");

            migrationBuilder.DropColumn(
                name: "OS",
                table: "AttendanceLogs");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "AttendanceLogs",
                newName: "DeviceId");
        }
    }
}
