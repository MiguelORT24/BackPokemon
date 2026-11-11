# Guía de endpoints para frontend

Esta guía describe las consultas de Pokémon, la curación, la búsqueda de usuarios
y el flujo de intercambios implementado en el backend. Los IDs, usuarios y fechas
de los ejemplos son ilustrativos: sustitúyanlos por los valores de las respuestas.

## 1. Conexión y autenticación

Configuren `BASE_URL` con el origen donde esté ejecutándose el backend
(protocolo, host y puerto). No incluye una ruta como `/me` o `/trades`.
En un dispositivo físico, `localhost` apunta al dispositivo, no a la computadora
del backend; utilicen la dirección accesible de esa computadora.

Todas las rutas de Pokémon, usuarios e intercambios de esta guía requieren:

```http
Authorization: Bearer <token>
```

Cuando envíen un cuerpo JSON, agreguen:

```http
Content-Type: application/json
```

Las rutas del juego funcionan con y sin el prefijo `/api`:
`/me/pokemon` y `/api/me/pokemon` son equivalentes. Usen una convención
consistente. Las rutas de autenticación son `/api/auth/register` y
`/api/auth/login`.

### Obtener el token

```http
POST /api/auth/login
```

Cuerpo obligatorio:

```json
{
  "userName": "nombre",
  "password": "TuContraseña"
}
```

Respuesta `200 OK`:

```json
{
  "token": "<token JWT>",
  "expiration": "2026-10-01T18:00:00Z"
}
```

Guarden el token para enviarlo en las siguientes peticiones. Un login incorrecto
devuelve `401`. Si necesitan registrar una cuenta:

```http
POST /api/auth/register
```

```json
{
  "userName": "mario",
  "email": "mario@example.com",
  "password": "Ejemplo!234a"
}
```

El registro devuelve `200` con un mensaje; no devuelve un token.
Después deben iniciar sesión. Si no cumple las reglas de Identity o el usuario
ya existe, devuelve `400` con los errores.

## 2. Resumen de peticiones

| Método | Ruta | Cuerpo |
|---|---|---|
| GET | `/me/pokemon` | Ninguno |
| GET | `/me/pokemon?health_lt=100&available=true` | Ninguno |
| GET | `/me/pokemon?available=true` | Ninguno |
| POST | `/me/pokemon/heal` | Ninguno o `{}` |
| GET | `/users?search=mario` | Ninguno |
| POST | `/trades` | `recipientUserId`, `pokemonUserId` |
| GET | `/me/trades?status=pending` | Ninguno |
| GET | `/trades/{id}` | Ninguno |
| POST | `/trades/{id}/accept` | `pokemonUserId` |
| POST | `/trades/{id}/confirm` | Ninguno o `{}` |
| POST | `/trades/{id}/cancel` | Ninguno o `{}` |

Las peticiones GET no llevan JSON: sus filtros van en la URL.
En las rutas, sustituyan `{id}` por el ID numérico del intercambio, sin llaves.

## 3. Consultar mis Pokémon

```http
GET /me/pokemon
```

**Qué hace:** obtiene los Pokémon del usuario identificado por el token.
No se envía un ID de usuario ni cuerpo JSON.

Respuesta `200 OK`:

```json
[
  {
    "id": 12,
    "idPokemon": 25,
    "idUsuario": "usuario-mario",
    "nombre": "Pikachu",
    "vidaActual": 80,
    "vidaTotal": 100,
    "estado": "Sano",
    "available": true,
    "reserved": false,
    "crianza": null
  }
]
```

Si no hay resultados devuelve `[]`.

**Diferencia entre IDs:** `id` identifica el Pokémon particular de un usuario.
Ese valor se envía como `pokemonUserId` al crear o aceptar intercambios.
`idPokemon` identifica su especie en la Pokédex; no lo usen como
`pokemonUserId`.

| Campo | Cómo usarlo |
|---|---|
| `nombre` | Nombre mostrado; puede ser null. |
| `vidaActual`, `vidaTotal` | Puntos de vida; pueden ser null si no hay estado registrado. |
| `estado` | Nombre del estado como texto, por ejemplo `Sano`; puede ser null. |
| `available` | Indica si se puede ofrecer en un intercambio en este momento. |
| `reserved` | Indica si está comprometido en un intercambio `pending` o `accepted`. |
| `crianza` | Datos de Tamagotchi o null si no tiene crianza. |

Cuando existe crianza, su forma es:

```json
{
  "hambre": 90,
  "energia": 80,
  "higiene": 100,
  "vivo": true,
  "ultimaActualizacion": "2026-10-01T15:00:00Z",
  "fechaAdopcion": "2026-10-01T10:00:00Z",
  "fechaMuerte": null
}
```

La crianza refleja el último estado persistido por Tamagotchi.
Un Pokémon está disponible si tiene vida mayor que cero, no tiene crianza
muerta y no está reservado. `available=false` no implica necesariamente
`reserved=true`: también puede faltarle vida o estado.

### Filtrar por vida y disponibilidad

```http
GET /me/pokemon?health_lt=100&available=true
```

Devuelve los propios con vida actual menor a 100 y disponibles para intercambio.

| Parámetro opcional | Valores | Significado |
|---|---|---|
| `health_lt` | Entero entre 0 y 100 | Vida actual estrictamente menor al valor. Son puntos de vida, no porcentaje. |
| `available` | `true` o `false` | Filtra por disponibilidad. |

Ambos filtros se combinan. Si se omite uno, no se filtra por ese criterio.
Los Pokémon sin estado registrado no coinciden con el filtro de vida.
Un valor inválido devuelve `400`.

### Consultar solo los listos para intercambiar

```http
GET /me/pokemon?available=true
```

Úsenlo para llenar el selector de Pokémon al invitar o aceptar un intercambio.
La disponibilidad puede cambiar después de la consulta; el backend vuelve a
comprobarla al enviar la operación.

## 4. Curar mis Pokémon

```http
POST /me/pokemon/heal
```

**Cuerpo:** pueden omitirlo o enviar:

```json
{}
```

**Qué hace:** restaura la vida hasta `vidaTotal`, pone el estado en `Sano`
y reinicia los contadores de estado de todos los Pokémon propios con estado
registrado y sin crianza muerta. No revive mascotas muertas.

No recibe una lista de Pokémon ni permite seleccionar uno en particular.

Respuesta `200 OK`:

```json
{
  "healed": 3
}
```

`healed` cuenta las filas actualizadas, incluso si ya estaban sanas; puede ser 0.
Después de curar, vuelvan a consultar `GET /me/pokemon` para actualizar la pantalla.

## 5. Buscar usuarios para intercambiar

```http
GET /users?search=mario
```

**Cuerpo:** ninguno.

**Qué hace:** busca coincidencias en el nombre de usuario. Excluye al usuario
autenticado y devuelve hasta 50 resultados ordenados por nombre.
No busca por correo.

`search` es obligatorio y debe tener entre 2 y 100 caracteres después de
recortar espacios. Codifiquen el valor para URL (por ejemplo, con
`URLSearchParams`). Una búsqueda inválida devuelve `400`.

Respuesta `200 OK`:

```json
[
  {
    "id": "usuario-mario",
    "userName": "mario"
  }
]
```

Sin coincidencias devuelve `[]`. Usen el `id` elegido como
`recipientUserId` al crear la invitación.

## 6. Crear una invitación

```http
POST /trades
```

Cuerpo obligatorio:

```json
{
  "recipientUserId": "usuario-luigi",
  "pokemonUserId": 12
}
```

| Campo | Tipo | De dónde obtenerlo |
|---|---|---|
| `recipientUserId` | string obligatorio | `id` del usuario elegido en `GET /users`. |
| `pokemonUserId` | entero positivo | `id` de un Pokémon propio en `GET /me/pokemon?available=true`. |

**Qué hace:** crea un intercambio `pending` y reserva el Pokémon del creador.
El creador se obtiene del JWT. No manden creador, estado, fechas ni Pokémon
del destinatario: este último lo elige al aceptar.

Devuelve `201 Created`, el objeto de intercambio descrito en la sección 7
y la cabecera `Location: /trades/{id}`.

Errores relevantes:

- `400`: campos inválidos o intento de invitarse a sí mismo.
- `404`: el destinatario no existe.
- `409`: el Pokémon no es propio, no está vivo/disponible o ya está reservado.

## 7. Forma de la respuesta de un intercambio

Crear, consultar, aceptar, confirmar y cancelar devuelven este mismo formato.
Ejemplo de una invitación recién creada, vista por el creador:

```json
{
  "id": 5,
  "role": "sent",
  "status": "pending",
  "creator": {
    "id": "usuario-mario",
    "userName": "mario"
  },
  "recipient": {
    "id": "usuario-luigi",
    "userName": "luigi"
  },
  "offeredPokemon": {
    "id": 12,
    "idPokemon": 25,
    "nombre": "Pikachu"
  },
  "recipientPokemon": null,
  "createdAt": "2026-10-01T16:00:00Z",
  "acceptedAt": null,
  "confirmedAt": null
}
```

- `id`: se usa en las rutas `/trades/{id}`.
- `role`: `sent` para el creador, `received` para el destinatario.
  Cambia según el usuario del token que haga la consulta.
- `status`: estado actual; consulten la tabla de acciones de la sección 12.
- `offeredPokemon`: Pokémon ofrecido por el creador.
- `recipientPokemon`: null hasta aceptar. Después contiene
  `id`, `idPokemon` y `nombre`, igual que `offeredPokemon`.
- `acceptedAt` y `confirmedAt`: null mientras no se haya realizado esa acción.

Las fechas se generan en UTC. Al leer fechas persistidas, la serialización
puede no incluir el sufijo `Z`; interprétenlas como UTC antes de convertirlas
a la hora local.

El detalle de intercambio incluye solo ID, especie y nombre del Pokémon,
no todos sus datos de salud. Los participantes representan a quienes realizaron
el intercambio, incluso después de cambiar los propietarios.

## 8. Consultar invitaciones enviadas y recibidas

```http
GET /me/trades?status=pending
```

**Cuerpo:** ninguno.

**Qué hace:** devuelve los intercambios donde el usuario es creador o destinatario,
ordenados del más reciente al más antiguo.

Respuesta `200 OK`: un arreglo de objetos con el formato de la sección 7,
o `[]` si no hay coincidencias.

`status` es opcional. Valores exactos, en minúsculas:
`pending`, `accepted`, `confirmed`, `cancelled`, `rejected`.
Un valor distinto devuelve `400`.

Para consultar todos:

```http
GET /me/trades
```

Usen `role` para separar “Enviadas” y “Recibidas”.
`status=pending` no incluye invitaciones ya aceptadas: para mostrar también las
que esperan confirmación, consulten todos o hagan otra consulta con
`status=accepted`.

## 9. Consultar un intercambio específico

```http
GET /trades/5
```

**Cuerpo:** ninguno.

Devuelve `200 OK` con el objeto de la sección 7. Solo pueden consultarlo
sus participantes. Si no existe o pertenece a otros usuarios, devuelve `404`.

## 10. Aceptar y confirmar

### Aceptar: lo hace el destinatario

```http
POST /trades/5/accept
```

Cuerpo obligatorio:

```json
{
  "pokemonUserId": 25
}
```

`pokemonUserId` debe ser un entero positivo que identifique un Pokémon
propio del destinatario, elegido de `GET /me/pokemon?available=true`.

**Qué hace:** cambia `pending -> accepted`, guarda el Pokémon del destinatario
y lo reserva. En este momento ambos Pokémon están reservados; todavía no
cambian de propietario.

Respuesta `200 OK`: objeto de intercambio con `status: "accepted"`,
`recipientPokemon` y `acceptedAt` completos.

Errores: `400` por cuerpo inválido; `403` si lo intenta el creador;
`404` si no existe o es ajeno; `409` si ya no está pendiente o alguno de los
Pokémon no cumple las condiciones.

### Confirmar: lo hace el creador

```http
POST /trades/5/confirm
```

**Cuerpo:** ninguno o:

```json
{}
```

**Qué hace:** requiere `accepted`, vuelve a validar ambos Pokémon y cambia
sus propietarios en una sola transacción. Pasa a `confirmed` y libera las
reservas.

Respuesta `200 OK`: objeto con `status: "confirmed"` y `confirmedAt`.

Errores: `403` si lo intenta el destinatario; `404` si no existe o es ajeno;
`409` si no está aceptado o los Pokémon dejaron de ser válidos.
Confirmar dos veces devuelve `409`; no ejecuta nuevamente el intercambio.

Después, actualicen `GET /me/pokemon` y la lista de intercambios.

## 11. Cancelar o rechazar

```http
POST /trades/5/cancel
```

**Cuerpo:** ninguno o:

```json
{}
```

**Qué hace:** termina un intercambio activo y libera sus reservas, conservando
el historial. No se elimina el registro.

| Estado actual | Quién envía la petición | Estado resultante |
|---|---|---|
| `pending` | Creador | `cancelled` |
| `pending` | Destinatario | `rejected` |
| `accepted` | Cualquiera de los dos | `cancelled` |

Respuesta `200 OK`: objeto del intercambio con el estado resultante.
Devuelve `404` si no existe o es ajeno y `409` si ya terminó.
No permite deshacer un intercambio confirmado.

## 12. Flujo recomendado y botones de la interfaz

1. Iniciar sesión y obtener el token.
2. Consultar `GET /me/pokemon?available=true` para seleccionar un Pokémon.
3. Buscar al destinatario con `GET /users?search=...`.
4. Enviar `POST /trades` con el usuario elegido y el Pokémon propio.
5. El destinatario consulta `GET /me/trades?status=pending`.
6. El destinatario elige su Pokémon disponible y envía `POST /trades/{id}/accept`.
7. El creador consulta el detalle y envía `POST /trades/{id}/confirm`.
8. Ambos actualizan su colección de Pokémon y sus intercambios.

| Estado | Significado | Acciones del creador | Acciones del destinatario |
|---|---|---|---|
| `pending` | Invitación enviada; Pokémon del creador reservado | Cancelar | Aceptar o rechazar |
| `accepted` | Ambos Pokémon reservados | Confirmar o cancelar | Cancelar |
| `confirmed` | Intercambio ejecutado | Consultar | Consultar |
| `cancelled` | Intercambio cancelado | Consultar | Consultar |
| `rejected` | Invitación rechazada | Consultar | Consultar |

Deshabiliten el botón mientras envían la petición para evitar dobles envíos.
Actualicen las listas después de cada operación. Estos endpoints no envían
notificaciones en tiempo real: vuelvan a consultar al abrir o refrescar la
pantalla y mientras esperen una respuesta, según el comportamiento del juego.

## 13. Manejo de errores

| Código HTTP | Significado | Acción en frontend |
|---|---|---|
| `400` | Parámetro o cuerpo inválido | Mostrar el error y corregir los datos. |
| `401` | Token ausente, inválido o vencido | Solicitar inicio de sesión. |
| `403` | El participante no puede realizar esa acción | Actualizar permisos/botones según su rol. |
| `404` | Recurso inexistente o intercambio privado ajeno | Avisar y actualizar la lista. |
| `409` | Estado, propietario o reserva en conflicto | Consultar de nuevo el intercambio y los Pokémon disponibles. |
| `500` o error de red | Fallo del servidor o de conexión | Mostrar un mensaje; consultar el estado antes de repetir una operación. |

No todos los errores tienen el mismo cuerpo. Algunos son texto plano,
otros son JSON de validación y otros no tienen cuerpo. No asuman que todas
las respuestas pueden leerse con `response.json()`.

Después de un error de red al crear o confirmar, consulten los intercambios:
la operación pudo completarse aunque no haya llegado la respuesta.
No repitan automáticamente un POST sin comprobar el resultado.

### Ejemplo JavaScript con fetch

```javascript
// Definir BASE_URL con el origen real del backend y token con el JWT del login.
async function api(path, { method = "GET", body } = {}) {
  const response = await fetch(BASE_URL + path, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(body !== undefined ? { "Content-Type": "application/json" } : {})
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {})
  });

  const text = await response.text();
  const isJson = response.headers.get("content-type")?.includes("json");
  const data = text ? (isJson ? JSON.parse(text) : text) : null;

  if (!response.ok) {
    const error = new Error(
      typeof data === "string" ? data : data?.title ?? `HTTP ${response.status}`
    );
    error.status = response.status;
    error.data = data; // Puede incluir errors de validación.
    throw error;
  }
  return data;
}

// Consultar los disponibles.
const pokemon = await api("/me/pokemon?available=true");

// Crear una invitación: reemplazar estos valores por la selección del usuario.
const trade = await api("/trades", {
  method: "POST",
  body: { recipientUserId: "usuario-luigi", pokemonUserId: 12 }
});

// Confirmar, usando el token del creador y solo después de la aceptación.
await api(`/trades/${trade.id}/confirm`, { method: "POST" });
```

## 14. Notas de integración

- No usen el antiguo CRUD `/api/Intercambios`: el flujo actual usa `/trades`.
- El cliente no debe mandar `status`, propietarios ni fechas en los cuerpos de
  intercambio; los determina el backend.
- Si el frontend web corre en otro origen, su acceso requiere configuración
  CORS en el backend. Las pruebas HTTP directas no comprueban CORS del navegador.
- Las listas de Pokémon e intercambios no tienen paginación en estos endpoints.
