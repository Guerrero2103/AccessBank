using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankApp_Models.Migrations
{
    /// <inheritdoc />
    public partial class AddWachtwoordResetCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WachtwoordResetCode",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WachtwoordResetVervaltijd",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WachtwoordResetCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WachtwoordResetVervaltijd",
                table: "AspNetUsers");
        }
    }
}
