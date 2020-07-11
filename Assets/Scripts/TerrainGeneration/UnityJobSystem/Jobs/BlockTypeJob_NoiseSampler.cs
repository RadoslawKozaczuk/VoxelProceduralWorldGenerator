using Unity.Collections;
using Unity.Jobs;
using Voxels.Common;
using Voxels.Common.DataModels;

namespace Voxels.TerrainGeneration.UnityJobSystem.Jobs
{
    struct BlockTypeJob_NoiseSampler : IJobParallelFor
    {
        // input
        [ReadOnly]
        internal int TotalBlockNumberX;
        [ReadOnly]
        internal int TotalBlockNumberY;
        [ReadOnly]
        internal int TotalBlockNumberZ;
        [ReadOnly]
        internal NativeArray<ReadonlyVector3Int> Heights;
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [ReadOnly]
        internal int Seed;
#pragma warning restore

        // output
        internal NativeArray<BlockType> Result;

        public void Execute(int i)
        {
            Utils.IndexDeflattenizer3D(i, TotalBlockNumberX, TotalBlockNumberY, out int x, out int y, out int z);
            Result[i] = TerrainGenerator.DetermineType_NoiseSampler(Seed, x, y, z, Heights[Utils.IndexFlattenizer2D(x, z, TotalBlockNumberX)]);
        }
    }
}
