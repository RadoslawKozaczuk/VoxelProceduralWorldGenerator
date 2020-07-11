using Unity.Collections;
using Unity.Jobs;
using Voxels.Common;
using Voxels.Common.DataModels;
using Voxels.TerrainGeneration.UnityJobSystem.Jobs;

namespace Voxels.TerrainGeneration
{
    internal static partial class TerrainGenerator
    {
        /// <summary>
        /// Calculates global heights. This method uses Unity Job System.
        /// Each column described by its x and z values has three heights.
        /// One for Bedrock, Stone and Dirt. Heights determines up to where certain types appear.
        /// x is Bedrock, y is Stone and z is Dirt.
        /// </summary>
        internal static ReadonlyVector3Int[] CalculateHeights_JobSystem_NoiseSampler()
        {
            // output data
            var heights = new ReadonlyVector3Int[TotalBlockNumberX * TotalBlockNumberZ];

            var heightJob = new HeightJob_NoiseSampler()
            {
                // input
                TotalBlockNumberX = TotalBlockNumberX,

                // output
                Result = new NativeArray<ReadonlyVector3Int>(heights, Allocator.TempJob)
            };

            JobHandle heightJobHandle = heightJob.Schedule(TotalBlockNumberX * TotalBlockNumberZ, 8);
            heightJobHandle.Complete();
            heightJob.Result.CopyTo(heights);

            // cleanup
            heightJob.Result.Dispose();

            return heights;
        }

        /// <summary>
        /// Calculates global heights. This method uses Unity Job System.
        /// Each column described by its x and z values has three heights.
        /// One for Bedrock, Stone and Dirt. Heights determines up to where certain types appear.
        /// x is Bedrock, y is Stone and z is Dirt.
        /// </summary>
        internal static ReadonlyVector3Int[] CalculateHeights_JobSystem_NoiseFunction()
        {
            // output data
            var heights = new ReadonlyVector3Int[TotalBlockNumberX * TotalBlockNumberZ];

            var heightJob = new HeightJob_NoiseFunction()
            {
                // input
                TotalBlockNumberX = TotalBlockNumberX,

                // output
                Result = new NativeArray<ReadonlyVector3Int>(heights, Allocator.TempJob)
            };

            JobHandle heightJobHandle = heightJob.Schedule(TotalBlockNumberX * TotalBlockNumberZ, 8);
            heightJobHandle.Complete();
            heightJob.Result.CopyTo(heights);

            // cleanup
            heightJob.Result.Dispose();

            return heights;
        }

        internal static BlockType[] CalculateBlockTypes_NoiseSampler(ReadonlyVector3Int[] heights)
        {
            int inputSize = TotalBlockNumberX * TotalBlockNumberY * TotalBlockNumberZ;

            // output data
            var types = new BlockType[inputSize];

            var typeJob = new BlockTypeJob_NoiseSampler()
            {
                // input
                TotalBlockNumberX = TotalBlockNumberX,
                TotalBlockNumberY = TotalBlockNumberY,
                TotalBlockNumberZ = TotalBlockNumberZ,
                Heights = new NativeArray<ReadonlyVector3Int>(heights, Allocator.TempJob),

                // output
                Result = new NativeArray<BlockType>(types, Allocator.TempJob)
            };

            JobHandle typeJobHandle = typeJob.Schedule(inputSize, 8);
            typeJobHandle.Complete();
            typeJob.Result.CopyTo(types);

            // cleanup
            typeJob.Result.Dispose();
            typeJob.Heights.Dispose();

            return types;
        }

        internal static BlockType[] CalculateBlockTypes_NoiseFunction(ReadonlyVector3Int[] heights)
        {
            int inputSize = TotalBlockNumberX * TotalBlockNumberY * TotalBlockNumberZ;

            // output data
            var types = new BlockType[inputSize];

            var typeJob = new BlockTypeJob_NoiseFunction()
            {
                // input
                TotalBlockNumberX = TotalBlockNumberX,
                TotalBlockNumberY = TotalBlockNumberY,
                TotalBlockNumberZ = TotalBlockNumberZ,
                Heights = new NativeArray<ReadonlyVector3Int>(heights, Allocator.TempJob),

                // output
                Result = new NativeArray<BlockType>(types, Allocator.TempJob)
            };

            JobHandle typeJobHandle = typeJob.Schedule(inputSize, 8);
            typeJobHandle.Complete();
            typeJob.Result.CopyTo(types);

            // cleanup
            typeJob.Result.Dispose();
            typeJob.Heights.Dispose();

            return types;
        }

        internal static void DeflattenizeOutput(ref BlockType[] types)
        {
            for (int x = 0; x < TotalBlockNumberX; x++)
                for (int y = 0; y < TotalBlockNumberY; y++)
                    for (int z = 0; z < TotalBlockNumberZ; z++)
                    {
                        BlockType type = types[Utils.IndexFlattenizer3D(x, y, z, TotalBlockNumberX, TotalBlockNumberY)];

                        ref BlockData b = ref GlobalVariables.Blocks[x, y, z];
                        b.Type = type;
                        b.Hp = LookupTables.BlockHealthMax[(int)type];
                    }
        }
    }
}
