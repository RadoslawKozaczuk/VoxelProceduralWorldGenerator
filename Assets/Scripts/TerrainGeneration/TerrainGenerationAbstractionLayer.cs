using Unity.Mathematics;
using UnityEngine;
using Voxels.Common;
using Voxels.Common.Interfaces;

namespace Voxels.TerrainGeneration
{
    /// <summary>
    /// Abstraction layer allows us to encapsulate the assembly's logic.
    /// Thanks to that we can, for example, hide the constructor and control the initialization from here.
    /// Which allows us to set internal parameters to readonly and control the re/initialization by reconstructing the object
    /// rather than by calling an arbitrate initialize method.
    /// Also this is the place when we can set the parameters etc.
    /// </summary>
    [DisallowMultipleComponent]
    public class TerrainGenerationAbstractionLayer : MonoBehaviour, INeedInitializeOnWorldSizeChange
    {
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [SerializeField] ComputeShader _heightsShader;
#pragma warning restore CS0649

        static TerrainGenerator _terrainGenerator;

        public void InitializeOnWorldSizeChange() => _terrainGenerator = new TerrainGenerator(_heightsShader);

        public static int3[] CalculateHeightsJobSystem() => _terrainGenerator.CalculateHeightsJobSystem();

        public static BlockType[] CalculateBlockTypes(int3[] heights) => _terrainGenerator.CalculateBlockTypes(heights);

        public static void CalculateBlockTypesParallel() => _terrainGenerator.CalculateBlockTypesParallel();

        public static void AddWater() => _terrainGenerator.AddWater();

        public static void AddTreesParallel() => _terrainGenerator.AddTreesParallel();

        public static int GenerateBedrockHeight(float x, float z) => _terrainGenerator.GenerateBedrockHeight(x, z);

        public static int GenerateStoneHeight(float x, float z) => _terrainGenerator.GenerateStoneHeight(x, z);

        public static int GenerateDirtHeight(float x, float z) => _terrainGenerator.GenerateDirtHeight(x, z);

        public static BlockType DetermineType(int worldX, int worldY, int worldZ, int3 heights) 
            => _terrainGenerator.DetermineType(worldX, worldY, worldZ, heights);
    }
}
