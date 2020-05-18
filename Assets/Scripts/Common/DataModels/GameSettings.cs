namespace Voxels.Common.DataModels
{
    public struct GameSettings
    {
        /// <summary>
        /// Width of the world measured in chunks.
        /// </summary>
        public int WorldSizeX;

        /// <summary>
        /// Depth of the world measured in chunks.
        /// </summary>
        public int WorldSizeZ;
        
        public int SeedValue, WaterLevel;
        public bool IsWater;

        public TreeProbability TreeProbability;
        public ComputingAccelerationMethod AccelerationMethod;
    }
}
