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
        internal static ReadonlyVector3Int[] CalculateHeightsJobSystem()
        {
            // output data
            var heights = new ReadonlyVector3Int[TotalBlockNumberX * TotalBlockNumberZ];

            var heightJob = new HeightJob()
            {
                // input
                TotalBlockNumberX = TotalBlockNumberX,

                // output
                Result = new NativeArray<ReadonlyVector3Int>(heights, Allocator.TempJob)
            };

            var heightJobHandle = heightJob.Schedule(TotalBlockNumberX * TotalBlockNumberZ, 8);
            heightJobHandle.Complete();
            heightJob.Result.CopyTo(heights);

            // cleanup
            heightJob.Result.Dispose();

            return heights;
        }

        internal static BlockType[] CalculateBlockTypes(ReadonlyVector3Int[] heights)
        {
            var inputSize = TotalBlockNumberX * TotalBlockNumberY * TotalBlockNumberZ;

            // output data
            var types = new BlockType[inputSize];

            var typeJob = new BlockTypeJob()
            {
                // input
                TotalBlockNumberX = TotalBlockNumberX,
                TotalBlockNumberY = TotalBlockNumberY,
                TotalBlockNumberZ = TotalBlockNumberZ,
                Heights = new NativeArray<ReadonlyVector3Int>(heights, Allocator.TempJob),

                // output
                Result = new NativeArray<BlockType>(types, Allocator.TempJob)
            };

            var typeJobHandle = typeJob.Schedule(inputSize, 8);
            typeJobHandle.Complete();
            typeJob.Result.CopyTo(types);

            // cleanup
            typeJob.Result.Dispose();
            typeJob.Heights.Dispose();

            return types;
        }

        internal static BlockTypeColumn[] CalculateBlockColumn()
        {
            int inputSize = TotalBlockNumberX * TotalBlockNumberY * TotalBlockNumberZ;

            var outputArray = new BlockTypeColumn[inputSize];

            var job = new BlockColumnJob()
            {
                // input
                TotalBlockNumberX = TotalBlockNumberX,
                TotalBlockNumberY = TotalBlockNumberY,
                TotalBlockNumberZ = TotalBlockNumberZ,

                // output
                Result = new NativeArray<BlockTypeColumn>(outputArray, Allocator.TempJob)
            };

            // second parameter is the Innerloop Batch Count (whatever it may be)
            // according to Unity's documentation when job is small 32 or 64 makes sense
            // for huge work loads the preferred value is 1, hence the value below
            // unfortunately, the documentation does not provide any knowledge on how to precisely determine this value, just hints
            var jobHandle = job.Schedule(inputSize, 8);
            jobHandle.Complete();
            job.Result.CopyTo(outputArray);

            // cleanup
            job.Result.Dispose();

            return outputArray;
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

        static void DeflattenizeOutputOld(ref BlockTypeColumn[] columns)
        {
            //for (int x = 0; x < TotalBlockNumberX; x++)
            //    for (int z = 0; z < TotalBlockNumberZ; z++)
            //    {
            //        BlockTypeColumn column = columns[Utils.IndexFlattenizer2D(x, z, TotalBlockNumberX)];

            //        // heights are inclusive
            //        for (int y = 0; y <= column.TerrainLevel; y++)
            //        {
            //            BlockType type;

            //            unsafe
            //            {
            //                type = (BlockType)column.Types[y];
            //            }

            //            ref BlockData b = ref GlobalVariables.Blocks[x, y, z];
            //            b.Type = type;
            //            b.Hp = LookupTables.BlockHealthMax[(int)type];
            //        }
            //    }
        }
    }
}
