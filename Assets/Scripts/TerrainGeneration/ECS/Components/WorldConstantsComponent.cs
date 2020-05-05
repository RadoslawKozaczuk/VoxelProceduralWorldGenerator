using Unity.Entities;

namespace Voxels.TerrainGeneration.ECS.Components
{
    public readonly struct WorldConstants : IComponentData
    {
        public readonly int Seed;

        public WorldConstants(int seed)
        {
            Seed = seed;
        }
    }
}
