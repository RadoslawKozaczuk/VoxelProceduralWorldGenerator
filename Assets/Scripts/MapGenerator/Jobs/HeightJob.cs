using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Voxels.Common;

namespace Voxels.MapGenerator.Jobs
{
	[BurstCompile(CompileSynchronously = true)]
	struct HeightJob : IJobParallelFor
	{
		[ReadOnly]
		internal int TotalBlockNumberX;
		[ReadOnly]
		internal float SeedValue;

		internal NativeArray<int3> Result;

		public void Execute(int i)
		{
			Utils.IndexDeflattenizer2D(i, TotalBlockNumberX, out int x, out int z);

			Result[i] = new int3()
			{
				x = TerrainGenerator.GenerateBedrockHeight(SeedValue, x, z),
				y = TerrainGenerator.GenerateStoneHeight(SeedValue, x, z),
				z = TerrainGenerator.GenerateDirtHeight(SeedValue, x, z)
			};
		}
	}
}
