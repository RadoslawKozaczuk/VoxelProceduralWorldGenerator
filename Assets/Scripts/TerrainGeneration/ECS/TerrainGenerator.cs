using Unity.Entities;
using Unity.Mathematics;
using Voxels.Common;
using Voxels.TerrainGeneration.ECS.Components;

namespace Voxels.TerrainGeneration
{
    internal static partial class TerrainGenerator
    {
        internal static void CreateEntities()
        {
            // we need entity manager in order to create entities
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // create an entity archetype (a blueprint), that will later be used in instantiation and querying
            EntityArchetype entityArchetype = entityManager.CreateArchetype(
                typeof(BlockTypesComponent),
                typeof(CoordinatesComponent));

            for (int x = 0; x < GlobalVariables.Settings.WorldSizeX * Constants.CHUNK_SIZE; x++)
                for (int y = 0; y < Constants.WORLD_SIZE_Y * Constants.CHUNK_SIZE; y++)
                    for (int z = 0; z < GlobalVariables.Settings.WorldSizeZ * Constants.CHUNK_SIZE; z++)
                    {
                        Entity entity = entityManager.CreateEntity(entityArchetype);
                        entityManager.SetComponentData(entity, new CoordinatesComponent(new int3(x, y, z)));
                    }
        }

        /// <summary>
        /// Adds <see cref="Components.Tags.CalculateMeTagComponent"/> to all entities.
        /// </summary>
        internal static void StartCalculation()
        {
            // we need entity manager in order to create entities
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // create an entity archetype (a blueprint), that will later be used in instantiation and querying
            EntityArchetype entityArchetype = entityManager.CreateArchetype(
                typeof(BlockTypesComponent),
                typeof(CoordinatesComponent));

            for (int x = 0; x < GlobalVariables.Settings.WorldSizeX * Constants.CHUNK_SIZE; x++)
                for (int y = 0; y < Constants.WORLD_SIZE_Y * Constants.CHUNK_SIZE; y++)
                    for (int z = 0; z < GlobalVariables.Settings.WorldSizeZ * Constants.CHUNK_SIZE; z++)
                    {
                        Entity entity = entityManager.CreateEntity(entityArchetype);
                        entityManager.SetComponentData(entity, new CoordinatesComponent(new int3(x, y, z)));
                    }
        }
    }
}
