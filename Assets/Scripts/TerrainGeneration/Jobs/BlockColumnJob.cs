using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Voxels.Common;

namespace Voxels.TerrainGeneration.Jobs
{
    // this isn't faster (in fact is 10 times slower) which is very surprising I think the problem may come from the fact that this needs to be copied
    // or maybe I made a mistake somewhere I am not 100% sure
    public unsafe struct BlockTypeColumn
    {
        // we need an array here but normally it is not possible to have an array in a struct 
        // as reference types are forbidden
        // therefore we have to use unsafe context and a static array
        public fixed byte Types[128];

        /// <summary>
        /// Up to where terrain is present. Everything above that is air.
        /// Since the Types array is not pre-initialized air will be effectively represented by garbage data.
        /// </summary>
        public readonly int TerrainLevel;

        public BlockTypeColumn(int terrainLevel)
        {
            TerrainLevel = terrainLevel;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct BlockColumnJob : IJobParallelFor
    {
        [ReadOnly]
        internal int TotalBlockNumberX;
        [ReadOnly]
        internal int TotalBlockNumberY;
        [ReadOnly]
        internal int TotalBlockNumberZ;
        [ReadOnly]
        internal int Seed;

        internal NativeArray<BlockTypeColumn> Result;

        public void Execute(int i)
        {
            Utils.IndexDeflattenizer2D(i, TotalBlockNumberX, out int x, out int z);

            int3 heights = new int3()
            {
                x = TerrainGenerationAbstractionLayer.GenerateBedrockHeight(Seed, x, z),
                y = TerrainGenerationAbstractionLayer.GenerateStoneHeight(Seed, x, z),
                z = TerrainGenerationAbstractionLayer.GenerateDirtHeight(Seed, x, z)
            };

            int max = heights.x; // max could be passed to the loop below but it requires air to be default type
            if (heights.y > max)
                max = heights.y;
            if (heights.z > max)
                max = heights.z;

            var blockTypes = new BlockTypeColumn(max);

            unsafe
            {
                // heights are inclusive
                for (int y = 0; y <= max; y++)
                    blockTypes.Types[y] = (byte)TerrainGenerationAbstractionLayer.DetermineType(Seed, x, y, z, heights);
            }

            Result[i] = blockTypes;
        }
    }
}
