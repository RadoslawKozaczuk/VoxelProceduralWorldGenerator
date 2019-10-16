using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

namespace Assets.Scripts.World
{
	[BurstCompile(CompileSynchronously = true)]
	struct HeightJob : IJobParallelFor
	{
		[ReadOnly]
		public int TotalBlockNumberX;
		[ReadOnly]
		public float SeedValue;

		public NativeArray<int3> Result;

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
