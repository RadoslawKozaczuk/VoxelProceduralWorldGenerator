using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Voxels.Common;
using Voxels.Common.DataModels;
using Voxels.Common.Interfaces;
using Voxels.TerrainGeneration.UnityJobSystem.Jobs;

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
                    ReadonlyVector3Int[] heights = TerrainGenerator.CalculateHeightsJobSystem();
                    BlockType[] types = TerrainGenerator.CalculateBlockTypes(heights);
                    TerrainGenerator.DeflattenizeOutput(ref types);
                    break;
                }
                case ComputingAccelerationMethod.UnityJobSystemPlusStaticArray:
                {
                    throw new NotImplementedException();

                    // new way of calculating unfortunately slower
                    // it uses job system but calculates entire columns 
                    // this approach needs static array allocation
                    BlockTypeColumn[] types = TerrainGenerator.CalculateBlockColumn();
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
        public static ReadonlyVector3Int CalculateHeights(int seed, int x, int z) => TerrainGenerator.CalculateHeights(seed, x, z);

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
