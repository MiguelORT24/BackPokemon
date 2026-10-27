using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptureGameplayAndBattleRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalidadTiro",
                table: "Captura",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CaptureRate",
                table: "Captura",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EncuentroId",
                table: "Captura",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FueCritica",
                table: "Captura",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "MultiplicadorBola",
                table: "Captura",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "MultiplicadorSituacional",
                table: "Captura",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "ProbabilidadCritica",
                table: "Captura",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<Guid?>(
                name: "LlaveIdempotencia",
                table: "Batallas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EfectoItem",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MultiplicadorCaptura = table.Column<float>(type: "real", nullable: true),
                    DuracionSegundos = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EfectoItem", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_EfectoItem_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Encuentro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PokemonId = table.Column<int>(type: "int", nullable: false),
                    PokemonNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitud = table.Column<double>(type: "float", nullable: false),
                    Longitud = table.Column<double>(type: "float", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Dormido = table.Column<bool>(type: "bit", nullable: false),
                    JugadorDetectado = table.Column<bool>(type: "bit", nullable: false),
                    ComiendoHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Capturado = table.Column<bool>(type: "bit", nullable: false),
                    CapturadoPorUsuarioId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encuentro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjetoCampo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Latitud = table.Column<double>(type: "float", nullable: false),
                    Longitud = table.Column<double>(type: "float", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecogidoPorUsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaRecogida = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjetoCampo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjetoCampo_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecompensaBatalla",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatallaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecompensaBatalla", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecompensaBatalla_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecompensaBatalla_Batallas_BatallaId",
                        column: x => x.BatallaId,
                        principalTable: "Batallas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecompensaBatalla_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Captura_EncuentroId",
                table: "Captura",
                column: "EncuentroId");

            migrationBuilder.CreateIndex(
                name: "IX_Batallas_LlaveIdempotencia",
                table: "Batallas",
                column: "LlaveIdempotencia",
                unique: true,
                filter: "[LlaveIdempotencia] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Encuentro_Capturado_FechaExpiracion",
                table: "Encuentro",
                columns: new[] { "Capturado", "FechaExpiracion" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjetoCampo_ItemId",
                table: "ObjetoCampo",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjetoCampo_RecogidoPorUsuarioId_FechaExpiracion",
                table: "ObjetoCampo",
                columns: new[] { "RecogidoPorUsuarioId", "FechaExpiracion" });

            migrationBuilder.CreateIndex(
                name: "IX_RecompensaBatalla_BatallaId",
                table: "RecompensaBatalla",
                column: "BatallaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecompensaBatalla_ItemId",
                table: "RecompensaBatalla",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecompensaBatalla_UsuarioId",
                table: "RecompensaBatalla",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Captura_Encuentro_EncuentroId",
                table: "Captura",
                column: "EncuentroId",
                principalTable: "Encuentro",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Captura_Encuentro_EncuentroId",
                table: "Captura");

            migrationBuilder.DropTable(
                name: "EfectoItem");

            migrationBuilder.DropTable(
                name: "Encuentro");

            migrationBuilder.DropTable(
                name: "ObjetoCampo");

            migrationBuilder.DropTable(
                name: "RecompensaBatalla");

            migrationBuilder.DropIndex(
                name: "IX_Captura_EncuentroId",
                table: "Captura");

            migrationBuilder.DropIndex(
                name: "IX_Batallas_LlaveIdempotencia",
                table: "Batallas");

            migrationBuilder.DropColumn(
                name: "CalidadTiro",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "CaptureRate",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "EncuentroId",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "FueCritica",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "MultiplicadorBola",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "MultiplicadorSituacional",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "ProbabilidadCritica",
                table: "Captura");

            migrationBuilder.DropColumn(
                name: "LlaveIdempotencia",
                table: "Batallas");
        }
    }
}
