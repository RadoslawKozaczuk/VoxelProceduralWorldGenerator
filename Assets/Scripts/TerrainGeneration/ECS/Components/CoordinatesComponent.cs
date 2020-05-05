using Unity.Entities;
using Unity.Mathematics;

namespace Voxels.TerrainGeneration.ECS.Components
{
    public readonly struct CoordinatesComponent : IComponentData
    {
        public readonly int3 Coordinates;

        public CoordinatesComponent(int3 coordinates)
        {
            Coordinates = coordinates;
        }
    }
}
