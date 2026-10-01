# Endpoints del juego

Todas estas rutas requieren `Authorization: Bearer <token>` del login existente.
Se aceptan tanto las rutas de esta tabla como sus equivalentes con prefijo `/api`.

| Método | Ruta | Uso |
|---|---|---|
| GET | `/me/pokemon` | Pokémon del usuario del JWT, vida, estado, crianza, `available` y `reserved`. |
| GET | `/me/pokemon?health_lt=100&available=true` | Combina ambos filtros. |
| GET | `/me/pokemon?available=true` | Pokémon que se pueden ofrecer. |
| POST | `/me/pokemon/heal` | Sin body o `{}`; cura todos los propios que no tengan crianza muerta. Devuelve `{ "healed": N }`. |
| GET | `/users?search=mario` | Busca por nombre, excluye al usuario actual; devuelve `id` y `userName`. |
| POST | `/trades` | Crea invitación y reserva el Pokémon del creador. |
| GET | `/me/trades?status=pending` | Enviados y recibidos; filtro opcional. |
| GET | `/trades/{id}` | Detalle privado para sus participantes. |
| POST | `/trades/{id}/accept` | El destinatario ofrece y reserva su Pokémon. |
| POST | `/trades/{id}/confirm` | El creador ejecuta ambos cambios de propietario en una transacción. |
| POST | `/trades/{id}/cancel` | Cancela o rechaza; conserva historial y libera reservas. |

## Cuerpos JSON

Crear intercambio:

```json
{ "recipientUserId": "id-del-usuario-destinatario", "pokemonUserId": 12 }
```

Aceptar intercambio:

```json
{ "pokemonUserId": 25 }
```

Confirmar y cancelar no requieren body. `pokemonUserId` identifica la instancia
en `PokemonUser`, no el número de especie de la Pokédex.

El detalle devuelve `id`, `role` (`sent`/`received`), `status`, `creator`,
`recipient`, `offeredPokemon`, `recipientPokemon` (null hasta aceptar),
`createdAt`, `acceptedAt` y `confirmedAt`. Los participantes solo incluyen
`id` y `userName`; los Pokémon incluyen `id`, `idPokemon` y `nombre`.

## Reglas

- `health_lt` compara puntos de vida actuales, no porcentaje; admite 0–100 y usa `<` estricto.
- `available=false` también funciona. Disponible significa vida mayor que cero,
  crianza ausente o viva y ninguna reserva `pending`/`accepted`. La crianza
  refleja el último estado persistido por Tamagotchi.
- Curar restaura `VidaTotal`, estado `Sano` y contadores de estado a cero.
  No crea estados faltantes ni revive crianzas muertas. `healed` cuenta filas actualizadas.
- `search` requiere 2–100 caracteres después de recortar espacios; devuelve hasta 50 resultados.
- Flujo: `pending -> accepted -> confirmed`.
- El creador puede cancelar `pending`/`accepted`; el destinatario rechaza
  `pending` (`rejected`) o cancela `accepted` (`cancelled`).
- Confirmar dos veces o modificar un intercambio terminado responde 409.
- 400: entrada inválida; 401: falta JWT; 403: participante sin permiso para esa
  acción; 404: recurso inexistente o intercambio ajeno; 409: estado o reserva en conflicto.
- Las reservas se derivan del historial activo. Las mutaciones usan aislamiento
  serializable en SQL Server; ante conflicto concurrente se devuelve 409.
- El antiguo CRUD `/api/Intercambios` fue reemplazado por este flujo.
  Los CRUD de Pokémon y estado ahora exigen JWT y propiedad. No permiten
  transferir propietarios ni cambiar la especie por PUT.

## Cuando conectes la base de datos

Configura `ConnectionStrings:DefaultConnection` y aplica:

```powershell
dotnet ef database update --project BackPokemon
```

No se ha actualizado la base configurada en appsettings. La nueva migración es
`PrepararIntercambios`. Si hay intercambios del esquema antiguo, se detiene antes
de modificar datos: `UserIdR`/`UserIdD` eran booleanos y no permiten reconstruir
los participantes. En ese caso se necesita un mapeo histórico real o archivar
esos registros antes de adaptar/aplicar la migración. No se inventan usuarios.

## Compilación

```powershell
dotnet build BackPokemon/BackPokemon.csproj
```
