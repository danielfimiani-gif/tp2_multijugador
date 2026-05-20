# Plan de Ejecución — TP2 Photon Fusion 2

## 1. Visión del juego

- **Género:** fighting estilo Super Smash Bros, multijugador competitivo.
- **Cámara:** vista 2D lateral (proyección ortográfica) sobre escena 3D.
- **Jugadores:** 2–4 por partida.
- **Combate:** porcentaje de daño + knockback proporcional (estilo Smash clásico).
- **Condición de victoria:**
  - Principal: stock (vidas) — gana el último en pie.
  - Tiempo límite obligatorio: si se acaba, gana quien tenga más vidas restantes.
  - Tie-breaker: menor % de daño actual.
  - Puntaje secundario sincronizado: KOs por jugador.

## 2. Arquitectura técnica

### 2.1 Topología de red

- `GameMode.Host` (el creador de sala actúa como host autoritativo).
- Lobby custom Photon (`SessionLobby.Custom`) con nombre fijo.
- Matchmaking: listado de salas; crear si no hay; unirse seleccionando.
- **Cupo mínimo configurable** antes de iniciar el match (Tier S).

### 2.2 Scripts

Estado: ✅ existe / ✏️ a refactorizar / ⛔ a crear.

| Archivo | Estado | Responsabilidad |
|---|---|---|
| `Scripts/Common/MonoBehaviourSingleton.cs` | ✅ | Singleton genérico (ya OK). |
| `Scripts/Network/NetworkRunnerController.cs` | ✏️ | Cambiar `GameMode.Shared` → `Host`; mover el spawn fuera (lo hace `GameManager`). |
| `Scripts/Network/GameManager.cs` | ⛔ | `NetworkBehaviour` que orquesta partida: estado, timer, KOs, fin de juego, spawn de jugadores. |
| `Scripts/Network/PlayerController.cs` | ⛔ | Input + `NetworkCharacterController` + integración con `NetworkMecanimAnimator`. |
| `Scripts/Network/PlayerCombat.cs` | ⛔ | `[Networked] DamagePercent`, `LastHitter`, gestión de ataque/knockback. |
| `Scripts/Network/PlayerStock.cs` | ⛔ | `[Networked] Lives`, respawn, detección de caída fuera del stage. |
| `Scripts/Combat/HitBox.cs` | ⛔ | Trigger activado durante un ataque; aplica daño al HurtBox que toca. |
| `Scripts/Combat/HurtBox.cs` | ⛔ | Receptor de daño; referencia a su `PlayerCombat`. |
| `Scripts/Combat/KillZone.cs` | ⛔ | Volumen que mata al jugador que entra (bordes del stage). |
| `Scripts/UI/MainMenuUI.cs` | ✏️ | Completar lógica: crear sala, listar, unirse. |
| `Scripts/UI/RoomItemUI.cs` | ⛔ | Vista de una sala en la lista. |
| `Scripts/UI/HUD.cs` | ⛔ | Por jugador: %, vidas. Global: timer, KOs. |
| `Scripts/UI/EndGameUI.cs` | ⛔ | Pantalla de fin con ranking y botón "Volver al menú". |

### 2.3 Propiedades `[Networked]` (mínimo 3 — Tier A)

1. `PlayerCombat.DamagePercent` — `float`.
2. `PlayerStock.Lives` — `int`.
3. `PlayerCombat.LastHitter` — `PlayerRef`.
4. `GameManager.MatchTimer` — `TickTimer`.
5. `GameManager.State` — enum `MatchState { WaitingForPlayers, Countdown, InProgress, Ended }`.
6. `GameManager.Kos` — `NetworkDictionary<PlayerRef, int>` (o array indexado).

Total: 6 (excede el requisito de 3).

### 2.4 RPCs (mínimo 3 — Tier A)

1. `RPC_RequestAttack(AttackType type)` — cliente → host: pedido de ataque (input authority → state authority).
2. `RPC_OnHit(PlayerRef victim, PlayerRef attacker, float damage, Vector2 knockback)` — host → all: feedback visual/sonoro del golpe.
3. `RPC_OnKO(PlayerRef victim, PlayerRef killer)` — host → all: anuncia un KO, actualiza HUD.
4. `RPC_OnMatchEnd(PlayerRef winner)` — host → all: muestra `EndGameUI`.

Total: 4 (excede el requisito de 3).

### 2.5 Físicas y cámara

- **Personaje:** `NetworkCharacterController` (3D).
  - Constraints lógicos: posición `Z` fija; rotación bloqueada en `X` y `Z`; rotación `Y` solo para flip visual.
  - Movimiento: input horizontal (X), salto vertical (Y), opcional doble salto.
- **Cámara:** ortográfica, mira al eje `+Z` o `-Z`. Sigue al centroide de jugadores con clamps al stage.
- **Stage:** plataformas como `BoxCollider` 3D; bordes invisibles con `KillZone`.

### 2.6 Animación (Tier S)

- `NetworkMecanimAnimator` en el prefab `Player`.
- Animator con estados mínimos: `Idle`, `Run`, `Jump`, `Fall`, `Attack`, `Hit`, `KO`.
- Para el prototipo: cápsula/cubo con animaciones placeholder; lo importante es que el componente esté integrado y sincronice.

## 3. Hitos (orden de implementación)

### M1 — Setup base de red (host-client + escenas)
**Entregables:**
- `NetworkRunnerController` con `GameMode.Host`.
- `MainMenuUI` funcional: lista salas, crear, unirse.
- `RoomItemUI` listo.
- Transición a escena `Game` al iniciar partida.
- Lobby con cupo mínimo (espera en `Game` hasta N jugadores).

### M2 — Personaje base
**Entregables:**
- Prefab `Player` con `NetworkCharacterController`, `NetworkObject`, `NetworkMecanimAnimator`.
- `PlayerController`: input + movimiento XY + salto.
- Cámara ortográfica con seguimiento.
- Stage placeholder con plataforma y `KillZone`.

### M3 — Combate (% + knockback)
**Entregables:**
- `HitBox` / `HurtBox`.
- `PlayerCombat` con `DamagePercent` networked.
- Cálculo de knockback en función del %.
- `LastHitter` para crédito de KO.
- `RPC_RequestAttack`, `RPC_OnHit`.

### M4 — Stock, KO y respawn
**Entregables:**
- `PlayerStock` con `Lives` networked.
- Respawn en posición segura.
- Modo espectador (cámara libre / observador) al quedarse sin vidas.
- `RPC_OnKO`.

### M5 — GameManager (timer, scoring, fin)
**Entregables:**
- `GameManager` con `State`, `MatchTimer`, `Kos`.
- Cuenta regresiva inicial (countdown).
- Detección de fin: 1 jugador vivo o tiempo agotado (con tie-breaker por %).
- `RPC_OnMatchEnd`.

### M6 — HUD + EndGame
**Entregables:**
- `HUD`: %, vidas por jugador; timer y KOs globales.
- `EndGameUI`: ranking, botón "Volver al menú".

### M7 — Pulido y QA
**Entregables:**
- Test con 2–4 clientes locales.
- Suavizado de movimiento / interpolación.
- Fix de bugs bloqueantes (requisito Tier S).
- Commits frecuentes (requisito de la consigna).

## 4. Checklist por Tier

### Tier B (4)
- [ ] Photon Fusion 2 integrado y usado.
- [ ] Topología host-client (`GameMode.Host`).
- [ ] Puntos sincronizados (KOs por `PlayerRef`).
- [ ] Tiempo límite (`TickTimer` en `GameManager`).

### Tier A (7) — requiere Tier B completo
- [ ] ≥ 3 propiedades `[Networked]` (apuntamos a 6).
- [ ] ≥ 3 RPCs (apuntamos a 4).
- [ ] `NetworkCharacterController` para movimiento.

### Tier S (10) — requiere Tier A completo
- [ ] `NetworkMecanimAnimator` sincronizando animaciones.
- [ ] Lobby con cupo mínimo: la partida arranca solo al cumplirse.
- [ ] Sin bugs bloqueantes recurrentes.

## 5. Estructura de carpetas final

```
Assets/_Project/
├── Art/
│   ├── Materials/
│   ├── Animations/        ← agregar
│   └── Sprites/           ← opcional
├── Prefabs/
│   ├── Player/Player.prefab
│   ├── Stage/             ← agregar (plataforma, kill zones)
│   ├── UI/RoomItem.prefab
│   └── FX/                ← opcional (hit effects)
├── Scenes/
│   ├── MainMenu.unity
│   └── Game.unity
└── Scripts/
    ├── Common/
    ├── Combat/            ← HitBox, HurtBox, KillZone
    ├── Network/
    └── UI/
```

## 6. Decisiones abiertas

A definir antes / durante M2–M3:

- **Doble salto:** ¿lo incluimos? (recomiendo: sí, es muy Smash).
- **Set de ataques:** mínimo viable = jab básico + ataque aéreo. Opcional: smash cargado, ataque hacia abajo. Recomendado partir de 2 y crecer.
- **Cantidad de stages:** 1 fijo para entrega; si sobra tiempo, agregar 1–2 más.
- **Personajes:** 1 único personaje placeholder con stats fijos.
- **Cupo mínimo del lobby:** 2 jugadores (ajustable).
- **Vidas por jugador:** 3 (ajustable).
- **Duración del match:** 2–3 minutos (ajustable).

## 7. Riesgos

- **Sincronización de hitboxes / knockback:** sensible al modelo de autoridad. Mitigación: el host calcula daño y knockback; el cliente solo solicita el ataque vía RPC.
- **Movimiento "feel":** `NetworkCharacterController` puede sentirse rígido. Mitigación: ajustar gravedad, acceleration custom; aceptar que es prototipo.
- **Bugs bloqueantes en lobby/transiciones:** requisito Tier S. Mitigación: testear cada hito con 2+ clientes antes de avanzar.
- **Tiempo:** plan ambicioso. Si apremia, recortar en este orden: FX → variedad de ataques → animaciones reales (mantener Animator pero con stubs).
