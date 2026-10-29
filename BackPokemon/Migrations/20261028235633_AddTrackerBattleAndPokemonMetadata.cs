using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    public partial class AddTrackerBattleAndPokemonMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(name: "FechaObtencion", table: "PokemonUser", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Origen", table: "PokemonUser", type: "int", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<string>(name: "IdUsuario", table: "Batallas", type: "nvarchar(450)", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Resultado", table: "Batallas", type: "int", nullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "LlaveIdempotencia", table: "Batallas", type: "uniqueidentifier",
                nullable: true, oldClrType: typeof(Guid), oldType: "uniqueidentifier");
            migrationBuilder.DropIndex(name: "IX_Batallas_PokemonUserGanador", table: "Batallas");
            migrationBuilder.AlterColumn<int>(name: "PokemonUserGanador", table: "Batallas", type: "int",
                nullable: true, oldClrType: typeof(int), oldType: "int");
            migrationBuilder.DropForeignKey(name: "FK_Batallas_PokemonUser_PokemonUserGanador", table: "Batallas");

            migrationBuilder.Sql(@"
UPDATE b
SET b.IdUsuario = pu.IdUsuario,
    b.Resultado = CASE WHEN b.PokemonUserGanador = b.PokemonUserR THEN 1 ELSE 2 END
FROM Batallas b
INNER JOIN PokemonUser pu ON pu.Id = b.PokemonUserR;

IF EXISTS (SELECT 1 FROM Batallas WHERE IdUsuario IS NULL OR Resultado IS NULL)
    THROW 51000, 'No se pudo asignar usuario o resultado a todas las batallas existentes.', 1;

;WITH EspeciesActuales AS
(
    SELECT pu.IdUsuario AS UserId, pu.IdPokemon AS PokemonId
    FROM PokemonUser pu
    GROUP BY pu.IdUsuario, pu.IdPokemon
)
INSERT INTO Pokedex (UserId, PokemonId, Fecha)
SELECT especies.UserId, especies.PokemonId, SYSUTCDATETIME()
FROM EspeciesActuales especies
WHERE NOT EXISTS
(
    SELECT 1 FROM Pokedex p
    WHERE p.UserId = especies.UserId AND p.PokemonId = especies.PokemonId
);");

            migrationBuilder.AlterColumn<string>(name: "IdUsuario", table: "Batallas", type: "nvarchar(450)", nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(450)", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "Resultado", table: "Batallas", type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.CreateIndex(name: "IX_Batallas_IdUsuario_Fecha", table: "Batallas", columns: new[] { "IdUsuario", "Fecha" });
            migrationBuilder.AddForeignKey(name: "FK_Batallas_AspNetUsers_IdUsuario", table: "Batallas", column: "IdUsuario",
                principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Batallas_AspNetUsers_IdUsuario", table: "Batallas");
            migrationBuilder.DropIndex(name: "IX_Batallas_IdUsuario_Fecha", table: "Batallas");
            migrationBuilder.DropColumn(name: "IdUsuario", table: "Batallas");
            migrationBuilder.DropColumn(name: "Resultado", table: "Batallas");
            migrationBuilder.DropColumn(name: "FechaObtencion", table: "PokemonUser");
            migrationBuilder.DropColumn(name: "Origen", table: "PokemonUser");

            migrationBuilder.Sql("UPDATE Batallas SET PokemonUserGanador = PokemonUserR WHERE PokemonUserGanador IS NULL; UPDATE Batallas SET LlaveIdempotencia = NEWID() WHERE LlaveIdempotencia IS NULL;");
            migrationBuilder.DropIndex(name: "IX_Batallas_PokemonUserGanador", table: "Batallas");
            migrationBuilder.AlterColumn<int>(name: "PokemonUserGanador", table: "Batallas", type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "LlaveIdempotencia", table: "Batallas", type: "uniqueidentifier", nullable: false,
                defaultValue: Guid.Empty, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);
            migrationBuilder.CreateIndex(name: "IX_Batallas_PokemonUserGanador", table: "Batallas", column: "PokemonUserGanador");
            migrationBuilder.AddForeignKey(name: "FK_Batallas_PokemonUser_PokemonUserGanador", table: "Batallas",
                column: "PokemonUserGanador", principalTable: "PokemonUser", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}