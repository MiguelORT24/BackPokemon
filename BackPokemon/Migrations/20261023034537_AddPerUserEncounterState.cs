using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class AddPerUserEncounterState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "Encuentro",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Encuentro_UsuarioId",
                table: "Encuentro",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Encuentro_AspNetUsers_UsuarioId",
                table: "Encuentro",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Encuentro_AspNetUsers_UsuarioId",
                table: "Encuentro");

            migrationBuilder.DropIndex(
                name: "IX_Encuentro_UsuarioId",
                table: "Encuentro");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Encuentro");
        }
    }
}
