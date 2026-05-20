# Programación de Videojuegos Multijugador con Unity — Trabajo Práctico 2

La entrega deberá consistir en un link a un repositorio de GitHub en donde se encuentre alojado un proyecto de Unity. Si bien puede ser desarrollado en cualquier versión del editor que se prefiera, la entrega debe poder ser abierta desde Unity 6 (versión 6000.0.36f1) y seguir funcionando debidamente. Asimismo, debe evidenciarse que el proyecto fue elaborado usando versionado (debe tener varios commits, a contraposición de uno solo con la consigna completada).

## Ejercicio

El proyecto debe consistir en un juego de acción multijugador competitivo hecho con la librería Photon Fusion 2 para Unity. El género del juego queda a libre elección. No obstante, se recomienda que se trate de uno relativamente sencillo de implementar en términos de jugabilidad (por ejemplo, un first-person shooter). Asimismo, no hace falta contar con assets gráficos en particular; puede entregarse un prototipo con placeholders.

En líneas generales, el juego debe permitir buscar partida, creando una en caso de que no existan otras ya. La topología a usar sería la de host client. La partida puede desarrollarse en modalidad "todos contra todos" o "por equipos". La idea es que exista una manera de contabilizar puntos, y que exista un tiempo límite para jugar. Más características pueden ser agregadas dependiendo de la naturaleza del género de juego. Queda a libre elección de diseño la manera de establecer el matchmaking y/o creación de hubs de espera antes de iniciar la partida.

## Criterios de evaluación

Se dispone de un sistema de tiers, donde cada uno dispone de requisitos a cumplir para ser alcanzado. Contar con uno de los requisitos de un tier otorgará un punto. Con la finalidad de pasar de un tier a otro, es necesario cumplir con todos los requisitos indicados en él. Es decir, no se considerarán requisitos de un determinado tier si los pertenecientes al anterior no están todos logrados.

### Tier B: 4 (cuatro)

- La librería de Photon está correctamente integrada y usada en el proyecto.
- El videojuego permite a los clientes conectarse usando una topología de tipo host-client.
- El juego permite a los participantes competir por puntos, los cuales se sincronizan correctamente entre clientes.
- El juego dispone de un tiempo límite de juego una vez iniciada la partida.

### Tier A: 7 (siete)

- Se utilizan al menos tres propiedades con el atributo `[Networked]` para sincronizar estados específicos entre clientes.
- Se utilizan al menos tres RPCs (remote procedure calls) para informar sobre cambios específicos en la partida de forma confiable.
- Se utiliza al `NetworkCharacterController` para mover a los personajes.

### Tier S: 10 (diez)

- Se utiliza un `NetworkMecanimAnimator` para sincronizar animaciones.
- El juego cuenta con una interfaz para unirse a partidas, haciendo que la sesión de juego empiece solamente cuando se cumpla la cuota mínima de jugadores necesarios.
- El proyecto no presenta bugs bloqueantes recurrentes.
