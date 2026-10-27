using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptureModifierAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CapturadoPorUsuarioId",
                table: "Encuentro",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoTeVio",
                table: "Captura",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PokemonComiendo",
                table: "Captura",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PokemonDormido",
                table: "Captura",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TiroPorLaEspalda",
                table: "Captura",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Encuentro_CapturadoPorUsuarioId",
                table: "Encuentro",
                column: "CapturadoPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Encuentro_AspNetUsers_CapturadoPorUsuarioId",
                table: "Encuentro",
                column: "CapturadoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Encuentro_AspNetUsers_CapturadoPorUsuarioId",
                table: "Encuentro");

            migrationBuilder.DropIndex(
                name: "IX_Encuentro_CapturadoPorUsuarioId",
                table: "Encuentro");

            migrationBuilder.DropColumn(
                name: "NoTeVio",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "PokemonComiendo",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "PokemonDormido",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "TiroPorLaEspalda",
                table: "Captura");

            migrationBuilder.AlterColumn<string>(
                name: "CapturadoPorUsuarioId",
                table: "Encuentro",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
