using Unity.Entities;

namespace Voxels.TerrainGeneration.ECS.Components.Tags
{
    /// <summary>
    /// Indicates that this entity is no longer needed and should be deleted.
    /// </summary>
    struct DeleteMeTagComponent : ISharedComponentData
    {
        // empty on purpose
    }
}
