using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
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

		public static int WaterLevel; // inclusive
		public static float SeedValue;

		readonly int _worldSizeX, _worldSizeZ, _totalBlockNumberX, _totalBlockNumberY, _totalBlockNumberZ;

		// Perlin function value of x is equal to its value of -x. Same for y.
		// To avoid it we need an offset, quite large one to be sure.
		public TerrainGenerator(GameSettings options)
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

		public static float Map(float newmin, float newmax, float origmin, float origmax, float value) =>
			Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));

		public static BlockTypes DetermineType(int worldX, int worldY, int worldZ, HeightData height)
		{
			if (worldY == 0) return BlockTypes.Bedrock;

			// check if this suppose to be a cave
			if (FractalFunc(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
				return BlockTypes.Air;

			// bedrock
			if (worldY <= height.Bedrock) return BlockTypes.Bedrock;

			// stone
			if (worldY <= height.Stone)
			{
				if (FractalFunc(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability
					&& worldY < DiamondMaxHeight)
					return BlockTypes.Diamond;

				if (FractalFunc(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability
					&& worldY < RedstoneMaxHeight)
					return BlockTypes.Redstone;

				return BlockTypes.Stone;
			}

			// dirt
			if (worldY == height.Dirt) return BlockTypes.Grass;
			if (worldY < height.Dirt) return BlockTypes.Dirt;

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

		// calculate global Heights
		public HeightData[] CalculateHeights()
		{
			// output data
			var heights = new HeightData[_totalBlockNumberX * _totalBlockNumberZ];

			var heightJob = new HeightJob()
			{
				// input
				TotalBlockNumberX = _totalBlockNumberX,

				// output
				Result = new NativeArray<HeightData>(heights, Allocator.TempJob)
			};

			var heightJobHandle = heightJob.Schedule(_totalBlockNumberX * _totalBlockNumberZ, 8);
			heightJobHandle.Complete();
			heightJob.Result.CopyTo(heights);

			// cleanup
			heightJob.Result.Dispose();

			return heights;
		}

		public BlockTypes[] CalculateBlockTypes(HeightData[] heights)
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
				Heights = new NativeArray<HeightData>(heights, Allocator.TempJob),

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
		/// If treeProb parameter is set to TreeProbability. None = no trees will be added.
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
				// this loop could know in advance where is the lowest grass
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