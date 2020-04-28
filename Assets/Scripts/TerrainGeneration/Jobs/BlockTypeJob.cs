using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Voxels.Common;

namespace Voxels.TerrainGeneration.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    struct BlockTypeJob : IJobParallelFor
    {
        // input
        [ReadOnly]
        internal int TotalBlockNumberX;
        [ReadOnly]
        internal int TotalBlockNumberY;
        [ReadOnly]
        internal int TotalBlockNumberZ;
        [ReadOnly]
        internal NativeArray<int3> Heights;
        [ReadOnly]
        internal int Seed;

        // output
        internal NativeArray<BlockType> Result;

        public void Execute(int i)
        {
            Utils.IndexDeflattenizer3D(i, TotalBlockNumberX, TotalBlockNumberY, out int x, out int y, out int z);
            Result[i] = TerrainGenerationAbstractionLayer.DetermineType(Seed, x, y, z, Heights[Utils.IndexFlattenizer2D(x, z, TotalBlockNumberX)]);
        }
    }
}
