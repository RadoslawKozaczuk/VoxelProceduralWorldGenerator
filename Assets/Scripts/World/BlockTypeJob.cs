using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

namespace Assets.Scripts.World
{
	[BurstCompile(CompileSynchronously = true)]
	struct BlockTypeJob : IJobParallelFor
	{
		[ReadOnly]
		public int TotalBlockNumberX;
		[ReadOnly]
		public int TotalBlockNumberY;
		[ReadOnly]
		public int TotalBlockNumberZ;
		[ReadOnly]
		public NativeArray<int3> Heights;
		[ReadOnly]
		public float SeedValue;

		public NativeArray<BlockType> Result;

		public void Execute(int i)
		{
			Utils.IndexDeflattenizer3D(i, TotalBlockNumberX, TotalBlockNumberY, out int x, out int y, out int z);
			Result[i] = TerrainGenerator.DetermineType(SeedValue, x, y, z, Heights[Utils.IndexFlattenizer2D(x, z, TotalBlockNumberX)]);
		}
	}
}
