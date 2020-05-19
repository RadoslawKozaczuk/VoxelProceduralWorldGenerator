using Unity.Entities;

namespace Voxels.TerrainGeneration.ECS.Components
{
    public struct SeedComponent : IComponentData
    {
        public int Seed;

        public SeedComponent(int seed)
        {
            Seed = seed;
        }
    }
}