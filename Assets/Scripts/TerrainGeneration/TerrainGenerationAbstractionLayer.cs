using System.Runtime.CompilerServices;
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

        // Loading from a non-readonly static field is not supported by burst, everything need to be static
        //readonly static TerrainGenerator _terrainGenerator = new TerrainGenerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeOnWorldSizeChange() => TerrainGenerator.Initialize(_heightsShader);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3[] CalculateHeightsJobSystem() => TerrainGenerator.CalculateHeightsJobSystem();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockType[] CalculateBlockTypes(int3[] heights) => TerrainGenerator.CalculateBlockTypes(heights);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalculateBlockTypesParallel() => TerrainGenerator.CalculateBlockTypesParallel();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWater() => TerrainGenerator.AddWater();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTreesParallel() => TerrainGenerator.AddTreesParallel();

        // The managed function is not supported by burst, everything need to be static
        //public static int GenerateBedrockHeight(float x, float z) => _terrainGenerator.GenerateBedrockHeight(x, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GenerateBedrockHeight(int seed, float x, float z) => TerrainGenerator.GenerateBedrockHeight(seed, x, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GenerateStoneHeight(int seed, float x, float z) => TerrainGenerator.GenerateStoneHeight(seed, x, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GenerateDirtHeight(int seed, float x, float z) => TerrainGenerator.GenerateDirtHeight(seed, x, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockType DetermineType(int seed, int worldX, int worldY, int worldZ, int3 heights) 
            => TerrainGenerator.DetermineType(seed, worldX, worldY, worldZ, heights);
    }
}
