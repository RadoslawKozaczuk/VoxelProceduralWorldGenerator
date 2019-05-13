using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Scripts.World
{
	struct HeightJob : IJobParallelFor
	{
		[ReadOnly]
		public int TotalBlockNumberX;

		public NativeArray<int3> Result;

		public void Execute(int i)
		{
			Utils.IndexDeflattenizer2D(i, TotalBlockNumberX, out int x, out int z);

			Result[i] = new int3()
			{
				x = TerrainGenerator.GenerateBedrockHeight(x, z),
				y = TerrainGenerator.GenerateStoneHeight(x, z),
				z = TerrainGenerator.GenerateDirtHeight(x, z)
			};
		}
	}
}
