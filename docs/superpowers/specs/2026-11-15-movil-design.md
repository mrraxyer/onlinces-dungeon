# OnLinces: Debug Dungeon en teléfono — diseño corto

Fecha: 2026-11-15 · Estado: borrador para aprobación

## Intención

Que el juego se pueda jugar completo en un teléfono Android (Title → Lobby → pisos → End) solo con la pantalla
táctil, sin estorbar el trabajo del paso 5 (Lobby) que hace otra sesión en la misma carpeta y rama.

## Decisión del usuario

- **Dos palancas:** la izquierda mueve; la derecha apunta y dispara sola mientras se empuja.

## Enfoques considerados

| Enfoque | Qué cambia | Por qué sí / no |
|---|---|---|
| **Dos palancas (elegido)** | Palanca derecha = apuntar + disparo automático | Conserva la puntería manual que pide el jefe `<div>` |
| Auto-apuntado | Un botón dispara al enemigo más cercano | Más fácil, pero no deja elegir hacia dónde empujar al `<div>` |
| Tocar para disparar | Tocar la pantalla dispara hacia ese punto | El dedo tapa la acción y cansa en peleas largas |

## Decisiones de Claude (revisables)

- **Las palancas son un gamepad virtual** (`OnScreenStick` / `OnScreenButton` del Input System). El juego ya
  entiende gamepad, así que el código del jugador casi no cambia.
- **Prefab `Assets/Resources/TouchControls.prefab`**, visible y editable en el editor (posición, tamaño, sprites).
  Un script pequeño lo crea solo al arrancar y lo conserva entre escenas; **no se toca ninguna escena**.
- **Solo aparece en pantalla táctil** (Android/iOS). En PC no cambia nada. Un interruptor en el prefab permite
  forzarlo en el editor para probar con el ratón.
- **Solo se ve donde hay jugador** (Lobby y pisos). Title y End ya funcionan tocando sus botones.
- **Palancas flotantes:** la palanca aparece donde pongas el dedo, mitad izquierda = mover, mitad derecha = apuntar.
- **Botones** (lado derecho, arriba de la palanca): Interactuar (solo visible cuando hay algo cerca),
  Cambiar arma, Habilidad (se conecta cuando el paso 5 la tenga), Pausa (arriba a la derecha).
- **Pausa mínima** dentro del mismo prefab: Continuar / Salir al título. También pausa sola si sales de la app.
- **El aviso "E: ..."** pasa a ser solo el texto ("Abrir cofre") cuando se juega con táctil.
- **Pantalla:** horizontal fija (ambos lados), 60 fps, y los controles respetan el notch (área segura).
- **Arte de los controles:** círculos y botones simples semitransparentes con la fuente pixel; se pueden cambiar
  por sprites propios después sin tocar código.

## Archivos

- Nuevos: `Assets/Scripts/Mobile/` (`TouchControls`, `FloatingStick`, `SafeArea`, `MobileSetup`),
  `Assets/Resources/TouchControls.prefab`.
- Cambios pequeños a existentes: `PlayerController` (disparar al empujar la palanca derecha),
  `PlayerInteractor` (texto del aviso, y avisar si hay algo con qué interactuar). Se avisan a la otra sesión antes.

## A mano en el editor (con guía)

Player Settings de Android: orientación horizontal, nombre de paquete (hoy `com.DefaultCompany.2D-URP`), ícono.
No se editan desde fuera porque el editor está abierto y los sobrescribe.

## Fuera de alcance

iOS, publicar en Play Store, vibración, rehacer el HUD existente. Si el HUD queda bajo el notch o muy chico en
pantallas alargadas, se ajusta en las escenas después del paso 5, para no chocar con la otra sesión.

## Anti-objetivos

- Que cambie algo al jugar en PC.
- Controles que tapen al personaje o las balas enemigas.
- Tocar escenas o prefabs que la otra sesión está editando.

## Cómo se verifica

Compilación sin errores en el editor, prueba en Play con el interruptor de forzar táctil, y prueba final tuya en
el teléfono con un build de Android (yo no puedo tocar el teléfono).
