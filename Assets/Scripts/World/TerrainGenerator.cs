using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.World
{
    public class TerrainGenerator : MonoBehaviour
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

		public static int WaterLevel; // inclusive
		public static float SeedValue;

		int _worldSizeX, _worldSizeZ, _totalBlockNumberX, _totalBlockNumberY, _totalBlockNumberZ;

        [SerializeField] ComputeShader _heightsShader;

        public void Initialize(GameSettings options)
		{
			_worldSizeX = options.WorldSizeX;
			_worldSizeZ = options.WorldSizeZ;
			_totalBlockNumberX = _worldSizeX * World.ChunkSize;
			_totalBlockNumberY = World.WorldSizeY * World.ChunkSize;
			_totalBlockNumberZ = _worldSizeZ * World.ChunkSize;

			WaterLevel = options.WaterLevel;
			SeedValue = options.SeedValue;
		}

		public static int GenerateBedrockHeight(float x, float z) =>
			(int)Map(0, MaxHeightBedrock, 0, 1,
				FractalBrownianMotion(x * SmoothBedrock, z * SmoothBedrock, OctavesBedrock, PersistenceBedrock));

		public static int GenerateStoneHeight(float x, float z) =>
			(int)Map(0, MaxHeightStone, 0, 1,
				FractalBrownianMotion(x * SmoothStone, z * SmoothStone, OctavesStone, PersistenceStone));

		public static int GenerateDirtHeight(float x, float z) =>
			(int)Map(0, MaxHeight, 0, 1,
				FractalBrownianMotion(x * Smooth, z * Smooth, Octaves, Persistence));

		public static float Map(float newMin, float newMax, float oldMin, float oldMax, float value) =>
			Mathf.Lerp(newMin, newMax, Mathf.InverseLerp(oldMin, oldMax, value));

		public static BlockTypes DetermineType(int worldX, int worldY, int worldZ, int3 height)
		{
			if (worldY == 0) return BlockTypes.Bedrock;

			// check if this suppose to be a cave
			if (FractalFunc(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
				return BlockTypes.Air;

			// bedrock
			if (worldY <= height.x) return BlockTypes.Bedrock;

			// stone
			if (worldY <= height.y)
			{
				if (worldY < DiamondMaxHeight
                    && FractalFunc(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability)
					return BlockTypes.Diamond;

				if (worldY < RedstoneMaxHeight
                    && FractalFunc(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability)
					return BlockTypes.Redstone;

				return BlockTypes.Stone;
			}

			// dirt
			if (worldY == height.z) return BlockTypes.Grass;
			if (worldY < height.z) return BlockTypes.Dirt;

			return BlockTypes.Air;
		}

		// good noise generator
		// persistence - if < 1 each function is less powerful than the previous one, for > 1 each is more important
		// octaves - number of functions that we sum up
		static float FractalBrownianMotion(float x, float z, int oct, float pers)
		{
			float total = 0, frequency = 1, amplitude = 1, maxValue = 0;

			for (int i = 0; i < oct; i++)
			{
				total += Mathf.PerlinNoise((x + SeedValue) * frequency, (z + SeedValue) * frequency) * amplitude;
				maxValue += amplitude;
				amplitude *= pers;
				frequency *= 2;
			}

			return total / maxValue;
		}

		// FractalBrownianMotion3D
		static float FractalFunc(float x, float y, int z, float smooth, int octaves)
		{
			// this is obviously more computational heavy
			float xy = FractalBrownianMotion(x * smooth, y * smooth, octaves, 0.5f);
			float yz = FractalBrownianMotion(y * smooth, z * smooth, octaves, 0.5f);
			float xz = FractalBrownianMotion(x * smooth, z * smooth, octaves, 0.5f);

			float yx = FractalBrownianMotion(y * smooth, x * smooth, octaves, 0.5f);
			float zy = FractalBrownianMotion(z * smooth, y * smooth, octaves, 0.5f);
			float zx = FractalBrownianMotion(z * smooth, x * smooth, octaves, 0.5f);

			return (xy + yz + xz + yx + zy + zx) / 6.0f;
		}

        /// <summary>
        /// Calculates global heights. This method uses Unity Job System.
        /// Each column described by its x and z values has three heights.
        /// One for Bedrock, Stone and Dirt. Heights determines up to where certain types appear.
        /// x is Bedrock, y is Stone and z is Dirt.
        /// </summary>
        public int3[] CalculateHeights()
		{
			// output data
			var heights = new int3[_totalBlockNumberX * _totalBlockNumberZ];

			var heightJob = new HeightJob()
			{
				// input
				TotalBlockNumberX = _totalBlockNumberX,

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
            // would be nice if we could pass 2d data seet

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

        public BlockTypes[] CalculateBlockTypes(int3[] heights)
		{
			var inputSize = _totalBlockNumberX * _totalBlockNumberY * _totalBlockNumberZ;

			// output data
			var types = new BlockTypes[inputSize];

			var typeJob = new BlockTypeJob()
			{
				// input
				TotalBlockNumberX = _totalBlockNumberX,
				TotalBlockNumberY = _totalBlockNumberY,
				TotalBlockNumberZ = _totalBlockNumberZ,
				Heights = new NativeArray<int3>(heights, Allocator.TempJob),

				// output
				Result = new NativeArray<BlockTypes>(types, Allocator.TempJob)
			};

			var typeJobHandle = typeJob.Schedule(inputSize, 8);
			typeJobHandle.Complete();
			typeJob.Result.CopyTo(types);

			// cleanup
			typeJob.Result.Dispose();
			typeJob.Heights.Dispose();

			return types;
		}

		public void AddWater(ref BlockData[,,] blocks)
		{
			// first run - turn all Air blocks at the WaterLevel and one level below into Water blocks
			for (int x = 0; x < _totalBlockNumberX; x++)
				for (int z = 0; z < _totalBlockNumberZ; z++)
					if (blocks[x, WaterLevel, z].Type == BlockTypes.Air)
					{
						blocks[x, WaterLevel, z].Type = BlockTypes.Water;
						if (blocks[x, WaterLevel - 1, z].Type == BlockTypes.Air) // level down scan
							blocks[x, WaterLevel - 1, z].Type = BlockTypes.Water;
					}

			PropagateWaterHorizontally(ref blocks, WaterLevel - 1);

			int currentY = WaterLevel - 1;
			bool waterAdded = true;
			while (waterAdded)
			{
				waterAdded = AddWaterBelow(ref blocks, currentY);
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
						if (blocks[x, y, z].Type != BlockTypes.Grass) continue;

						if (IsThereEnoughSpaceForTree(in blocks, x, y, z))
							if (FractalFunc(x, y, z, WoodbaseSmooth, WoodbaseOctaves) < woodbaseProbability)
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

            int logicalProcessorCount = Environment.ProcessorCount;
            var pendingTasks = new List<Task>(World.Settings.WorldSizeX * World.Settings.WorldSizeZ);
            var scheduledTasks = new Task[logicalProcessorCount];

            // schedule one task per chunk
            for (int i = 0, num = 0; i < World.Settings.WorldSizeX; i++)
                for (int j = 0; j < World.Settings.WorldSizeZ; j++)
                {
                    // use variable capture to "pass in" parameters in order to avoid data share
                    // because values changed outside of a task are also changed in the task
                    int iCopy = i;
                    int jCopy = j;

                    // start first 8 (or any processors the target machine has)
                    if (num < logicalProcessorCount)
                    {
                        scheduledTasks[num] = new Task(() => AddTreesInChunkParallel(woodbaseProbability, iCopy, jCopy));
                        scheduledTasks[num].Start();
                    }
                    else
                        pendingTasks.Add(new Task(() => AddTreesInChunkParallel(woodbaseProbability, iCopy, jCopy)));

                    num++;
                }

            // start new task as soon as we have a free thread available
            // and keep on doing that until you reach the end of the array
            do
            {
                int completedId = Task.WaitAny(scheduledTasks);

                if (pendingTasks.Count == 0)
                    break;

                scheduledTasks[completedId] = pendingTasks[0];
                pendingTasks.RemoveAt(0);
                scheduledTasks[completedId].Start();
            }
            while (true);

            Task.WaitAll(scheduledTasks);
        }

        void AddTreesInChunkParallel(float woodbaseProbability, int chunkColumnX, int chunkColumnZ)
        {
            for (int x = 1 + chunkColumnX * World.ChunkSize; x < chunkColumnX * World.ChunkSize + World.ChunkSize - 1; x++)
                // this 20 is hard coded as for now but generally it would be nice if
                // this loop could know in advance where the lowest grass is
                for (int y = 20; y < _totalBlockNumberY - TreeHeight - 1; y++)
                    for (int z = 1 + chunkColumnZ * World.ChunkSize; z < chunkColumnZ * World.ChunkSize + World.ChunkSize - 1; z++)
                    {
                        if (World.Blocks[x, y, z].Type != BlockTypes.Grass)
                            continue;

                        if (IsThereEnoughSpaceForTree(in World.Blocks, x, y, z))
                            if (FractalFunc(x, y, z, WoodbaseSmooth, WoodbaseOctaves) < woodbaseProbability)
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
			BlockTypes type;
			int x, z; // iteration variables

			// z asc
			for (x = 0; x < _totalBlockNumberX; x++)
				for (z = 0; z < _totalBlockNumberZ; z++)
				{
					type = blocks[x, currentY, z].Type;
					if (ChangeToWater())
						blocks[x, currentY, z].Type = BlockTypes.Water;
				}

			// x asc
			for (z = 0; z < _totalBlockNumberZ; z++)
				for (x = 0; x < _totalBlockNumberX; x++)
				{
					type = blocks[x, currentY, z].Type;
					if (ChangeToWater())
						blocks[x, currentY, z].Type = BlockTypes.Water;
				}

			// z desc
			for (x = 0; x < _totalBlockNumberX; x++)
				for (z = _totalBlockNumberZ - 1; z >= 0; z--)
				{
					type = blocks[x, currentY, z].Type;
					if (ChangeToWater())
						blocks[x, currentY, z].Type = BlockTypes.Water;
				}

			// x desc
			for (z = 0; z < _totalBlockNumberZ; z++)
				for (x = _totalBlockNumberX - 1; x >= 0; x--)
				{
					type = blocks[x, currentY, z].Type;
					if (ChangeToWater())
						blocks[x, currentY, z].Type = BlockTypes.Water;
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
					if (type == BlockTypes.Air)
						return true;

					if (type != BlockTypes.Water)
						foundWater = false;
				}
				else if (type == BlockTypes.Water)
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
                        if (blocks[x, currentY, z].Type == BlockTypes.Air)
                        {
                            if (x < _totalBlockNumberX - 1 && blocks[x + 1, currentY, z].Type == BlockTypes.Water) // right
                            {
                                blocks[x, currentY, z].Type = BlockTypes.Water;
                                reiterate = true;
                            }
                            else if (x > 0 && blocks[x - 1, currentY, z].Type == BlockTypes.Water) // left
                            {
                                blocks[x, currentY, z].Type = BlockTypes.Water;
                                reiterate = true;
                            }
                            else if (z < _totalBlockNumberZ - 1 && blocks[x, currentY, z + 1].Type == BlockTypes.Water) // front
                            {
                                blocks[x, currentY, z].Type = BlockTypes.Water;
                                reiterate = true;
                            }
                            else if (z > 0 && blocks[x, currentY, z - 1].Type == BlockTypes.Water) // back
                            {
                                blocks[x, currentY, z].Type = BlockTypes.Water;
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
			bool waterAdded = false;
			for (int x = 0; x < _totalBlockNumberX; x++)
				for (int z = 0; z < _totalBlockNumberZ; z++)
					if (blocks[x, currentY, z].Type == BlockTypes.Water && blocks[x, currentY - 1, z].Type == BlockTypes.Air)
					{
						blocks[x, currentY - 1, z].Type = BlockTypes.Water;
						waterAdded = true;
					}

			return waterAdded;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsThereEnoughSpaceForTree(in BlockData[,,] blocks, int x, int y, int z)
		{
			for (int i = 2; i < TreeHeight; i++)
			{
				if (blocks[x + 1, y + i, z].Type != BlockTypes.Air
					|| blocks[x - 1, y + i, z].Type != BlockTypes.Air
					|| blocks[x, y + i, z + 1].Type != BlockTypes.Air
					|| blocks[x, y + i, z - 1].Type != BlockTypes.Air
					|| blocks[x + 1, y + i, z + 1].Type != BlockTypes.Air
					|| blocks[x + 1, y + i, z - 1].Type != BlockTypes.Air
					|| blocks[x - 1, y + i, z + 1].Type != BlockTypes.Air
					|| blocks[x - 1, y + i, z - 1].Type != BlockTypes.Air)
					return false;
			}

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void BuildTree(ref BlockData[,,] blocks, int x, int y, int z)
		{
			CreateBlock(ref blocks[x, y, z], BlockTypes.Woodbase);
			CreateBlock(ref blocks[x, y + 1, z], BlockTypes.Wood);
			CreateBlock(ref blocks[x, y + 2, z], BlockTypes.Wood);

			int i, j, k;
			for (i = -1; i <= 1; i++)
				for (j = -1; j <= 1; j++)
					for (k = 3; k <= 4; k++)
						CreateBlock(ref blocks[x + i, y + k, z + j], BlockTypes.Leaves);

			CreateBlock(ref blocks[x, y + 5, z], BlockTypes.Leaves);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CreateBlock(ref BlockData block, BlockTypes type)
		{
			block.Type = type;
			block.Hp = LookupTables.BlockHealthMax[(int)type];
		}
	}
}