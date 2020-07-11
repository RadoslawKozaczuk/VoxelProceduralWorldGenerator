# VoxelProceduralWorldGenerator
Contains easy-to-read, clean, and highly optimized code, written with a focus on data (data-oriented approach), that is able to create Minecraft-like worlds.

It can generate ~2.4 million cubes per second (on i7-7700HQ and GeForce GTX 1050 Ti (laptop)) and that includes creating trees, different types of terrain and resources, as well as various caves with realistically spread water that leaves upper parts of the caves filled with air.

Important parts:
- parallelisation (Unity JobSystem with and without Burst, pure C# threads, GPU (compute shaders), and DOTS (as a proof of concept for now)
- procedural world generation with the usage of the Perlin Noise 3D function (analytical function or precalculated texture, first is slower, second less precise)
- basic shader for water
- object and scene management, save and load system
- destructible ground
- trees!

6 different computing methods implemented:
- Single thread (no parallelisation, uses noise texture for better computing times)
- Pure C# parallelisation (uses noise texture for better computing times)
- Unity Job System (better than above, uses noise texture for better computing times)
- Unity Job System with Burst compiler (it has to use the analytical function as managed objects are not allowed in Burst)
- Compute Shader (it has to use noise texture as there is no Perlin noise implementation on the GPU side, currently the fastest method resulting in spectacular 2.4 mln blocks per second)
- DOTS/ECS (relatively slow - I will try to utilise this technology more optimally in the future but for now at least it can serve as a proof of concept)

![Gameplay demo](demo/demo.gif)

![Generated World](https://i.imgur.com/R1HfNmB.jpg)

Kind Regards

Radek
<br/>
<br/>
Acknowledgements:

Apollo - for Burst compiler introduction plus other minor but important changes
<br/>
