using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Voxels.Common.DataModels;
using Voxels.TerrainGeneration.ECS.Components;

namespace Voxels.TerrainGeneration.ECS.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    struct BlockTypeJob : IJobChunk
    {
        [ReadOnly] public ArchetypeChunkComponentType<CoordinatesComponent> Coordinates;
        public ArchetypeChunkComponentType<BlockTypesComponent> BlockTypes;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<CoordinatesComponent> coordinatesArray = chunk.GetNativeArray(Coordinates);
            NativeArray<BlockTypesComponent> blockTypesArray = chunk.GetNativeArray(BlockTypes);

            for (int i = 0; i < chunk.Count; i++)
            {
                int3 coordinates = coordinatesArray[i].Coordinates;

                ReadonlyVector3Int heights = TerrainGenerator.CalculateHeights(TerrainGenerator.Seed, coordinates.x, coordinates.z);

                blockTypesArray[i] = new BlockTypesComponent()
                {
                    BlockType = TerrainGenerator.DetermineType(TerrainGenerator.Seed, coordinates.x, coordinates.y, coordinates.z, in heights)
                };
            }
        }
    }
}
