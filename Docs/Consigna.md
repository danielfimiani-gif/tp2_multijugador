# Consigna

## Ejercicio 1

Teniendo integrado el SDK de Fusion, crear una escena en donde se permita crear lobbies personalizados con un nombre para la sala, teniendo una UI que permita:

- Crear una sala nueva (antes de crear, permitir configurar el nombre de la sala).
- Unirse a una sala existente (mostrar un listado de salas ya creadas, cada una con su nombre, y un botón para unirse a esa sala).

Una vez dentro de la sala, instanciar un cubo que represente al jugador, que cuente con un `NetworkBehaviour` que le permita moverse con WASD por un plano.

## Ejercicio 2

Teniendo una mecanismo de gestión de lobbies, hacer que — cuando el máximo de jugadores permitidos se conecten — comience un juego en donde cada jugador tenga que correr una carrera desde un punto A a un punto B. El primer jugador en llegar debe ganar. El juego tiene que contar con las siguientes características:

- Cada jugador debe ser representado por un modelo humanoide animado, cuyas animaciones se encuentran sincronizadas entre clientes.
- Deben haber obstáculos movibles que están sincronizados entre clientes, que bloquean el paso de los jugadores.
- Tiene que existir un contador de tiempo de tipo "3, 2, 1…" antes de arrancar la carrera.
- Tiene que haber un power up que al ser recolectado aumente la velocidad del jugador durante cierto tiempo.
