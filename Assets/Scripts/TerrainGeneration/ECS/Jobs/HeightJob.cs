using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Voxels.Common.DataModels;
using Voxels.TerrainGeneration.ECS.Components;

namespace Voxels.TerrainGeneration.ECS.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    struct HeightJob : IJobChunk
    {
        [ReadOnly] public ArchetypeChunkComponentType<WorldConstants> WorldConstants;
        [ReadOnly] public ArchetypeChunkComponentType<CoordinatesComponent> Coordinates;

        public ArchetypeChunkComponentType<BlockTypesComponent> BlockTypes;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<CoordinatesComponent> coordinatesArray = chunk.GetNativeArray(Coordinates);
            NativeArray<WorldConstants> worldConstArray = chunk.GetNativeArray(WorldConstants);
            NativeArray<BlockTypesComponent> blockTypesArray = chunk.GetNativeArray(BlockTypes);

            for (int i = 0; i < chunk.Count; i++)
            {
                int3 coordinates = coordinatesArray[i].Coordinates;
                WorldConstants worldConstants = worldConstArray[i];

                // calculate heights
                ReadonlyVector3Int heights = TerrainGenerator.CalculateHeights(worldConstants.Seed, coordinates.x, coordinates.z);

                blockTypesArray[i] = new BlockTypesComponent()
                {
                    BlockType = TerrainGenerator.DetermineType(worldConstants.Seed, coordinates.x, coordinates.y, coordinates.z, in heights)
                };
            }
        }
    }
}
