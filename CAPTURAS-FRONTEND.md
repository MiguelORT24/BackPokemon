# Guía de integración frontend: capturas

Esta guía describe el flujo de capturas implementado por la API. Las rutas protegidas requieren el JWT de la sesión (`Authorization: Bearer <token>`). Los IDs de objetos son los `Item.Id` internos de esta API, no los IDs de PokéAPI.

## Flujo recomendado

1. Cargar la mochila con `GET /api/me/inventory` y filtrar los objetos jugables por `item.tipoJuego` (`ball` o `berry`). Mostrar cantidad y, para bolas, `multiplicadorCaptura`.
2. Cuando el jugador esté en el mapa, consultar `GET /api/encuentros/cercanos` con su ubicación y un radio en metros.
3. Al abrir un encuentro, mostrar el Pokémon y su estado (`dormido`, `comiendo`, detección). El encuentro ya tiene estado individual por usuario.
4. Opcionalmente usar una baya con `POST /api/encuentros/{id}/bayas` y esperar/mostrar el tiempo indicado en `comiendoHasta`.
5. Elegir una Poké Ball y enviar `POST /api/capturas` con `encuentroId` y `pokeballItemId`.
6. Actualizar mochila, equipo Pokémon y Pokédex según la respuesta. Un intento consume una bola tanto si tiene éxito como si falla.
7. Para el historial, consultar `GET /api/me/capturas` o un detalle con `GET /api/me/capturas/{id}`.

## Autenticación y formato

- Las rutas de encuentros cercanos, capturas, bayas, objetos de campo cercanos/recoger, inventario y Pokédex requieren sesión autenticada.
- Enviar JSON con `Content-Type: application/json` para los POST con cuerpo.
- Fechas (`fecha`, `fechaExpiracion`, `comiendoHasta`) son fechas UTC ISO 8601. Compararlas con la hora actual para mostrar expiración y temporizadores.
- Los errores usan los códigos HTTP descritos abajo y normalmente un texto en el cuerpo. Conviene mostrar un mensaje recuperable para `409` (encuentro terminado, objeto agotado o sin unidades), refrescando datos.

## Endpoints de encuentro

### Consultar encuentros cercanos

`GET /api/encuentros/cercanos?latitud=19.4326&longitud=-99.1332&radioMetros=1000`

Parámetros requeridos: `latitud` (-90 a 90), `longitud` (-180 a 180). `radioMetros` es opcional (predeterminado 1000, máximo 5000, debe ser mayor a 0). La distancia se calcula en el servidor; devuelve solo encuentros del usuario del JWT que no se capturaron ni expiraron y que están dentro del radio.

Respuesta `200`:

```json
[
  {
    "id": 12,
    "pokemonId": 25,
    "pokemonNombre": "pikachu",
    "latitud": 19.4326,
    "longitud": -99.1332,
    "fechaExpiracion": "2026-10-27T20:30:00Z",
    "dormido": false,
    "comiendo": false,
    "jugadorDetectado": false,
    "tiroPorLaEspaldaDisponible": true
  }
]
```

Si la posición o radio está fuera de rango: `400`. El cliente debe tratar una lista vacía como “sin encuentros cercanos”. El backend no ofrece aquí una ruta de detalle individual de encuentro.

### Alta y estado de encuentros (operación de juego/admin)

Estas rutas son para el servicio de juego o herramientas administrativas con rol `GameAdmin`; no son acciones que el jugador deba llamar desde la app normal.

`POST /api/encuentros`

```json
{
  "pokemonId": 25,
  "usuarioId": "identity-user-id",
  "latitud": 19.4326,
  "longitud": -99.1332,
  "dormido": false,
  "duracionMinutos": 30
}
```

`pokemonId` debe existir en PokéAPI; `duracionMinutos` va de 1 a 1440 (predeterminado 30). El encuentro creado queda asociado a ese usuario. Responde `201` con el mismo formato `EncuentroDto` de la búsqueda.

`PUT /api/encuentros/{id}/estado`

```json
{
  "dormido": false,
  "jugadorDetectado": false,
  "tiroPorLaEspaldaDisponible": true,
  "calidadTiroDisponible": "normal"
}
```

`calidadTiroDisponible`: `normal`, `bueno`, `excelente` o `perfecto`. Esta ruta permite que el sistema de juego actualice el estado y la calidad del próximo lanzamiento. Responde `204`; `404` si el ID no existe y `409` si el encuentro terminó/expiró.

## Mochila y catálogo de objetos

### Mochila del usuario

`GET /api/me/inventory` devuelve los stacks del usuario autenticado. También existe `GET /api/ItemUser`.

Ejemplo de estructura (campos del DTO):

```json
[
  {
    "id": 4,
    "idItem": 3,
    "idUser": "identity-user-id",
    "cantidad": 5,
    "item": {
      "id": 3,
      "pokeApiItemId": 4,
      "nombre": "poke-ball",
      "urlImagen": "https://...",
      "descripcion": "...",
      "tipoJuego": "ball",
      "multiplicadorCaptura": 1.0,
      "duracionSegundos": null
    }
  }
]
```

Las propiedades adicionales de `item` (por ejemplo `probabilidad`, `efecto`, `valor`) pueden venir en el DTO. `tipoJuego` define el uso: `ball` significa Poké Ball utilizable; `berry` significa baya de captura. El multiplicador y la duración vienen de la regla de juego del ítem. Ignorar objetos sin `tipoJuego` para la mecánica de capturas. Enviar `idItem`/`item.id` como `pokeballItemId` o `bayaItemId`; no enviar el ID del stack (`id`) ni `pokeApiItemId`.

### Catálogo

- `GET /api/Items` (también `GET /api/Item`) devuelve el catálogo. `GET /api/Items/{id}` obtiene uno por ID.
- El catálogo también incluye `tipoJuego`, `multiplicadorCaptura` y `duracionSegundos`.
- Sincronización del catálogo y configuración de reglas requieren `GameAdmin`; el frontend de jugador solo necesita leerlo y su inventario.

## Usar una baya

`POST /api/encuentros/{encuentroId}/bayas`

```json
{ "bayaItemId": 17 }
```

El `bayaItemId` debe identificar un ítem de tipo `berry` que el usuario tenga en cantidad mayor a cero. Respuesta `200`:

```json
{
  "encuentroId": 12,
  "comiendoHasta": "2026-10-27T20:00:30Z",
  "cantidadRestante": 1
}
```

La baya se consume y el Pokémon come durante `duracionSegundos` configurado para ese ítem. Mientras come, el lanzamiento tiene un multiplicador situacional de ×1.5. No se puede usar otra baya mientras ya esté comiendo (`409`). Otros errores: `404` encuentro ajeno/inexistente; `409` encuentro expirado/capturado o sin baya; `400` si el objeto no tiene regla `berry`.

## Intentar capturar

`POST /api/capturas`

```json
{
  "encuentroId": 12,
  "pokeballItemId": 3
}
```

Ambos IDs son enteros positivos. La bola debe estar en la mochila y tener regla `TipoJuego = "ball"` con multiplicador. La probabilidad, el resultado y los modificadores se calculan en servidor. El cliente no envía porcentaje, calidad, ni flags de situación.

### Regla de probabilidad

El valor de `capture_rate` viene de PokéAPI:

```text
probabilidad normal = clamp(capture_rate / 255 × multiplicador de bola × multiplicador situacional, 0, 1)
```

Modificadores situacionales aplicados por el servidor:

| Situación | Multiplicador |
| --- | ---: |
| Jugador no detectado, primer lanzamiento | ×1.5 |
| Tiro por la espalda disponible y jugador no detectado | ×2 (reemplaza el ×1.5 anterior) |
| Pokémon comiendo | ×1.5 |
| Pokémon dormido | ×2 |
| Calidad `bueno` / `excelente` / `perfecto` | ×1.2 / ×1.5 / ×2 |

La calidad normal equivale a ×1. Los factores se multiplican entre sí y luego la probabilidad normal se limita a 100%. También existe una probabilidad de captura crítica de `2% × especies distintas registradas en Pokédex`, limitada al 25%; si ocurre, garantiza el éxito. El lanzamiento hace que el Pokémon detecte al jugador y consume el estado de tiro por la espalda; la calidad de tiro disponible vuelve a `normal` después del intento, incluso si la captura falla.

### Respuesta `200`

```json
{
  "captureId": 31,
  "captured": true,
  "remainingPokeballs": 4,
  "capture": {
    "id": 31,
    "encuentroId": 12,
    "pokemonId": 25,
    "pokemonNombre": "pikachu",
    "pokeballItemId": 3,
    "exitosa": true,
    "probabilidadAplicada": 0.45,
    "pokemonUserId": 108,
    "fecha": "2026-10-27T20:00:00Z",
    "captureRate": 190,
    "multiplicadorBola": 1.0,
    "multiplicadorSituacional": 1.5,
    "noTeVio": true,
    "tiroPorLaEspalda": false,
    "pokemonComiendo": false,
    "pokemonDormido": false,
    "probabilidadCritica": 0.04,
    "fueCritica": false,
    "calidadTiro": "normal"
  },
  "pokemon": {
    "id": 108,
    "idPokemon": 25,
    "nombre": "pikachu",
    "vidaActual": 35,
    "vidaTotal": 35,
    "estado": "Sano",
    "tipoPrimario": "electric",
    "tipoSecundario": null,
    "urlFrontal": "https://...",
    "urlTrasera": "https://..."
  },
  "message": null
}
```

`pokemon` es `null` si falla. En ese caso `captured` será `false` y `message` normalmente será `"El Pokémon escapó."`. En un éxito, se crea un Pokémon de usuario con estado inicial sano y vida completa, y se registra la especie en la Pokédex si aún no estaba. `remainingPokeballs` ya refleja la unidad consumida. Los nombres de propiedades se serializan en camelCase.

Errores esperables:

| HTTP | Significado / acción sugerida |
| --- | --- |
| `400` | El ítem indicado no es una Poké Ball utilizable. Volver a cargar la mochila. |
| `401` | JWT ausente o vencido; renovar sesión. |
| `404` | Encuentro ajeno/inexistente o Pokémon del encuentro no disponible en PokéAPI. |
| `409` | Encuentro expirado/ya capturado o sin unidades de la bola. Refrescar encuentros y mochila. |
| `502`, `504` | PokéAPI no respondió; permitir reintento. |

## Historial de capturas

- `GET /api/me/capturas` lista los intentos del usuario, recientes primero.
- `GET /api/me/capturas?success=true` filtra éxitos; `success=false` filtra fallos.
- `GET /api/me/capturas/{id}` devuelve un intento propio; `404` si no existe o pertenece a otro usuario.

Cada registro conserva el resultado, probabilidad calculada, bola, calidad y modificadores. `pokemonUserId` es `null` en un fallo.

## Pokédex y Pokémon del usuario

- `GET /api/me/pokedex` o `GET /api/Pokedex` devuelve especies descubiertas por el usuario. La captura exitosa registra una especie una sola vez; los duplicados siguen creando otro Pokémon de usuario.
- `pokemon.id` en la respuesta de captura es el ID de `PokemonUser` (instancia del equipo); `pokemon.idPokemon` es el ID de especie de PokéAPI. Para operaciones sobre la instancia, usar el primero; para Pokédex/datos de especie, el segundo.

## Objetos de campo (opcional)

El juego puede ofrecer objetos recogibles para conseguir bolas o bayas. La creación es administrativa; el jugador solo consulta y recoge.

- `GET /api/objetos-campo/cercanos?latitud=...&longitud=...&radioMetros=500`: devuelve drops no recogidos y no expirados dentro del radio (máximo 5000 m).
- `POST /api/objetos-campo/{id}/recoger?latitud=...&longitud=...`: el servidor valida que el jugador esté a 50 m o menos y suma la cantidad al inventario. Respuesta `200` incluye `id`, `itemId`, `nombre` y `cantidad` (cantidad total actual del stack).
- `409` si ya se recogió/expiró; `400` si coordenadas fuera de rango o distancia mayor a 50 m; `404` si no existe.

Ejemplo de un drop cercano:

```json
{
  "id": 8,
  "itemId": 3,
  "nombre": "poke-ball",
  "urlImagen": "https://...",
  "cantidad": 2,
  "latitud": 19.4326,
  "longitud": -99.1332,
  "fechaExpiracion": "2026-10-27T21:00:00Z"
}
```

## Relaciones de datos que impactan la UI

```text
Usuario ──< Encuentro (encuentro asignado a usuario)
Encuentro ──< Captura (cada intento queda en historial)
Usuario ──< Captura
Captura ──> Item (Poké Ball usada)
Captura ──> PokemonUser (solo cuando Exitosa; opcional)
Captura ──> Encuentro (opcional)
Usuario ──< ItemUser >── Item ──0..1 EfectoItem (ball/berry)
Usuario ──< Pokedex (una fila por especie)
Item ──< ObjetoCampo (drops del mapa)
```

`Encuentro` representa una aparición temporal del mapa y su estado individual (dormido, detectado, comiendo, tiro/calidad disponibles). `Captura` es el intento y su auditoría, incluso si falla. `PokemonUser` es la instancia poseída que aparece solo tras éxito. `Pokedex` es el registro único de especies vistas/obtenidas por el usuario. `ItemUser` representa el stack de mochila y `EfectoItem` da las reglas de juego al ítem.

## Notas de integración

- Las ubicaciones que se muestran son las que persisten en el servidor; usar geolocalización del dispositivo al consultar cercanos y recoger drops.
- El frontend no elige ni fuerza el éxito. Mostrar el resultado recibido y sincronizar mochila/equipo/Pokédex con el servidor.
- La respuesta de inventario debe ser la fuente para habilitar/deshabilitar la selección de objetos (`cantidad > 0`). Aun así, las operaciones validan stock en servidor, por lo que manejar `409` por concurrencia o cambios desde otro dispositivo.
- Algunos DTO incluyen propiedades opcionales (`pokemon`, `pokemonUserId`, `tipoJuego`, secundario). Manejar `null`.
