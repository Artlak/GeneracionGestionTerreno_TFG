# GeneracionDestruccionTerreno_TFG
Trabajo de fin de grado Videojuegos

El objetivo del proyecto es la generación mediante el uso de las mallas de vertices en Unity de un mundo de gran tamaño que se encuentre cargado en todo momento mediante un sistema de carga de LoDs adaptativo.

Tutorial de parámetros y control de personaje y mundo:

- Tamaño del lado del mapa: Cuanto va a ser el lado del mapa. Mapa = lado * lado
- Densidad: Cantidad de vertices extra entre los básicos. La longitud es la misma, solo cambia la cantidad por metro cuadrado.
- Biomas extra: Parches de terrenos aleatorios que pueden haber por el mapa.
- Radio de alto detalle: Numero de chunks alrrededor del jugador que se encuentran a máxima calidad.
- NIvel de LoD máximo permitido: Cantidad de veces que se pueden fusionar unos chunks con otros para reducir el detalle.
- Altura máxima: Altura máxima que puede alcanzar el terreno de juego.
- Semilla: Valor que define los valores aleatorios generados posteriormente.
- Movimiento del jugador: Saltos infinitos para poder ver el mapa desde arriba, teclas wasd.
- Para salir al menú pulsa Esc durante el tiempo de juego.
