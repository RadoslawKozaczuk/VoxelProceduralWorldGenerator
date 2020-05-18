using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Voxels.Common;
using Voxels.Common.DataModels;

namespace Voxels.TerrainGeneration.UnityJobSystem.Jobs
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
        /// Since the <see cref="Types"/> array is not pre-initialized air will be effectively represented by garbage data.
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
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [ReadOnly]
        internal int Seed;
#pragma warning restore CS0649

        internal NativeArray<BlockTypeColumn> Result;

        public void Execute(int i)
        {
            Utils.IndexDeflattenizer2D(i, TotalBlockNumberX, out int x, out int z);

            ReadonlyVector3Int heights = TerrainGenerator.CalculateHeights(Seed, x, z);

            int max = heights.X; // max could be passed to the loop below but it requires air to be default type
            if (heights.Y > max)
                max = heights.Y;
            if (heights.Z > max)
                max = heights.Z;

            var blockTypes = new BlockTypeColumn(max);

            unsafe
            {
                // heights are inclusive
                for (int y = 0; y <= max; y++)
                    blockTypes.Types[y] = (byte)TerrainGenerator.DetermineType(Seed, x, y, z, in heights);
            }

            Result[i] = blockTypes;
        }
    }
}
