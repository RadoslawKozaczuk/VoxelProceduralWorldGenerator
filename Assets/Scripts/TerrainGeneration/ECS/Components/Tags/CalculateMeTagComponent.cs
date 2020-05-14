using Unity.Entities;

namespace Voxels.TerrainGeneration.ECS.Components.Tags
{
    /// <summary>
    /// Indicates that this entity is ready to be processed.
    /// </summary>
    struct CalculateMeTagComponent : ISharedComponentData
    {
        // empty on purpose
    }
}
