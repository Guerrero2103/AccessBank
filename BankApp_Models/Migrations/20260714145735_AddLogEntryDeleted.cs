using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankApp_Models.Migrations
{
    /// <inheritdoc />
    public partial class AddLogEntryDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted",
                table: "LogEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "LogEntries",
                keyColumn: "Id",
                keyValue: 1,
                column: "Deleted",
                value: new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "LogEntries");
        }
    }
}
