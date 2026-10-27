using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPokemon.Migrations
{
    /// <inheritdoc />
    public partial class CompartirEncuentrosPorZona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Encuentro_AspNetUsers_CapturadoPorUsuarioId",
                table: "Encuentro");

            migrationBuilder.DropIndex(
                name: "IX_Encuentro_Capturado_FechaExpiracion",
                table: "Encuentro");

            migrationBuilder.DropIndex(
                name: "IX_Encuentro_CapturadoPorUsuarioId",
                table: "Encuentro");

            migrationBuilder.AddColumn<string>(
                name: "ZonaKey",
                table: "Encuentro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EncuentroUsuario",
                columns: table => new
                {
                    EncuentroId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    JugadorDetectado = table.Column<bool>(type: "bit", nullable: false),
                    TiroPorLaEspaldaDisponible = table.Column<bool>(type: "bit", nullable: false),
                    CalidadTiroDisponible = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ComiendoHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Capturado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCaptura = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncuentroUsuario", x => new { x.EncuentroId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_EncuentroUsuario_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EncuentroUsuario_Encuentro_EncuentroId",
                        column: x => x.EncuentroId,
                        principalTable: "Encuentro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve old private encounters and their user state. Legacy rows are
            // hidden from the shared map and removed after they expire.
            migrationBuilder.Sql("""
                UPDATE [Encuentro]
                SET [ZonaKey] = CONCAT(N'legacy-', [Id])
                WHERE [ZonaKey] IS NULL;

                INSERT INTO [EncuentroUsuario]
                    ([EncuentroId], [UsuarioId], [JugadorDetectado], [TiroPorLaEspaldaDisponible],
                     [CalidadTiroDisponible], [ComiendoHasta], [Capturado], [FechaCaptura])
                SELECT [Id], [UsuarioId], [JugadorDetectado], [TiroPorLaEspaldaDisponible],
                       [CalidadTiroDisponible], [ComiendoHasta], [Capturado], NULL
                FROM [Encuentro]
                WHERE [UsuarioId] IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ZonaKey",
                table: "Encuentro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropColumn(name: "CalidadTiroDisponible", table: "Encuentro");
            migrationBuilder.DropColumn(name: "Capturado", table: "Encuentro");
            migrationBuilder.DropColumn(name: "CapturadoPorUsuarioId", table: "Encuentro");
            migrationBuilder.DropColumn(name: "ComiendoHasta", table: "Encuentro");
            migrationBuilder.DropColumn(name: "JugadorDetectado", table: "Encuentro");
            migrationBuilder.DropColumn(name: "TiroPorLaEspaldaDisponible", table: "Encuentro");

            migrationBuilder.CreateIndex(
                name: "IX_Encuentro_FechaExpiracion",
                table: "Encuentro",
                column: "FechaExpiracion");

            migrationBuilder.CreateIndex(
                name: "IX_Encuentro_Latitud_Longitud",
                table: "Encuentro",
                columns: new[] { "Latitud", "Longitud" });

            migrationBuilder.CreateIndex(
                name: "IX_Encuentro_ZonaKey",
                table: "Encuentro",
                column: "ZonaKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EncuentroUsuario_UsuarioId_Capturado",
                table: "EncuentroUsuario",
                columns: new[] { "UsuarioId", "Capturado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncuentroUsuario");

            migrationBuilder.DropIndex(
                name: "IX_Encuentro_FechaExpiracion",
                table: "Encuentro");

            migrationBuilder.DropIndex(
                name: "IX_Encuentro_Latitud_Longitud",
                table: "Encuentro");

            migrationBuilder.DropIndex(
                name: "IX_Encuentro_ZonaKey",
                table: "Encuentro");

            migrationBuilder.DropColumn(
                name: "ZonaKey",
                table: "Encuentro");

            migrationBuilder.AddColumn<string>(
                name: "CalidadTiroDisponible",
                table: "Encuentro",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Capturado",
                table: "Encuentro",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CapturadoPorUsuarioId",
                table: "Encuentro",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ComiendoHasta",
                table: "Encuentro",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JugadorDetectado",
                table: "Encuentro",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TiroPorLaEspaldaDisponible",
                table: "Encuentro",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Encuentro_Capturado_FechaExpiracion",
                table: "Encuentro",
                columns: new[] { "Capturado", "FechaExpiracion" });

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
    }
}
