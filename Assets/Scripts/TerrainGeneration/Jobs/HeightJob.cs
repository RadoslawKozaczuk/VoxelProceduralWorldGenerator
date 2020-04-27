using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Voxels.Common;

namespace Voxels.TerrainGeneration.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    struct HeightJob : IJobParallelFor
    {
        [ReadOnly]
        internal int TotalBlockNumberX;

        internal NativeArray<int3> Result;

        public void Execute(int i)
        {
            Utils.IndexDeflattenizer2D(i, TotalBlockNumberX, out int x, out int z);

            Result[i] = new int3()
            {
                x = TerrainGenerationAbstractionLayer.GenerateBedrockHeight(x, z),
                y = TerrainGenerationAbstractionLayer.GenerateStoneHeight(x, z),
                z = TerrainGenerationAbstractionLayer.GenerateDirtHeight(x, z)
            };
        }
    }
}
