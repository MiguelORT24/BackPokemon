using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarCatalogoPokeballs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PokeApiItemId",
                table: "Item",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Item_PokeApiItemId",
                table: "Item",
                column: "PokeApiItemId",
                unique: true,
                filter: "[PokeApiItemId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Item_PokeApiItemId",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "PokeApiItemId",
                table: "Item");
        }
    }
}
