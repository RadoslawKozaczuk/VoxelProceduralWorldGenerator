namespace Voxels.Common.DataModels
{
	public struct GameSettings
	{
		public TreeProbability TreeProbability;
		public int WorldSizeX, WorldSizeZ, SeedValue, WaterLevel;
		public bool IsWater;
		public ComputingAcceleration ComputingAcceleration;
	}
}
