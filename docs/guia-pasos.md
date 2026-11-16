# Guía paso a paso — qué importar y cuándo

Todo el arte y audio vive en **`ArtSource/`** (fuera de `Assets/`, Unity no lo ve). En cada paso se arrastra a la
ventana **Project** solo lo que ese paso usa, a la carpeta indicada de `Assets/`. El preset `PixelSprite`
configura solo los sprites nuevos (Point, sin compresión, 32 PPU). Después solo hay que **cortar** las hojas.

**Cómo cortar una hoja:** seleccionar el PNG → *Sprite Mode: Multiple* → **Open Sprite Editor** → **Slice** →
*Type: Grid By Cell Count* → columnas × filas de la tabla → *Pivot* (Center salvo que se indique) → **Slice** → **Apply**.

Ya se borró lo que no entra en el juego: los puestos Celestino, Víctor, Rafa, Martín y Fierro, los kits viejos,
y los props máquina expendedora, pizarrón, cables, impresora, archivero, mesa de juntas, librero, trofeos, bandera,
podio y escritorio de oficina. Si algún día vuelven, están en el repo de Godot.

---

## Paso 1 — Pantalla de título y personajes  ← (ya importado)

| Archivo | Carpeta en Assets | Cortar (col × fila) | Pivot |
|---|---|---|---|
| `title.png` | `Art/Sprites/UI` | — (1 sprite) | Center |
| `on_full.png`, `off_full.png` | `Art/Sprites/Characters/On`, `/Off` | 5 × 4 | Bottom |
| `on_body.png`, `off_body.png` | igual | 5 × 4 | Bottom |
| `on_head.png`, `off_head.png` | igual | 3 × 4 | Bottom |
| `on_extra.png`, `off_extra.png` | igual | 4 × 1 | Bottom |
| `shadow.png` | `Art/Sprites/Characters` | — | Center |

*`_full` = On/Off completo (para menús). `_body` + `_head` + `_extra` = capas del personaje jugable.*

**En el editor:** escena `Title` con Pixel Perfect Camera (32 PPU, 480×320), logo, On y Off parados a los lados,
botones "Jugar con On" / "Jugar con Off".
**Código (Claude):** `GameManager`, `SceneFlow` (cambiar de escena desde los botones).
**Escenas vacías a crear:** `Lobby`, `Floor`, `End` con un botón cada una para probar el flujo completo.

## Paso 2 — On/Off caminando y disparando

| Archivo (de `ArtSource/Sprites`) | Carpeta en Assets | Cortar | Pivot |
|---|---|---|---|
| `Weapons/weapons.png` | `Art/Sprites/Weapons` | 15 × 1 | Left |
| `UI/tokens.png` (balas de código) | `Art/Sprites/UI` | 63 × 1 | Center |
| `FX/muzzle.png` | `Art/Sprites/FX` | 3 × 1 | Left |
| `Tiles/floor.png`, `Tiles/wall.png` | `Art/Sprites/Tiles` | floor 6 × 1, wall — | Center |
| `Audio/Sfx/shoot_on.wav`, `shoot_off.wav` | `Audio/Sfx` | — | — |

**En el editor:** prefab `Player` (3 SpriteRenderer hijos + sombra), animaciones caminar/quieto, sala de prueba con Tilemap.
**Código:** `PlayerController`, `PlayerWeapons`, `Projectile`, acciones de Input System.

## Paso 3 — Combate básico

| Archivo | Carpeta | Cortar | Pivot |
|---|---|---|---|
| `Enemies/bug.png`, `glitch.png`, `leak.png` | `Art/Sprites/Enemies` | 2 × 1 | Bottom |
| `Tiles/door.png`, `Tiles/gate.png` | `Art/Sprites/Tiles` | 2 × 1 | Center |
| `FX/dust.png`, `FX/tears.png`, `FX/explosion.png` | `Art/Sprites/FX` | 3×1, 3×1, 5×1 | Center |
| `Items/hearts.png` | `Art/Sprites/Items` | 3 × 1 | Center |
| Sfx: `hit`, `enemy_die`, `enemy_shot`, `player_hurt`, `player_die`, `door_close`, `door_open`, `tear_pop` | `Audio/Sfx` | — | — |

**En el editor:** prefabs de enemigos, sala de combate con puertas, HUD de corazones.
**Código:** `Health`, `Enemy`, `WaveSpawner`, `Room`, `Door`.

## Paso 4 — Piso 1 completo

| Archivo | Carpeta | Cortar | Pivot |
|---|---|---|---|
| `Tiles/tiles.png`, `Tiles/rock.png` | `Art/Sprites/Tiles` | tiles 8 × 4, rock — | Center |
| `Enemies/bug_f1`, `glitch_f1`, `leak_f1`, `copypaste` | `Art/Sprites/Enemies` | 2 × 1 | Bottom |
| `Bosses/boss_spaghetti.png`, `boss_div.png` | `Art/Sprites/Bosses` | 2 × 1 | Bottom |
| `Props/chest_supply`, `chest_weapon`, `crate`, `merchant`, `pedestal`, `coffee_machine` | `Art/Sprites/Props` | merchant 2×1, resto — | Bottom |
| `Items/items`, `items2`, `coin`, `coffee`, `pizza`, `drink` | `Art/Sprites/Items` | items 5×1, items2 5×1, coin 4×1, coffee 6×1 | Center |
| `FX/fire.png`, `FX/shield.png` | `Art/Sprites/FX` | 3×1, 2×1 | Center |
| Sfx: `coin`, `buy`, `denied`, `heal`, `pickup_heart`, `pickup_item`, `explosion`, `boss_intro`, `boss_phase`, `boss_die`, `shield_up`, `shield_break`, `teleport`, `lock` | `Audio/Sfx` | — | — |
| Music: `dungeon.wav`, `boss.wav` | `Audio/Music` | — | — |

**En el editor:** 3–4 salas de combate, sala inicio, cofre, tienda y jefe como prefabs; HUD y minimapa.
**Código:** `FloorBuilder`, `Chest`, `Shop`, `Crate`, `CoffeeMachine`, `Blackout` (apagón), jefes Spaghetti y `<div>`.

## Paso 5 — Lobby del club

| Archivo | Carpeta | Cortar | Pivot |
|---|---|---|---|
| `Props/fountain`, `sofa`, `plant`, `soda`, `coffee_bar`, `chair` | `Art/Sprites/Props` | — | Bottom |
| Estación Luis: `desk` (2×1), `arcade` | `Art/Sprites/Props` | | Bottom |
| Estación Sofi: `camera`, `ringlight`, `corkboard` | `Art/Sprites/Props` | — | Bottom |
| Estación Lemus: `rack` (3×1), `switch` (2×1), `router` (2×1) | `Art/Sprites/Props` | | Bottom |
| `Props/code_portal` (6×1), `portal` (2×1), `terminal` (2×1) | `Art/Sprites/Props` | | Bottom |
| `Characters/Staff/staff_script`, `staff_cyd`, `staff_ciber` | `Art/Sprites/Characters/Staff` | 4 × 1 | Bottom |
| `Characters/Staff/portraits.png` | igual | 8 × 1 | Center |
| `Characters/On|Off/*_script_*`, `*_cyd_*`, `*_ciber_*` | `Characters/On`, `/Off` | body 5×4, extra 4×1 | Bottom |
| `UI/class_icons.png` | `Art/Sprites/UI` | 6 × 1 | Center |
| `Fonts/pixel.png` + `pixel.fnt` | `Art/Fonts` | (se ve en ese paso) | — |
| Sfx: `menu_move`, `menu_select`, `pause`, `portal`; Music: `menu.wav` | `Audio/...` | — | — |

**Código:** `RoleData` + 4 habilidades (Deploy doble, Hot Reload, Post viral, Firewall total), terminal de dificultad, `SaveSystem`.

## Paso 6 — Pisos 2 y 3

| Archivo | Carpeta | Cortar | Pivot |
|---|---|---|---|
| `Enemies/*_f2`, `*_f3` (bug, glitch, leak, loop, nullptr, virus) | `Art/Sprites/Enemies` | 2 × 1 | Bottom |
| `Enemies/loop`, `nullptr`, `virus`, `ddos`, `coldstart`, `turret` | igual | 2 × 1 | Bottom |
| `Bosses/boss_kernel`, `boss_ransom` | `Art/Sprites/Bosses` | 2 × 1 | Bottom |
| `FX/cloud_puff`, `spark`, `slash` | `Art/Sprites/FX` | cloud 2×1, spark 3×1, slash 3×1 | Center |
| Sfx: `laser_charge`, `laser_fire`, `dash` | `Audio/Sfx` | — | — |

**Código:** `TrafficSpike`, `Turbulence`, `DDoS`, `ColdStart`, `KernelPanic`, `Ransomware`.

## Paso 7 — Pulido · Paso 8 — Builds

Lo que quede en `ArtSource` (música `victory`, `game_over`). Al terminar, `ArtSource/` debe quedar vacío o solo
con lo que se decidió no usar.

> Los tamaños de corte vienen del juego de Godot. Si un corte se ve mal (cuadros desfasados), probar
> *Grid By Cell Size* con el tamaño en píxeles: ancho del PNG ÷ columnas.
