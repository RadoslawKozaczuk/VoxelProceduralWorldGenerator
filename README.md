# VoxelProceduralWorldGenerator
Contains easy-to-read, clean, and highly optimized code written with a focus on data (data-oriented approach), that is able to create Minecraft-like worlds.

It can generate ~1.3 million cubes per second (on i7-7700HQ) and that includes creating trees, different types of terrain and resources, as well as various caves with realistically spread water that leaves upper parts of the caves filled with air.

Important parts:
- parallelisation (Unity JobSystem as well as pure C# threads)
- procedural world generation with the usage of the Perlin Noise 3D function
- basic shader for water (and to some extent also for terrain calculation)
- object and scene management, save and load system
- destructible ground
- trees!

![Gameplay demo](demo/demo.gif)

![Generated World](https://i.imgur.com/R1HfNmB.jpg)

Kind Regards

Radek
<br/>
<br/>
Acknowledgements:<br/>
Apollo - for Burst compiler introduction plus other minor but important changes
