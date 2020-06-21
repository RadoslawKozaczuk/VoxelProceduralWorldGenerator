using Unity.Entities;
using Unity.Jobs;
using Voxels.Common;
using Voxels.TerrainGeneration.ECS.Components;
using Voxels.TerrainGeneration.ECS.Jobs;

namespace Voxels.TerrainGeneration.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    class BlockTypeCalculationSystem : SystemBase
    {
        bool flag = false;
        EntityQuery _query;

        protected override void OnCreate()
        {
            // query is like a SQL query, allows us to retrieve only these entities that we want
            _query = GetEntityQuery(typeof(BlockTypesComponent), typeof(CoordinatesComponent));
        }

        protected override void OnUpdate()
        {
            if (flag)
                return;

            var blockTypesType = GetArchetypeChunkComponentType<BlockTypesComponent>(); // read-write access
            var coordinateType = GetArchetypeChunkComponentType<CoordinatesComponent>(true); // true means it is read only
            var seedType = GetArchetypeChunkComponentType<SeedComponent>(true); // true means it is read only

            var job = new BlockTypeJob()
            {
                BlockTypes = blockTypesType,
                Coordinates = coordinateType,
                Seed = seedType
            };
   
            JobHandle jobHandle = job.Schedule(_query);
            jobHandle.Complete(); // wait until completed

            // all calculations are done in one frame
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // there is a bug here - throws ECS-related null reference exception
            Entities
                // allows us to modify or delete entities
                //.WithStructuralChanges()
                // this serves as a signature as well
                .ForEach((Entity entity, in CoordinatesComponent coordinates, in BlockTypesComponent blockTypes) =>
                {
                    // copy data to the main array
                    //TerrainGenerator.CreateBlock(
                    //    ref GlobalVariables.Blocks[coordinates.Coordinates.x, coordinates.Coordinates.y, coordinates.Coordinates.z],
                    //    blockTypes.BlockType);

                    // clean up and prevent further calculations
                    //entityManager.DestroyEntity(entity);
                }).Run(); // "Run" means run on the main thread

            flag = true;
        }
    }
}
