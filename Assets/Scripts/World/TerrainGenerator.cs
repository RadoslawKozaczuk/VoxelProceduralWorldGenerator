using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.World
{
    public class TerrainGenerator
	{
		#region Constants
		// caves should be more erratic so has to be a higher number
		const float CaveProbability = 0.44f;
		const float CaveSmooth = 0.09f;
		const int CaveOctaves = 3; // reduced a bit to lower workload but not to much to maintain randomness

		// shiny diamonds!
		const float DiamondProbability = 0.38f; // this is not percentage chance because we are using Perlin function
		const float DiamondSmooth = 0.06f;
		const int DiamondOctaves = 1;
		const int DiamondMaxHeight = 80;

		// red stones
		const float RedstoneProbability = 0.36f;
		const float RedstoneSmooth = 0.06f;
		const int RedstoneOctaves = 1;
		const int RedstoneMaxHeight = 50;

		// woodbase
		const float WoodbaseHighProbability = 0.36f;
		const float WoodbaseSomeProbability = 0.31f;
		const float WoodbaseSmooth = 0.4f;
		const int WoodbaseOctaves = 1;
		const int TreeHeight = 7;

		const int MaxHeight = 90;
		const float Smooth = 0.01f; // bigger number increases sampling of the function
		const int Octaves = 3;
		const float Persistence = 0.5f;

		const int MaxHeightStone = 80;
		const float SmoothStone = 0.05f;
		const int OctavesStone = 2;
		const float PersistenceStone = 0.25f;

		const int MaxHeightBedrock = 15;
		const float SmoothBedrock = 0.1f;
		const int OctavesBedrock = 1;
		const float PersistenceBedrock = 0.5f;
		#endregion

		public int WaterLevel; // inclusive
		public float SeedValue;

		int _worldSizeX, _worldSizeZ, _totalBlockNumberX, _totalBlockNumberY, _totalBlockNumberZ;

        [SerializeField] ComputeShader _heightsShader;

        public void Initialize(GameSettings options)
		{
			_worldSizeX = options.WorldSizeX;
			_worldSizeZ = options.WorldSizeZ;
			_totalBlockNumberX = _worldSizeX * World.CHUNK_SIZE;
			_totalBlockNumberY = World.WORLD_SIZE_Y * World.CHUNK_SIZE;
			_totalBlockNumberZ = _worldSizeZ * World.CHUNK_SIZE;

			WaterLevel = options.WaterLevel;
			SeedValue = options.SeedValue;
		}

		public static int GenerateBedrockHeight(float SeedValue, float x, float z) =>
			(int)Map(0, MaxHeightBedrock, 0, 1,
				FractalBrownianMotion(SeedValue, x * SmoothBedrock, z * SmoothBedrock, OctavesBedrock, PersistenceBedrock));

		public static int GenerateStoneHeight(float SeedValue, float x, float z) =>
			(int)Map(0, MaxHeightStone, 0, 1,
				FractalBrownianMotion(SeedValue, x * SmoothStone, z * SmoothStone, OctavesStone, PersistenceStone));

		public static int GenerateDirtHeight(float SeedValue, float x, float z) =>
			(int)Map(0, MaxHeight, 0, 1,
				FractalBrownianMotion(SeedValue, x * Smooth, z * Smooth, Octaves, Persistence));

		public static float Map(float newMin, float newMax, float oldMin, float oldMax, float value) =>
			Mathf.Lerp(newMin, newMax, Mathf.InverseLerp(oldMin, oldMax, value));

        /// <summary>
        /// Heights are inclusive.
        /// First height is bedrock, second is stone, and the third is dirt.
        /// </summary>
		public static BlockType DetermineType(float seedValue, int worldX, int worldY, int worldZ, int3 heights)
		{
			if (worldY == 0)
                return BlockType.Bedrock;

			// check if this suppose to be a cave
			if (FractalFunc(seedValue, worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
				return BlockType.Air;

			// bedrock
			if (worldY <= heights.x)
                return BlockType.Bedrock;

			// stone
			if (worldY <= heights.y)
			{
				if (worldY < DiamondMaxHeight
                    && FractalFunc(seedValue, worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability)
					return BlockType.Diamond;

				if (worldY < RedstoneMaxHeight
                    && FractalFunc(seedValue, worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability)
					return BlockType.Redstone;

				return BlockType.Stone;
			}

			// dirt
			if (worldY == heights.z)
                return BlockType.Grass;

			if (worldY < heights.z)
                return BlockType.Dirt;

			return BlockType.Air;
		}

		// good noise generator
		// persistence - if < 1 each function is less powerful than the previous one, for > 1 each is more important
		// octaves - number of functions that we sum up
		static float FractalBrownianMotion(float seedValue, float x, float z, int oct, float pers)
		{
			float total = 0, frequency = 1, amplitude = 1, maxValue = 0;

			for (int i = 0; i < oct; i++)
			{
				total += Mathf.PerlinNoise((x + seedValue) * frequency, (z + seedValue) * frequency) * amplitude;
				maxValue += amplitude;
				amplitude *= pers;
				frequency *= 2;
			}

			return total / maxValue;
		}

		// FractalBrownianMotion3D
		static float FractalFunc(float seedValue, float x, float y, int z, float smooth, int octaves)
		{
			// this is obviously more computational heavy
			float xy = FractalBrownianMotion(seedValue, x * smooth, y * smooth, octaves, 0.5f);
			float yz = FractalBrownianMotion(seedValue, y * smooth, z * smooth, octaves, 0.5f);
			float xz = FractalBrownianMotion(seedValue, x * smooth, z * smooth, octaves, 0.5f);

			float yx = FractalBrownianMotion(seedValue, y * smooth, x * smooth, octaves, 0.5f);
			float zy = FractalBrownianMotion(seedValue, z * smooth, y * smooth, octaves, 0.5f);
			float zx = FractalBrownianMotion(seedValue, z * smooth, x * smooth, octaves, 0.5f);

			return (xy + yz + xz + yx + zy + zx) / 6.0f;
		}

        /// <summary>
        /// Calculates global heights. This method uses Unity Job System.
        /// Each column described by its x and z values has three heights.
        /// One for Bedrock, Stone and Dirt. Heights determines up to where certain types appear.
        /// x is Bedrock, y is Stone and z is Dirt.
        /// </summary>
        public int3[] CalculateHeightsJobSystem()
		{
			// output data
			var heights = new int3[_totalBlockNumberX * _totalBlockNumberZ];

			var heightJob = new HeightJob()
			{
				// input
				TotalBlockNumberX = _totalBlockNumberX,
				SeedValue = SeedValue,

				// output
				Result = new NativeArray<int3>(heights, Allocator.TempJob)
			};

			var heightJobHandle = heightJob.Schedule(_totalBlockNumberX * _totalBlockNumberZ, 8);
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
        public int3[] CalculateHeightsGPU()
        {
            var inputData = new int3[_totalBlockNumberX * _totalBlockNumberZ];

            // unfortunately shaders can receive only one dim data
            // there is no 2-dimensional buffers in HLSL
            // would be nice if we could pass 2d data set

            // preparing - this takes the most time at the moment
            for (int x = 0; x < _totalBlockNumberX; x++)
                for (int z = 0; z < _totalBlockNumberZ; z++)
                    inputData[Utils.IndexFlattenizer2D(x, z, _totalBlockNumberX)] = new int3(x, 0, z);

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

            var output = new int3[_totalBlockNumberX * _totalBlockNumberZ];
            buffer.GetData(output);

            return output;
        }

        public BlockType[] CalculateBlockTypes(int3[] heights)
		{
			var inputSize = _totalBlockNumberX * _totalBlockNumberY * _totalBlockNumberZ;

			// output data
			var types = new BlockType[inputSize];

			var typeJob = new BlockTypeJob()
			{
				// input
				TotalBlockNumberX = _totalBlockNumberX,
				TotalBlockNumberY = _totalBlockNumberY,
				TotalBlockNumberZ = _totalBlockNumberZ,
				Heights = new NativeArray<int3>(heights, Allocator.TempJob),

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

        public BlockTypeColumn[] CalculateBlockColumn()
        {
            int inputSize = _totalBlockNumberX * _totalBlockNumberY * _totalBlockNumberZ;

            var outputArray = new BlockTypeColumn[inputSize];

            var job = new BlockColumnJob()
            {
                // input
                TotalBlockNumberX = _totalBlockNumberX,
                TotalBlockNumberY = _totalBlockNumberY,
                TotalBlockNumberZ = _totalBlockNumberZ,
                SeedValue = SeedValue,

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

        public void CalculateBlockTypesParallel()
        {
            var queue = new MultiThreadTaskQueue();

            int x, z;
            for (x = 0; x < _totalBlockNumberX; x++)
                for (z = 0; z < _totalBlockNumberZ; z++)
                    queue.ScheduleTask(CalculateBlockTypesForColumnParallel, x, z);

            queue.RunAllInParallel();
        }

        void CalculateBlockTypesForColumnParallel(int colX, int colZ)
        {
            int3 height = new int3()
            {
                x = GenerateBedrockHeight(SeedValue, colX, colZ),
                y = GenerateStoneHeight(SeedValue, colX, colZ),
                z = GenerateDirtHeight(SeedValue, colX, colZ)
            };

            // omit everything about the maximum height as it is air anyway
            int max = height.x;
            if (height.y > max)
                max = height.y;
            if (height.z > max)
                max = height.z;

            // height is inclusive
            for (int y = 0; y <= max; y++)
                CreateBlock(ref World.Blocks[colX, y, colZ], DetermineType(SeedValue, colX, y, colZ, height));
        }

        public void AddWater(ref BlockData[,,] blocks)
		{
			// first run - turn all Air blocks at the WaterLevel and one level below into Water blocks
			for (int x = 0; x < _totalBlockNumberX; x++)
				for (int z = 0; z < _totalBlockNumberZ; z++)
					if (blocks[x, WaterLevel, z].Type == BlockType.Air)
					{
						blocks[x, WaterLevel, z].Type = BlockType.Water;
						if (blocks[x, WaterLevel - 1, z].Type == BlockType.Air) // level down scan
							blocks[x, WaterLevel - 1, z].Type = BlockType.Water;
					}

			PropagateWaterHorizontally(ref blocks, WaterLevel - 1);

			int currentY = WaterLevel - 1;

			bool waterAdded = true;
			while (waterAdded)
			{
                waterAdded = currentY > 1 
                    ? AddWaterBelow(ref blocks, currentY) 
                    : false;

                if (waterAdded)
                    PropagateWaterHorizontally(ref blocks, --currentY);
            }
		}

		/// <summary>
		/// Adds trees to the world.
		/// If treeProb parameter is set to TreeProbability.None then no trees will be added.
		/// </summary>
		public void AddTrees(ref BlockData[,,] blocks, TreeProbability treeProb)
		{
			if (treeProb == TreeProbability.None)
				return;

			float woodbaseProbability = treeProb == TreeProbability.Some
				? WoodbaseSomeProbability
				: WoodbaseHighProbability;

			for (int x = 1; x < _totalBlockNumberX - 1; x++)
				// this 20 is hard coded as for now but generally it would be nice if
				// this loop could know in advance where the lowest grass is
				for (int y = 20; y < _totalBlockNumberY - TreeHeight - 1; y++)
					for (int z = 1; z < _totalBlockNumberZ - 1; z++)
					{
						if (blocks[x, y, z].Type != BlockType.Grass) 
							continue;

						if (IsThereEnoughSpaceForTree(in blocks, x, y, z))
							if (FractalFunc(SeedValue, x, y, z, WoodbaseSmooth, WoodbaseOctaves) < woodbaseProbability)
								BuildTree(ref blocks, x, y, z);
					}
		}

        /// <summary>
		/// Adds trees to the world.
		/// If treeProb parameter is set to TreeProbability.None then no trees will be added.
        /// In order to avoid potential collisions boundary cubes on x and z axis are ignored 
        /// and therefore will never grow a tree resulting in slightly different although unnoticeable
        /// for player results.
		/// </summary>
		public void AddTreesParallel(TreeProbability treeProb)
        {
            if (treeProb == TreeProbability.None)
                return;

            float woodbaseProbability = treeProb == TreeProbability.Some
                ? WoodbaseSomeProbability
                : WoodbaseHighProbability;

            var queue = new MultiThreadTaskQueue();

            // schedule one task per chunk
            for (int i = 0; i < World.Settings.WorldSizeX; i++)
                for (int j = 0; j < World.Settings.WorldSizeZ; j++)
                    queue.ScheduleTask(AddTreesInChunkParallel, woodbaseProbability, i, j);

            queue.RunAllInParallel(); // this is synchronous
        }

        void AddTreesInChunkParallel(float woodbaseProbability, int chunkColumnX, int chunkColumnZ)
        {
            for (int x = 1 + chunkColumnX * World.CHUNK_SIZE; x < chunkColumnX * World.CHUNK_SIZE + World.CHUNK_SIZE - 1; x++)
                // this 20 is hard coded as for now but generally it would be nice if
                // this loop could know in advance where the lowest grass is
                for (int y = 20; y < _totalBlockNumberY - TreeHeight - 1; y++)
                    for (int z = 1 + chunkColumnZ * World.CHUNK_SIZE; z < chunkColumnZ * World.CHUNK_SIZE + World.CHUNK_SIZE - 1; z++)
                    {
                        if (World.Blocks[x, y, z].Type != BlockType.Grass)
                            continue;

                        if (IsThereEnoughSpaceForTree(in World.Blocks, x, y, z))
                            if (FractalFunc(SeedValue, x, y, z, WoodbaseSmooth, WoodbaseOctaves) < woodbaseProbability)
                                BuildTree(ref World.Blocks, x, y, z);
                    }
        }

        /// <summary>
        /// Spread the water horizontally.
        /// All air blocks that have a horizontal access to any water blocks will be turned into water blocks.
        void PropagateWaterHorizontally(ref BlockData[,,] blocks, int currentY)
		{
			/*
				This algorithm works in two steps:
				Step 1 - scan the layer line by line and if there is an air block preceded by a water block then convert this block to water.
				Step 2 - scan each block in the layer individually and if the block is air then check if any of its neighbors is water,
				    if so convert it to water. If any block has been converted during the process repeat the whole step 2 again.
			*/

			// === Step 1 ===
			bool foundWater = false;
			BlockType type;
			int x, z; // iteration variables

			// z ascending
			for (x = 0; x < _totalBlockNumberX; x++)
				for (z = 0; z < _totalBlockNumberZ; z++)
				{
					type = blocks[x, currentY, z].Type;
					if (ChangeToWater())
						blocks[x, currentY, z].Type = BlockType.Water;
				}

			// x ascending
			for (z = 0; z < _totalBlockNumberZ; z++)
				for (x = 0; x < _totalBlockNumberX; x++)
				{
					type = blocks[x, currentY, z].Type;
					if (ChangeToWater())
						blocks[x, currentY, z].Type = BlockType.Water;
				}

			// z descending
			for (x = 0; x < _totalBlockNumberX; x++)
				for (z = _totalBlockNumberZ - 1; z >= 0; z--)
				{
					type = blocks[x, currentY, z].Type;
					if (ChangeToWater())
						blocks[x, currentY, z].Type = BlockType.Water;
				}

			// x descending
			for (z = 0; z < _totalBlockNumberZ; z++)
				for (x = _totalBlockNumberX - 1; x >= 0; x--)
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

                for (x = 0; x < _totalBlockNumberX; x++)
                    for (z = 0; z < _totalBlockNumberZ; z++)
                        if (blocks[x, currentY, z].Type == BlockType.Air)
                        {
                            if (x < _totalBlockNumberX - 1 && blocks[x + 1, currentY, z].Type == BlockType.Water) // right
                            {
                                blocks[x, currentY, z].Type = BlockType.Water;
                                reiterate = true;
                            }
                            else if (x > 0 && blocks[x - 1, currentY, z].Type == BlockType.Water) // left
                            {
                                blocks[x, currentY, z].Type = BlockType.Water;
                                reiterate = true;
                            }
                            else if (z < _totalBlockNumberZ - 1 && blocks[x, currentY, z + 1].Type == BlockType.Water) // front
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
		bool AddWaterBelow(ref BlockData[,,] blocks, int currentY)
		{
            // assertion 
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(currentY < 1)
                throw new System.ArgumentException("Y value cannot be lower than 1.", "currentY");
#endif

            bool waterAdded = false;
			for (int x = 0; x < _totalBlockNumberX; x++)
				for (int z = 0; z < _totalBlockNumberZ; z++)
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
		bool IsThereEnoughSpaceForTree(in BlockData[,,] blocks, int x, int y, int z)
		{
			for (int i = 2; i < TreeHeight; i++)
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
		void BuildTree(ref BlockData[,,] blocks, int x, int y, int z)
		{
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
		void CreateBlock(ref BlockData block, BlockType type)
		{
			block.Type = type;
			block.Hp = LookupTables.BlockHealthMax[(int)type];
		}
	}
}