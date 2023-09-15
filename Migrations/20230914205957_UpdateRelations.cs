using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacialRecognition.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RecognizedFaces_PhotoId",
                table: "RecognizedFaces",
                column: "PhotoId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecognizedFaces_Photos_PhotoId",
                table: "RecognizedFaces",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecognizedFaces_Photos_PhotoId",
                table: "RecognizedFaces");

            migrationBuilder.DropIndex(
                name: "IX_RecognizedFaces_PhotoId",
                table: "RecognizedFaces");
        }
    }
}
