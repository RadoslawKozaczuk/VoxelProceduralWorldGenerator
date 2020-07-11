using System.Runtime.CompilerServices;
using UnityEngine;
using Voxels.Common;
using Voxels.Common.DataModels;
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
        [SerializeField] Texture2D _perlinNoise;
        [SerializeField] int _textureResolution;
#pragma warning restore CS0649

        // Loading from a non-readonly static field is not supported by burst, everything need to be static
        //readonly static TerrainGenerator _terrainGenerator = new TerrainGenerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeOnWorldSizeChange() => TerrainGenerator.Initialize(_heightsShader, _perlinNoise, _textureResolution);

        public static void CalculateBlockTypes()
        {
            switch (GlobalVariables.Settings.AccelerationMethod)
            {
                case ComputingAccelerationMethod.None:
                {
                    TerrainGenerator.CalculateBlockTypes_SingleThread();
                    break;
                }
                case ComputingAccelerationMethod.PureCSParallelisation:
                {
                    TerrainGenerator.CalculateBlockTypes_PureCSParallel();
                    break;
                }
                case ComputingAccelerationMethod.UnityJobSystem:
                {
                    ReadonlyVector3Int[] heights = TerrainGenerator.CalculateHeights_JobSystem_NoiseSampler();
                    BlockType[] types = TerrainGenerator.CalculateBlockTypes_NoiseSampler(heights);
                    TerrainGenerator.DeflattenizeOutput(ref types);
                    break;
                }
                case ComputingAccelerationMethod.UnityJobSystemBurstCompiler:
                {
                    ReadonlyVector3Int[] heights = TerrainGenerator.CalculateHeights_JobSystem_NoiseFunction();
                    BlockType[] types = TerrainGenerator.CalculateBlockTypes_NoiseFunction(heights);
                    TerrainGenerator.DeflattenizeOutput(ref types);
                    break;
                }
                case ComputingAccelerationMethod.ComputeShader:
                {
                    TerrainGenerator.CalculateBlockTypes_ComputeShader();
                    break;
                }
                case ComputingAccelerationMethod.ECS:
                {
                    TerrainGenerator.CreateEntities();
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadonlyVector3Int CalculateHeights(int seed, int x, int z) => TerrainGenerator.CalculateHeights_NoiseSampler(seed, x, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWater() => TerrainGenerator.AddWater();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTrees()
        {
            if (GlobalVariables.Settings.AccelerationMethod == ComputingAccelerationMethod.None)
                TerrainGenerator.AddTrees_SingleThread();
            else
                TerrainGenerator.AddTrees_Parallel();
        }

        // The managed function is not supported by burst, everything need to be static
        //public static int GenerateBedrockHeight(float x, float z) => _terrainGenerator.GenerateBedrockHeight(x, z);
    }
}
