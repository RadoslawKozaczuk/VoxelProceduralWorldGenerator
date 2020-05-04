using System;
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
#pragma warning restore CS0649

        // Loading from a non-readonly static field is not supported by burst, everything need to be static
        //readonly static TerrainGenerator _terrainGenerator = new TerrainGenerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeOnWorldSizeChange() => TerrainGenerator.Initialize(_heightsShader);

        public static void CalculateBlockTypes()
        {
            switch (GlobalVariables.Settings.ComputingAcceleration)
            {
                case ComputingAccelerationMethod.None:
                {
                    CalculateBlockTypes_PureCSParallel();
                    break;
                }
                case ComputingAccelerationMethod.PureCSParallelisation:
                {
                    CalculateBlockTypes_PureCSParallel();
                    break;
                }
                case ComputingAccelerationMethod.UnityJobSystem:
                {
                    ReadonlyVector3Int[] heights = TerrainGenerator.CalculateHeightsJobSystem();
                    BlockType[] types = TerrainGenerator.CalculateBlockTypes(heights);
                    DeflattenizeOutput(ref types);
                    break;
                }
                case ComputingAccelerationMethod.UnityJobSystemPlusStaticArray:
                {
                    throw new NotImplementedException();

                    // new way of calculating unfortunately slower
                    // it uses job system but calculates entire columns 
                    // this approach needs static array allocation
                    //BlockTypeColumn[] types = _terrainGenerator.CalculateBlockColumn();
                    break;
                }
                case ComputingAccelerationMethod.ComputeShader:
                {
                    throw new NotImplementedException();

                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadonlyVector3Int CalculateHeights(int seed, int x, int z) => TerrainGenerator.CalculateHeights(seed, x, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWater() => TerrainGenerator.AddWater();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddTreesParallel() => TerrainGenerator.AddTreesParallel();

        // The managed function is not supported by burst, everything need to be static
        //public static int GenerateBedrockHeight(float x, float z) => _terrainGenerator.GenerateBedrockHeight(x, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockType DetermineType(int seed, int worldX, int worldY, int worldZ, in ReadonlyVector3Int heights)
            => TerrainGenerator.DetermineType(seed, worldX, worldY, worldZ, in heights);

        static void CalculateBlockTypes_PureCSParallel()
        {
            var queue = new MultiThreadTaskQueue();

            int x, z;
            for (x = 0; x < TerrainGenerator.TotalBlockNumberX; x++)
                for (z = 0; z < TerrainGenerator.TotalBlockNumberZ; z++)
                    queue.ScheduleTask(TerrainGenerator.CalculateBlockTypesForColumnParallel, GlobalVariables.Settings.SeedValue, x, z);

            queue.RunAllInParallel();
        }

        static void DeflattenizeOutput(ref BlockType[] types)
        {
            for (int x = 0; x < TerrainGenerator.TotalBlockNumberX; x++)
                for (int y = 0; y < TerrainGenerator.TotalBlockNumberY; y++)
                    for (int z = 0; z < TerrainGenerator.TotalBlockNumberZ; z++)
                    {
                        BlockType type = types[Utils.IndexFlattenizer3D(x, y, z, TerrainGenerator.TotalBlockNumberX, TerrainGenerator.TotalBlockNumberY)];

                        ref BlockData b = ref GlobalVariables.Blocks[x, y, z];
                        b.Type = type;
                        b.Hp = LookupTables.BlockHealthMax[(int)type];
                    }
        }
    }
}
