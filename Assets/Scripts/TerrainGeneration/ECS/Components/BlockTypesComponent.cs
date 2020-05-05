using Unity.Entities;
using Voxels.Common;

namespace Voxels.TerrainGeneration.ECS.Components
{
    struct BlockTypesComponent : IComponentData
    {
        public BlockType BlockType;
    }
}
