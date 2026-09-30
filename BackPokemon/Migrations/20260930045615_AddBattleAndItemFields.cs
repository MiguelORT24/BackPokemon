using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleAndItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batallas_PokemonUser_PokemonUserC",
                table: "Batallas");

            migrationBuilder.DropIndex(
                name: "IX_Batallas_PokemonUserC",
                table: "Batallas");

            migrationBuilder.RenameColumn(
                name: "PokemonUserC",
                table: "Batallas",
                newName: "IdPokemonRival");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "PokemonUser",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Efecto",
                table: "Item",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Valor",
                table: "Item",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PokemonUserId",
                table: "Batallas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batallas_PokemonUserId",
                table: "Batallas",
                column: "PokemonUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batallas_PokemonUser_PokemonUserId",
                table: "Batallas",
                column: "PokemonUserId",
                principalTable: "PokemonUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batallas_PokemonUser_PokemonUserId",
                table: "Batallas");

            migrationBuilder.DropIndex(
                name: "IX_Batallas_PokemonUserId",
                table: "Batallas");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "PokemonUser");

            migrationBuilder.DropColumn(
                name: "Efecto",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "Valor",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "PokemonUserId",
                table: "Batallas");

            migrationBuilder.RenameColumn(
                name: "IdPokemonRival",
                table: "Batallas",
                newName: "PokemonUserC");

            migrationBuilder.CreateIndex(
                name: "IX_Batallas_PokemonUserC",
                table: "Batallas",
                column: "PokemonUserC");

            migrationBuilder.AddForeignKey(
                name: "FK_Batallas_PokemonUser_PokemonUserC",
                table: "Batallas",
                column: "PokemonUserC",
                principalTable: "PokemonUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
