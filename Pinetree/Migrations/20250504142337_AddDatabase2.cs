using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinetree.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserStorageUsages");

            migrationBuilder.DropColumn(
                name: "BlobUrl",
                table: "UserBlobInfos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserBlobInfos");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UserStorageUsages",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PineconeGuid",
                table: "UserBlobInfos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UserBlobInfos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UserStorageUsages");

            migrationBuilder.DropColumn(
                name: "PineconeGuid",
                table: "UserBlobInfos");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UserBlobInfos");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserStorageUsages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlobUrl",
                table: "UserBlobInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserBlobInfos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }
    }
}
