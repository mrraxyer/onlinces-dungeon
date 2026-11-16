# OnLinces: Debug Dungeon en Unity — diseño

Fecha: 2026-11-15 · Estado: borrador para aprobación

## Intención

Rehacer desde cero en Unity el roguelike *OnLinces: Debug Dungeon* (hoy en Godot 4.7, carpeta
`C:\Users\Rafael\Documents\Programming\onlinces-debug-dungeon`), con **los mismos personajes, sprites y audio**,
pero con una estructura más simple: menos sistemas generados por código, escenas visibles en el editor y
contenido definido como datos. La versión de Godot es la **especificación de referencia** (comportamientos,
números de balance, textos); no se traduce línea por línea.

## Decisiones del usuario

- **Alcance:** los 3 pisos completos (enemigo especial, evento y jefe de cada piso) en la primera versión terminada.
- **Escenas:** una por pantalla: `Title` → `Lobby` → `Floor` (la misma escena para los 3 pisos, con datos distintos) → `End`.
- **Puestos:** On/Off + 3 puestos al inicio: **Luis** (Script Estudiantil), **Sofi** (Cultura y Difusión),
  **Lemus** (Ciberseguridad), más el **Miembro** (puesto base). Los otros 5 se agregan después solo con datos y sprites.
- **Quién programa:** Claude escribe el código; el usuario prueba y decide.
- **Desde cero:** del proyecto de Godot solo se toman el **contexto** (diseño, reglas, números, textos) y el
  **arte/audio**. Ningún script ni estructura de Godot se porta. Godot queda congelado como referencia.
- **Todo se arma en el editor, paso a paso:** escenas, prefabs, salas, UI y configuración de importación se crean
  a mano en Unity, con guía. No hay scripts que generen escenas, assets o configuración (nada de "todo por código"
  como en Godot). El código C# es solo comportamiento del juego (moverse, disparar, enemigos, flujo).
  El arte se trae ya hecho (PNG/WAV); los generadores de Python no se mudan.
- **Proyecto independiente:** el repo `onlinces-dungeon` es la fuente de todo, incluido el arte. Sus Releases
  son propios (otros links); no reemplazan los de Godot.

## Decisiones de Claude (revisables)

- Unity **6000.6.9f1** (el que se instaló), URP 2D, Input System nuevo, uGUI para la interfaz.
- Proyecto: `C:\Users\Rafael\Documents\Programming\onlinces-dungeon\onlinces-dungeon` (repo git propio).
- **Salas hechas a mano** como prefabs (Tilemap) en vez del generador de Godot. Cada piso = **6 salas en línea**:
  Inicio → Combate → Combate → Cofre o Tienda (50/50) → Combate → Jefe. Las salas de combate se eligen al azar de
  un grupo de 3–4 plantillas por piso.
- **Armas: 8** (todas de disparo, sin cuerpo a cuerpo): `print()`, `for (i++)` escopeta, `while(true)`,
  Packet Tracer, Router lanzapaquetes + legendarias Deploy a producción, npm install --global, Recursión infinita.
- **Ítems: 6**: Café de las 3 a.m., Teclado mecánico, git push --force, Unit tests, Debugger, Firewall.
- **Minijuegos: solo el quiz** (preguntas de `QUIZ` en `game_data.gd`).
- **Props:** cofres, tienda, café/cafetera, cajas explosivas, routers rompibles (necesarios para el piso 2).
  Máquina expendedora, pizarrones y cables con chispas quedan fuera.
- **Habilidades:** una clase de C# por habilidad (que hereda de una base común) y el `RoleData` apunta a ella.
  Agregar un puesto nuevo = datos + sprites + su clase de habilidad. No se construye un sistema genérico de efectos.
- **Cuerpo a cuerpo pospuesto, no descartado:** `WeaponData` tiene un campo `kind` (disparo / cuerpo a cuerpo)
  y `PlayerWeapons` deja ese punto listo, aunque solo se implemente el disparo.
- Resolución base 480×320 con escalado entero (Pixel Perfect Camera), como en Godot.
- Números de balance (vida, daño, cadencia, velocidades, multiplicadores de dificultad) se copian de
  `game/scripts/game_data.gd` y de los scripts de Godot.

## Contenido

### Personajes

On y Off se arman con **3 capas** que se animan juntas (mismo cuadro, mismo flip):

| Capa | Archivo | Cuadrícula |
|---|---|---|
| Cuerpo / ropa del puesto | `{kit}_body.png` (On) · `off_{puesto}_body.png` (Off) | 5×4 de 48 px |
| Cabeza | `on_head.png` / `off_head.png` | 3×4 de 48 px |
| Extra (pose del puesto) | `{kit}_extra.png` / `off_{puesto}_extra.png` | 4×1 de 48 px |

La regla exacta de qué archivo usar por mascota y puesto está en `game_data.gd` (`CHARS`, `KITS`, `ROLES`,
`KIT_ALIAS`) y `player.gd:119`. En el lobby, los integrantes usan los retratos de `make_staff_art.py`.

### Pisos

| Piso | Enemigos (de `FLOORS`) | Especial | Evento (solo salas de combate) | Jefe |
|---|---|---|---|---|
| 1 Laboratorio | bug, fast, glitch, leak | Ctrl+V (se copia a los ~5 s, máx. 2 generaciones) | Apagón: círculo de luz (Light 2D), balas enemigas brillan | Spaghetti Code o `<div>` sin centrar |
| 2 Data center | + nullptr, virus, loop | DDoS (enjambre de 4) | Pico de tráfico: +45 % velocidad; romper un router lo corta | Kernel Panic |
| 3 La nube | igual que piso 2 | Cold Start (despierta y embiste) | Turbulencia: ráfagas que empujan jugador y balas | Ransomware |

Reglas de los eventos (del README): avisan 1.5 s antes, duran 3–7 s, calma de 11–20 s.

### Dificultad y progreso

Fácil / Media / Difícil con los multiplicadores de `DIFFICULTIES`. Guardado en
`Application.persistentDataPath/progreso.json`: qué dificultades se han ganado. En esta versión no hay puestos
bloqueados (los 3 elegidos son libres); el guardado queda listo para Rafa/Martín/Fierro.

## Estructura de archivos

```
Assets/
  Scenes/       Title, Lobby, Floor, End
  Scripts/
    Core/       GameManager (persiste entre escenas), SceneFlow, SaveSystem, AudioManager, InputReader
    Data/       RoleData, WeaponData, ItemData, EnemyData, FloorData, BossData, DifficultyData (ScriptableObject)
    Player/     PlayerController, PlayerOutfit, PlayerWeapons, Abilities/
    Combat/     Health, Projectile, ProjectilePool
    Enemies/    Enemy + CtrlV, DDoS, ColdStart
    Bosses/     BossBase + SpaghettiCode, DivUncentered, KernelPanic, Ransomware
    Dungeon/    FloorBuilder, Room, Door, WaveSpawner
    FloorEvents/ Blackout, TrafficSpike, Turbulence
    Props/      Chest, Shop, CoffeeMachine, Crate, Router, QuizTerminal
    UI/         HUD, Minimap, PauseMenu, TitleMenu, EndScreen, QuizPanel
    Dev/        CommandLineArgs, Bot
  Data/         .asset por rol, arma, ítem, enemigo, piso, jefe, dificultad
  Prefabs/      Player, Enemies/, Bosses/, Projectiles/, Props/, Rooms/F1..F3/, UI/
  Art/Sprites/  Characters/, Enemies/, Bosses/, Tiles/F1..F3/, Props/, Items/, Weapons/, FX/, UI/
  Art/Animations/, Art/Fonts/
  Audio/Music/, Audio/Sfx/
  Editor/       PixelSpriteImporter (Point, sin compresión, PPU fijo, solo en importación nueva), BuildScript
docs/superpowers/   specs y planes
```

En el paso 1 se copian una sola vez los PNG, WAV y la fuente de `game/assets/` de Godot (sin los `.import`).
Los datos (`.asset`) se crean a mano en el editor tomando los valores de `game_data.gd`; no hay sincronización
con Godot. La carpeta `Editor/` y `Tools/` de la estructura no se usan por ahora.

## Pasos (cada uno termina en algo jugable)

1. **Base:** importar sprites, audio y fuente con la configuración pixel; 4 escenas conectadas con botones; GameManager.
2. **On/Off:** personaje por capas que camina, apunta y dispara `print()` en una sala de prueba.
3. **Combate:** enemigo base, oleadas, sala que cierra puertas hasta limpiarla.
4. **Piso 1 completo:** 6 salas, cofre, tienda, ítems, Ctrl+V, apagón, jefe Spaghetti, HUD y minimapa.
   Juego jugable Title → Lobby (sin puestos) → Piso 1 → End.
5. **Lobby:** fuente y decoración, 3 puestos + Miembro con habilidad, terminal de dificultad, portal, guardado.
6. **Pisos 2 y 3:** enemigos, DDoS, Cold Start, tráfico, turbulencia, Kernel Panic, Ransomware, `<div>`. Juego completo.
7. **Pulido:** las 8 armas, café, cajas, routers, quiz, música y efectos.
8. **Pruebas y builds:** argumentos `--autoplay --bot --seed --floor --log` en batchmode; `BuildScript` para
   Windows y Mac; Release en GitHub del repo `onlinces-dungeon` (versiones propias, empezando en v0.1).

## Fuera de alcance (por ahora)

Los otros 5 puestos y sus desbloqueos, 7 armas restantes, 5 ítems restantes, minijuegos git y cazar bugs,
máquina expendedora, pizarrones, cables con chispas, video demo.

## Anti-objetivos

- Que On/Off o el arte se vean distintos (borrosos, otra escala, otros colores).
- Volver a armar todo por código en una sola escena: las salas y pantallas deben poder verse y editarse en Unity.
- Eventos de piso que castiguen sin aviso.

## Lo que decide quien construye

Nombres internos, organización de componentes dentro de cada prefab, layout exacto de cada sala, curvas de
animación y efectos visuales, mientras respeten lo de arriba.
