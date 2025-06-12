using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinetree.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedSpecialAccessFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecialAccessGrantedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SpecialAccessReason",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SpecialAccessGrantedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialAccessReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
