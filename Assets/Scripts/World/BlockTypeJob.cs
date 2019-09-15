using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.World
{
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

		public NativeArray<BlockType> Result;

		public void Execute(int i)
		{
			Utils.IndexDeflattenizer3D(i, TotalBlockNumberX, TotalBlockNumberY, out int x, out int y, out int z);
			Result[i] = TerrainGenerator.DetermineType(x, y, z, Heights[Utils.IndexFlattenizer2D(x, z, TotalBlockNumberX)]);
		}
	}
}
