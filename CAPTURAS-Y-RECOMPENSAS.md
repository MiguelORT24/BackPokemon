# Capturas, objetos de campo y recompensas

## Catálogo e inventario

- `POST /api/Item/sync/capture-items` sincroniza todos los ítems de las categorías `poke-balls` y `berries` de PokéAPI. Conserva el catálogo externo completo de esas categorías; las reglas del juego viven en `EfectoItem`.
- La sincronización asigna reglas iniciales a Poké Ball (`×1`), Great/Super Ball (`×1,5`), Ultra Ball (`×2`) y Razz Berry (distracción de 30 segundos). Los demás ítems quedan en catálogo, sin efecto jugable hasta configurar uno.
- Para habilitar más objetos, `PUT /api/Item/{id}/regla-juego` permite a `GameAdmin` configurarlos como `ball` con `multiplicadorCaptura` o `berry` con `duracionSegundos`.
- `GET /api/me/inventory` sigue siendo la mochila (`ItemUser`) y solo devuelve el inventario del JWT.
- La recarga pasiva entrega 15 bolas cada 30 minutos: 10 Poké Balls y 5 bolas asignadas al azar entre Great/Super Ball y Ultra Ball. Cada tipo tiene tope de 100; si una de las dos especiales llega al tope, las tiradas se asignan a la otra mientras tenga espacio.
- La primera consulta elegible entrega el paquete inicial de 15. El sistema calcula los intervalos vencidos cuando el usuario consulta su inventario, busca encuentros o intenta capturar; no requiere un proceso programado ni guarda encuentros adicionales. Si el catálogo o las reglas de captura aún no están sincronizados, la recarga espera y vuelve a intentar en la próxima consulta.

La sincronización y la configuración de ítems requieren el rol `GameAdmin`. `GameAdmin` debe asignarse mediante el proceso administrativo de Identity; el backend no otorga ese rol automáticamente. Las rutas de lectura de catálogo siguen disponibles según el controlador existente. `Item.Probabilidad` se conserva por compatibilidad, pero el cálculo de captura ya no lo usa.

## Encuentros

- `GET /api/encuentros/cercanos?latitud=...&longitud=...&radioMetros=1000` lista encuentros activos cercanos y crea uno aleatorio para la zona actual si aún no hay uno activo.
- Las zonas usan celdas geográficas de `0,01°` (aproximadamente 1 km). Todas las personas que consultan la misma celda ven el mismo Pokémon, que expira 60 minutos después de generarse.
- El estado particular de cada jugador (si ya lo capturó, bayas, detección y calidad de tiro) vive en `EncuentroUsuario`. Cada cuenta puede capturar su propia copia una vez; capturarla no elimina el encuentro para los demás.
- `POST /api/encuentros` y `PUT /api/encuentros/{id}/estado` siguen disponibles para administración y pruebas, con rol `GameAdmin`; no son necesarios durante el juego normal.
- Cada consulta de cercanos elimina los encuentros vencidos. El estado de usuario se borra en cascada; el historial de intentos permanece en `Captura` y conserva el resultado aunque ya no exista el encuentro asociado.

## Intentar capturar

`POST /api/capturas` requiere JWT:

```json
{
  "encuentroId": 12,
  "pokeballItemId": 3
}
```

`calidadTiroDisponible` acepta `normal`, `bueno`, `excelente` y `perfecto`, con multiplicadores `×1`, `×1,2`, `×1,5` y `×2`. El estado del jugador en el encuentro registra y consume la calidad y el tiro por la espalda; el cliente de captura no puede enviarlos directamente. “No te vio” y comer también son estado por jugador; dormir es una condición compartida del spawn.

La probabilidad normal es `capture_rate / 255 × multiplicador de bola × modificadores`, limitada a 100 %. La captura crítica ocurre con `2 % × especies distintas de la Pokédex`, hasta `25 %`, y garantiza el éxito. La respuesta y el historial conservan los factores y resultado de cada intento. Una Poké Ball se consume tanto si el intento tiene éxito como si falla.

`POST /api/encuentros/{id}/bayas` con `{ "bayaItemId": 17 }` consume una baya de captura y deja al Pokémon comiendo durante el tiempo configurado en el efecto (30 segundos para Razz Berry). Mientras come recibe `×1,5`.

## Objetos recogidos en el campo

- `GET /api/objetos-campo/cercanos?latitud=...&longitud=...&radioMetros=500` lista objetos disponibles.
- `POST /api/objetos-campo` genera un objeto de campo (rol `GameAdmin`).
- `POST /api/objetos-campo/{id}/recoger?latitud=...&longitud=...` lo asigna a `ItemUser`; requiere JWT, una distancia máxima de 50 metros y el objeto solo puede recogerse una vez.

## Premios de batalla

Crear una batalla registra su finalización y entrega una unidad elegida con probabilidad uniforme entre los ítems configurados como bola o baya. El resultado queda asociado a la batalla y se devuelve en la respuesta. `LlaveIdempotencia` debe ser un GUID nuevo por batalla y reutilizarse al reintentar la misma solicitud; así no se vuelve a entregar el premio. La creación requiere el rol `GameAdmin` para impedir que el cliente se otorgue premios inventando batallas.
