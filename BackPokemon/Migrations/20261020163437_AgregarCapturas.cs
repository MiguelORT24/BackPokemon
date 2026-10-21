using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCapturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pokedex_UserId",
                table: "Pokedex");

            migrationBuilder.CreateTable(
                name: "Captura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PokemonId = table.Column<int>(type: "int", nullable: false),
                    PokemonNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdPokeball = table.Column<int>(type: "int", nullable: false),
                    Exitosa = table.Column<bool>(type: "bit", nullable: false),
                    ProbabilidadAplicada = table.Column<float>(type: "real", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdPokemonUser = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Captura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Captura_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Captura_Item_IdPokeball",
                        column: x => x.IdPokeball,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Captura_PokemonUser_IdPokemonUser",
                        column: x => x.IdPokemonUser,
                        principalTable: "PokemonUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pokedex_UserId_PokemonId",
                table: "Pokedex",
                columns: new[] { "UserId", "PokemonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Captura_IdPokeball",
                table: "Captura",
                column: "IdPokeball");

            migrationBuilder.CreateIndex(
                name: "IX_Captura_IdPokemonUser",
                table: "Captura",
                column: "IdPokemonUser");

            migrationBuilder.CreateIndex(
                name: "IX_Captura_UsuarioId_Fecha",
                table: "Captura",
                columns: new[] { "UsuarioId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Captura");

            migrationBuilder.DropIndex(
                name: "IX_Pokedex_UserId_PokemonId",
                table: "Pokedex");

            migrationBuilder.CreateIndex(
                name: "IX_Pokedex_UserId",
                table: "Pokedex",
                column: "UserId");
        }
    }
}
