using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Common;
using Voxels.Common.DataModels;
using Voxels.TerrainGeneration.Jobs;

namespace Voxels.TerrainGeneration
{
    public static class TerrainGenerator
    {
        #region Constants
        // caves should be more erratic so has to be a higher number
        const float CAVE_PROBABILITY = 0.44f;
        const float CAVE_SMOOTH = 0.09f;
        const int CAVE_OCTAVES = 3; // reduced a bit to lower workload but not to much to maintain randomness

        // shiny diamonds!
        const float DIAMOND_PROBABILITY = 0.38f; // this is not percentage chance because we are using Perlin function
        const float DIAMOND_SMOOTH = 0.06f;
        const int DIAMOND_OCTAVES = 1;
        const int DIAMOND_MAX_HEIGHT = 80;

        // red stones
        const float REDSTONE_PROBABILITY = 0.36f;
        const float REDSTONE_SMOOTH = 0.06f;
        const int REDSTONE_OCTAVES = 1;
        const int REDSTONE_MAX_HEIGHT = 50;

        // woodbase
        const float WOODBASE_HIGH_PROBABILITY = 0.36f;
        const float WOODBASE_SOME_PROBABILITY = 0.31f;
        const float WOODBASE_SMOOTH = 0.4f;
        const int WOODBASE_OCTAVES = 1;
        const int TREE_HEIGHT = 7;

        // dirt
        const int MAX_HEIGHT_DIRT = 90;
        const float SMOOTH_DIRT = 0.01f; // bigger number increases sampling of the function
        const int OCTAVES_DIRT = 3;
        const float PERSISTENCE_DIRT = 0.5f;

        // stone
        const int MAX_HEIGHT_STONE = 80;
        const float SMOOTH_STONE = 0.05f;
        const int OCTAVES_STONE = 2;
        const float PERSISTENCE_STONE = 0.25f;

        // bedrock
        const int MAX_HEIGHT_BEDROCK = 15;
        const float SMOOTH_BEDROCK = 0.1f;
        const int OCTAVES_BEDROCK = 1;
        const float PERSISTENCE_BEDROCK = 0.5f;
        #endregion

        internal static int TotalBlockNumberX, TotalBlockNumberY, TotalBlockNumberZ;

        static ComputeShader _heightsShader;

        /// <summary>
        /// Water level inclusive.
        /// </summary>
        static int _waterLevel;
        static int _worldSizeX, _worldSizeZ;

        internal static void Initialize(ComputeShader heightsShader)
        {
            _worldSizeX = GlobalVariables.Settings.WorldSizeX;
            _worldSizeZ = GlobalVariables.Settings.WorldSizeZ;
            TotalBlockNumberX = _worldSizeX * Constants.CHUNK_SIZE;
            TotalBlockNumberY = Constants.WORLD_SIZE_Y * Constants.CHUNK_SIZE;
            TotalBlockNumberZ = _worldSizeZ * Constants.CHUNK_SIZE;

            _waterLevel = GlobalVariables.Settings.WaterLevel;

            _heightsShader = heightsShader;
        }

        internal static ReadonlyVector3Int CalculateHeights(int seed, int x, int z)
            => new ReadonlyVector3Int(
                (int)Map(0, MAX_HEIGHT_BEDROCK, 0, 1, 
                    FractalBrownianMotion(seed, x * SMOOTH_BEDROCK, z * SMOOTH_BEDROCK, OCTAVES_BEDROCK, PERSISTENCE_BEDROCK)),
                (int)Map(0, MAX_HEIGHT_STONE, 0, 1,
                    FractalBrownianMotion(seed, x * SMOOTH_STONE, z * SMOOTH_STONE, OCTAVES_STONE, PERSISTENCE_STONE)),
                (int)Map(0, MAX_HEIGHT_DIRT, 0, 1,
                    FractalBrownianMotion(seed, x * SMOOTH_DIRT, z * SMOOTH_DIRT, OCTAVES_DIRT, PERSISTENCE_DIRT)));

        //internal static int GenerateBedrockHeight(int seed, int x, int z) =>
        //    (int)Map(0, MAX_HEIGHT_BEDROCK, 0, 1,
        //        FractalBrownianMotion(seed, x * SMOOTH_BEDROCK, z * SMOOTH_BEDROCK, OCTAVES_BEDROCK, PERSISTENCE_BEDROCK));

        //internal static int GenerateStoneHeight(int seed, int x, int z) =>
        //    (int)Map(0, MAX_HEIGHT_STONE, 0, 1,
        //        FractalBrownianMotion(seed, x * SMOOTH_STONE, z * SMOOTH_STONE, OCTAVES_STONE, PERSISTENCE_STONE));

        //internal static int GenerateDirtHeight(int seed, int x, int z) =>
        //    (int)Map(0, MAX_HEIGHT_DIRT, 0, 1,
        //        FractalBrownianMotion(seed, x * SMOOTH_DIRT, z * SMOOTH_DIRT, OCTAVES_DIRT, PERSISTENCE_DIRT));

        static float Map(float newMin, float newMax, float oldMin, float oldMax, float value) 
            => Mathf.Lerp(newMin, newMax, Mathf.InverseLerp(oldMin, oldMax, value));

        /// <summary>
        /// Heights are inclusive.
        /// First height is bedrock, second is stone, and the third is dirt.
        /// </summary>
		internal static BlockType DetermineType(int seed, int worldX, int worldY, int worldZ, in ReadonlyVector3Int heights)
        {
            if (worldY == 0)
                return BlockType.Bedrock;

            // check if this suppose to be a cave
            if (FractalFunc(seed, worldX, worldY, worldZ, CAVE_SMOOTH, CAVE_OCTAVES) < CAVE_PROBABILITY)
                return BlockType.Air;

            // bedrock
            if (worldY <= heights.X)
                return BlockType.Bedrock;

            // stone
            if (worldY <= heights.Y)
            {
                if (worldY < DIAMOND_MAX_HEIGHT
                    && FractalFunc(seed, worldX, worldY, worldZ, DIAMOND_SMOOTH, DIAMOND_OCTAVES) < DIAMOND_PROBABILITY)
                    return BlockType.Diamond;

                if (worldY < REDSTONE_MAX_HEIGHT
                    && FractalFunc(seed, worldX, worldY, worldZ, REDSTONE_SMOOTH, REDSTONE_OCTAVES) < REDSTONE_PROBABILITY)
                    return BlockType.Redstone;

                return BlockType.Stone;
            }

            // dirt
            if (worldY == heights.Z)
                return BlockType.Grass;

            if (worldY < heights.Z)
                return BlockType.Dirt;

            return BlockType.Air;
        }

        // good noise generator
        // persistence - if < 1 each function is less powerful than the previous one, for > 1 each is more important
        // octaves - number of functions that we sum up
        static float FractalBrownianMotion(int seed, float x, float z, int oct, float pers)
        {
            float total = 0, frequency = 1, amplitude = 1, maxValue = 0;

            for (int i = 0; i < oct; i++)
            {
                total += Mathf.PerlinNoise((x + seed) * frequency, (z + seed) * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= pers;
                frequency *= 2;
            }

            return total / maxValue;
        }

        // FractalBrownianMotion3D
        static float FractalFunc(int seed, float x, float y, int z, float smooth, int octaves)
        {
            // this is obviously more computational heavy
            float xy = FractalBrownianMotion(seed, x * smooth, y * smooth, octaves, 0.5f);
            float yz = FractalBrownianMotion(seed, y * smooth, z * smooth, octaves, 0.5f);
            float xz = FractalBrownianMotion(seed, x * smooth, z * smooth, octaves, 0.5f);

            float yx = FractalBrownianMotion(seed, y * smooth, x * smooth, octaves, 0.5f);
            float zy = FractalBrownianMotion(seed, z * smooth, y * smooth, octaves, 0.5f);
            float zx = FractalBrownianMotion(seed, z * smooth, x * smooth, octaves, 0.5f);

            return (xy + yz + xz + yx + zy + zx) / 6.0f;
        }

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

        /// <summary>
        /// Calculates global heights. This method uses compute shaders.
        /// Unfortunately, this method is slower than the classic approach.
        /// Mostly due to the necessity of data preparation.
        /// Each column described by its x and z values has three heights.
        /// One for Bedrock, Stone and Dirt. Heights determines up to where certain types appear.
        /// x is Bedrock, y is Stone and z is Dirt.
        /// </summary>
        public static int3[] CalculateHeightsGPU()
        {
            var inputData = new int3[TotalBlockNumberX * TotalBlockNumberZ];

            // unfortunately shaders can receive only one dim data
            // there is no 2-dimensional buffers in HLSL
            // would be nice if we could pass 2d data set

            // preparing - this takes the most time at the moment
            for (int x = 0; x < TotalBlockNumberX; x++)
                for (int z = 0; z < TotalBlockNumberZ; z++)
                    inputData[Utils.IndexFlattenizer2D(x, z, TotalBlockNumberX)] = new int3(x, 0, z);

            // size of a single element in the array
            //int size = System.Runtime.InteropServices.Marshal.SizeOf(new int3()); // equals 12
            var buffer = new ComputeBuffer(inputData.Length, 12);

            buffer.SetData(inputData);

            // The FindKernel function takes a string name, which corresponds to one of the kernel names 
            // we set up in the compute shader. 
            int kernel = _heightsShader.FindKernel("CSMain");

            _heightsShader.SetBuffer(kernel, "Result", buffer);
            _heightsShader.SetInt("Seed", 1300);

            // the integers passed to the Dispatch call specify the number of thread groups we want to spawn
            _heightsShader.Dispatch(kernel, 16, 32, 16);

            var output = new int3[TotalBlockNumberX * TotalBlockNumberZ];
            buffer.GetData(output);

            return output;
        }

        public static BlockType[] CalculateBlockTypes(ReadonlyVector3Int[] heights)
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

        public static BlockTypeColumn[] CalculateBlockColumn()
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

        internal static void CalculateBlockTypesForColumnParallel(int seed, int colX, int colZ)
        {
            ReadonlyVector3Int heights = CalculateHeights(seed, colX, colZ);

            // omit everything above the maximum height as it's air anyway
            int max = heights.X;
            if (heights.Y > max)
                max = heights.Y;
            if (heights.Z > max)
                max = heights.Z;

            // height is inclusive
            for (int y = 0; y <= max; y++)
                CreateBlock(ref GlobalVariables.Blocks[colX, y, colZ], DetermineType(seed, colX, y, colZ, in heights));
        }

        /// <summary>
        /// Adds water to the <see cref="GlobalVariables.Blocks"/>.
        /// </summary>
        internal static void AddWater()
        {
            BlockData[,,] blocks = GlobalVariables.Blocks;

            // first run - turn all Air blocks at the WaterLevel and one level below into Water blocks
            for (int x = 0; x < TotalBlockNumberX; x++)
                for (int z = 0; z < TotalBlockNumberZ; z++)
                    if (blocks[x, _waterLevel, z].Type == BlockType.Air)
                    {
                        blocks[x, _waterLevel, z].Type = BlockType.Water;
                        if (blocks[x, _waterLevel - 1, z].Type == BlockType.Air) // level down scan
                            blocks[x, _waterLevel - 1, z].Type = BlockType.Water;
                    }

            PropagateWaterHorizontally(_waterLevel - 1);

            int currentY = _waterLevel - 1;

            bool waterAdded = true;
            while (waterAdded)
            {
                waterAdded = currentY > 1
                    ? AddWaterBelow(currentY)
                    : false;

                if (waterAdded)
                    PropagateWaterHorizontally(--currentY);
            }
        }

        /// <summary>
        /// Adds trees to the <see cref="GlobalVariables.Blocks"/>.
        /// If treeProb parameter is set to TreeProbability.None then no trees will be added.
        /// </summary>
        internal static void AddTrees(TreeProbability treeProb)
        {
            if (treeProb == TreeProbability.None)
                return;

            BlockData[,,] blocks = GlobalVariables.Blocks;

            float woodbaseProbability = treeProb == TreeProbability.Some
                ? WOODBASE_SOME_PROBABILITY
                : WOODBASE_HIGH_PROBABILITY;

            for (int x = 1; x < TotalBlockNumberX - 1; x++)
                // this 20 is hard coded as for now but generally it would be nice if
                // this loop could know in advance where the lowest grass is
                for (int y = 20; y < TotalBlockNumberY - TREE_HEIGHT - 1; y++)
                    for (int z = 1; z < TotalBlockNumberZ - 1; z++)
                    {
                        if (blocks[x, y, z].Type != BlockType.Grass)
                            continue;

                        if (IsThereEnoughSpaceForTree(in blocks, x, y, z))
                            if (FractalFunc(GlobalVariables.Settings.SeedValue, x, y, z, WOODBASE_SMOOTH, WOODBASE_OCTAVES) < woodbaseProbability)
                                BuildTree(x, y, z);
                    }
        }

        /// <summary>
		/// Adds trees to the world.
		/// If treeProb parameter is set to TreeProbability.None then no trees will be added.
        /// In order to avoid potential collisions boundary cubes on x and z axis are ignored 
        /// and therefore will never grow a tree resulting in slightly different although unnoticeable
        /// for player results.
		/// </summary>
		internal static void AddTreesParallel()
        {
            TreeProbability treeProb = GlobalVariables.Settings.TreeProbability;

            if (treeProb == TreeProbability.None)
                return;

            float woodbaseProbability = treeProb == TreeProbability.Some
                ? WOODBASE_SOME_PROBABILITY
                : WOODBASE_HIGH_PROBABILITY;

            var queue = new MultiThreadTaskQueue();

            // schedule one task per chunk
            for (int i = 0; i < GlobalVariables.Settings.WorldSizeX; i++)
                for (int j = 0; j < GlobalVariables.Settings.WorldSizeZ; j++)
                    queue.ScheduleTask(AddTreesInChunkParallel, woodbaseProbability, i, j);

            queue.RunAllInParallel(); // this is synchronous
        }

        static void AddTreesInChunkParallel(float woodbaseProbability, int chunkColumnX, int chunkColumnZ)
        {
            for (int x = 1 + chunkColumnX * Constants.CHUNK_SIZE; x < chunkColumnX * Constants.CHUNK_SIZE + Constants.CHUNK_SIZE - 1; x++)
                // this 20 is hard coded as for now but generally it would be nice if
                // this loop could know in advance where the lowest grass is
                for (int y = 20; y < TotalBlockNumberY - TREE_HEIGHT - 1; y++)
                    for (int z = 1 + chunkColumnZ * Constants.CHUNK_SIZE; z < chunkColumnZ * Constants.CHUNK_SIZE + Constants.CHUNK_SIZE - 1; z++)
                    {
                        if (GlobalVariables.Blocks[x, y, z].Type != BlockType.Grass)
                            continue;

                        if (IsThereEnoughSpaceForTree(in GlobalVariables.Blocks, x, y, z))
                            if (FractalFunc(GlobalVariables.Settings.SeedValue, x, y, z, WOODBASE_SMOOTH, WOODBASE_OCTAVES) < woodbaseProbability)
                                BuildTree(x, y, z);
                    }
        }

        /// <summary>
        /// Spread the water horizontally.
        /// All air blocks that have a horizontal access to any water blocks will be turned into water blocks.
        static void PropagateWaterHorizontally(int currentY)
        {
            /*
				This algorithm works in two steps:
				Step 1 - scan the layer line by line and if there is an air block preceded by a water block then convert this block to water.
				Step 2 - scan each block in the layer individually and if the block is air then check if any of its neighbors is water,
				    if so convert it to water. If any block has been converted during the process repeat the whole step 2 again.
			*/

            BlockData[,,] blocks = GlobalVariables.Blocks;

            // === Step 1 ===
            bool foundWater = false;
            BlockType type;
            int x, z; // iteration variables

            // z ascending
            for (x = 0; x < TotalBlockNumberX; x++)
                for (z = 0; z < TotalBlockNumberZ; z++)
                {
                    type = blocks[x, currentY, z].Type;
                    if (ChangeToWater())
                        blocks[x, currentY, z].Type = BlockType.Water;
                }

            // x ascending
            for (z = 0; z < TotalBlockNumberZ; z++)
                for (x = 0; x < TotalBlockNumberX; x++)
                {
                    type = blocks[x, currentY, z].Type;
                    if (ChangeToWater())
                        blocks[x, currentY, z].Type = BlockType.Water;
                }

            // z descending
            for (x = 0; x < TotalBlockNumberX; x++)
                for (z = TotalBlockNumberZ - 1; z >= 0; z--)
                {
                    type = blocks[x, currentY, z].Type;
                    if (ChangeToWater())
                        blocks[x, currentY, z].Type = BlockType.Water;
                }

            // x descending
            for (z = 0; z < TotalBlockNumberZ; z++)
                for (x = TotalBlockNumberX - 1; x >= 0; x--)
                {
                    type = blocks[x, currentY, z].Type;
                    if (ChangeToWater())
                        blocks[x, currentY, z].Type = BlockType.Water;
                }

            // local functions introduced in C# 7.0 are quite useful as they have access to all variables in the upper scope
            // by definition, these functions are private, and they cannot have any attributes
            // it's a bit sad they can't access the variables passed by a reference
            bool ChangeToWater()
            {
                // previous block was water
                if (foundWater)
                {
                    // this block is air
                    if (type == BlockType.Air)
                        return true;

                    if (type != BlockType.Water)
                        foundWater = false;
                }
                else if (type == BlockType.Water)
                    foundWater = true;

                return false;
            }

            // === Step 2 ===
            // reiterate if at least one block was converted to water in the previous iteration
            bool reiterate;
            do
            {
                reiterate = false;

                for (x = 0; x < TotalBlockNumberX; x++)
                    for (z = 0; z < TotalBlockNumberZ; z++)
                        if (blocks[x, currentY, z].Type == BlockType.Air)
                        {
                            if (x < TotalBlockNumberX - 1 && blocks[x + 1, currentY, z].Type == BlockType.Water) // right
                            {
                                blocks[x, currentY, z].Type = BlockType.Water;
                                reiterate = true;
                            }
                            else if (x > 0 && blocks[x - 1, currentY, z].Type == BlockType.Water) // left
                            {
                                blocks[x, currentY, z].Type = BlockType.Water;
                                reiterate = true;
                            }
                            else if (z < TotalBlockNumberZ - 1 && blocks[x, currentY, z + 1].Type == BlockType.Water) // front
                            {
                                blocks[x, currentY, z].Type = BlockType.Water;
                                reiterate = true;
                            }
                            else if (z > 0 && blocks[x, currentY, z - 1].Type == BlockType.Water) // back
                            {
                                blocks[x, currentY, z].Type = BlockType.Water;
                                reiterate = true;
                            }
                        }
            } while (reiterate);
        }

        /// <summary>
        /// Returns true if at least on block was changed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool AddWaterBelow(int currentY)
        {
            // assertion 
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (currentY < 1)
                throw new System.ArgumentException("Y value cannot be lower than 1.", "currentY");
#endif

            BlockData[,,] blocks = GlobalVariables.Blocks;

            bool waterAdded = false;
            for (int x = 0; x < TotalBlockNumberX; x++)
                for (int z = 0; z < TotalBlockNumberZ; z++)
                {
                    if (blocks[x, currentY, z].Type == BlockType.Water
                        && blocks[x, currentY - 1, z].Type == BlockType.Air)
                    {
                        blocks[x, currentY - 1, z].Type = BlockType.Water;
                        waterAdded = true;
                    }
                }

            return waterAdded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsThereEnoughSpaceForTree(in BlockData[,,] blocks, int x, int y, int z)
        {
            for (int i = 2; i < TREE_HEIGHT; i++)
            {
                if (blocks[x + 1, y + i, z].Type != BlockType.Air
                    || blocks[x - 1, y + i, z].Type != BlockType.Air
                    || blocks[x, y + i, z + 1].Type != BlockType.Air
                    || blocks[x, y + i, z - 1].Type != BlockType.Air
                    || blocks[x + 1, y + i, z + 1].Type != BlockType.Air
                    || blocks[x + 1, y + i, z - 1].Type != BlockType.Air
                    || blocks[x - 1, y + i, z + 1].Type != BlockType.Air
                    || blocks[x - 1, y + i, z - 1].Type != BlockType.Air)
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void BuildTree(int x, int y, int z)
        {
            BlockData[,,] blocks = GlobalVariables.Blocks;

            CreateBlock(ref blocks[x, y, z], BlockType.Woodbase);
            CreateBlock(ref blocks[x, y + 1, z], BlockType.Wood);
            CreateBlock(ref blocks[x, y + 2, z], BlockType.Wood);

            int i, j, k;
            for (i = -1; i <= 1; i++)
                for (j = -1; j <= 1; j++)
                    for (k = 3; k <= 4; k++)
                        CreateBlock(ref blocks[x + i, y + k, z + j], BlockType.Leaves);

            CreateBlock(ref blocks[x, y + 5, z], BlockType.Leaves);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CreateBlock(ref BlockData block, BlockType type)
        {
            block.Type = type;
            block.Hp = LookupTables.BlockHealthMax[(int)type];
        }
    }
}