using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinetree.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserBlobInfos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserBlobInfos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserBlobInfos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserBlobInfos");
        }
    }
}
