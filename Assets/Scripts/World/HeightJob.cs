using Unity.Collections;
using Unity.Jobs;

namespace Assets.Scripts.World
{
	struct HeightJob : IJobParallelFor
	{
		[ReadOnly]
		public int TotalBlockNumberX;

		public NativeArray<HeightData> Result;

		public void Execute(int i)
		{
			Utils.IndexDeflattenizer2D(i, TotalBlockNumberX, out int x, out int z);

			Result[i] = new HeightData()
			{
				Bedrock = TerrainGenerator.GenerateBedrockHeight(x, z),
				Stone = TerrainGenerator.GenerateStoneHeight(x, z),
				Dirt = TerrainGenerator.GenerateDirtHeight(x, z)
			};
		}
	}
}
