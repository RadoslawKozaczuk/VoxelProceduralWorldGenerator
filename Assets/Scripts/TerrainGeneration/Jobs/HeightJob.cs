using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Voxels.Common;
using Voxels.Common.DataModels;

namespace Voxels.TerrainGeneration.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    struct HeightJob : IJobParallelFor
    {
        // input
        [ReadOnly]
        internal int TotalBlockNumberX;
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [ReadOnly]
        internal int Seed;
#pragma warning restore CS0649

        // output
        internal NativeArray<ReadonlyVector3Int> Result;

        public void Execute(int i)
        {
            Utils.IndexDeflattenizer2D(i, TotalBlockNumberX, out int x, out int z);
            Result[i] = TerrainGenerator.CalculateHeights(Seed, x, z);
        }
    }
}
