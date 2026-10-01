using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class PrepararIntercambios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los booleanos antiguos no permiten reconstruir los participantes históricos.
            // Detenerse antes de modificar datos en lugar de inventar identidades.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Intercambio])
                    THROW 51000, 'PrepararIntercambios requiere mapear o archivar los intercambios antiguos antes de migrar; UserIdR/UserIdD eran booleanos.', 1;
                """);
            migrationBuilder.DropColumn(
                name: "UserIdD",
                table: "Intercambio");

            migrationBuilder.DropColumn(
                name: "UserIdR",
                table: "Intercambio");

            migrationBuilder.AlterColumn<int>(
                name: "PokemonUserD",
                table: "Intercambio",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptacion",
                table: "Intercambio",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaConfirmacion",
                table: "Intercambio",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCreadorId",
                table: "Intercambio",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioDestinatarioId",
                table: "Intercambio",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Intercambio_Estado_PokemonUserD",
                table: "Intercambio",
                columns: new[] { "Estado", "PokemonUserD" });

            migrationBuilder.CreateIndex(
                name: "IX_Intercambio_Estado_PokemonUserR",
                table: "Intercambio",
                columns: new[] { "Estado", "PokemonUserR" });

            migrationBuilder.CreateIndex(
                name: "IX_Intercambio_UsuarioCreadorId",
                table: "Intercambio",
                column: "UsuarioCreadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Intercambio_UsuarioDestinatarioId",
                table: "Intercambio",
                column: "UsuarioDestinatarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Intercambio_AspNetUsers_UsuarioCreadorId",
                table: "Intercambio",
                column: "UsuarioCreadorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Intercambio_AspNetUsers_UsuarioDestinatarioId",
                table: "Intercambio",
                column: "UsuarioDestinatarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Intercambio])
                    THROW 51000, 'No se puede revertir PrepararIntercambios con historial de intercambios nuevo.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_Intercambio_AspNetUsers_UsuarioCreadorId",
                table: "Intercambio");

            migrationBuilder.DropForeignKey(
                name: "FK_Intercambio_AspNetUsers_UsuarioDestinatarioId",
                table: "Intercambio");

            migrationBuilder.DropIndex(
                name: "IX_Intercambio_Estado_PokemonUserD",
                table: "Intercambio");

            migrationBuilder.DropIndex(
                name: "IX_Intercambio_Estado_PokemonUserR",
                table: "Intercambio");

            migrationBuilder.DropIndex(
                name: "IX_Intercambio_UsuarioCreadorId",
                table: "Intercambio");

            migrationBuilder.DropIndex(
                name: "IX_Intercambio_UsuarioDestinatarioId",
                table: "Intercambio");

            migrationBuilder.DropColumn(
                name: "FechaAceptacion",
                table: "Intercambio");

            migrationBuilder.DropColumn(
                name: "FechaConfirmacion",
                table: "Intercambio");

            migrationBuilder.DropColumn(
                name: "UsuarioCreadorId",
                table: "Intercambio");

            migrationBuilder.DropColumn(
                name: "UsuarioDestinatarioId",
                table: "Intercambio");

            migrationBuilder.AlterColumn<int>(
                name: "PokemonUserD",
                table: "Intercambio",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UserIdD",
                table: "Intercambio",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UserIdR",
                table: "Intercambio",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
