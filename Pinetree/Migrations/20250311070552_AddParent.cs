using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinetree.Migrations
{
    /// <inheritdoc />
    public partial class AddParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pinecone_Pinecone_PineconeId",
                table: "Pinecone");

            migrationBuilder.DropIndex(
                name: "IX_Pinecone_PineconeId",
                table: "Pinecone");

            migrationBuilder.DropColumn(
                name: "PineconeId",
                table: "Pinecone");

            migrationBuilder.CreateIndex(
                name: "IX_Pinecone_ParentId",
                table: "Pinecone",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pinecone_Pinecone_ParentId",
                table: "Pinecone",
                column: "ParentId",
                principalTable: "Pinecone",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pinecone_Pinecone_ParentId",
                table: "Pinecone");

            migrationBuilder.DropIndex(
                name: "IX_Pinecone_ParentId",
                table: "Pinecone");

            migrationBuilder.AddColumn<long>(
                name: "PineconeId",
                table: "Pinecone",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pinecone_PineconeId",
                table: "Pinecone",
                column: "PineconeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pinecone_Pinecone_PineconeId",
                table: "Pinecone",
                column: "PineconeId",
                principalTable: "Pinecone",
                principalColumn: "Id");
        }
    }
}
