using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TerrainGenerator
{
    struct HeightData
    {
        public float Bedrock, Stone, Dirt;
    }

    struct HeightJob : IJobParallelFor
    {
        [ReadOnly]
        public int ChunkPosX;
        [ReadOnly]
        public int ChunkPosZ;
        [ReadOnly]
        public int ChunkSize;

        // result
        public NativeArray<HeightData> Result;

        public void Execute(int i)
        {
            // deflattenization - extract coords from the index
            var index = i;
            var z = index / ChunkSize;
            index -= z * ChunkSize;
            var x = index;

            Result[i] = new HeightData()
            {
                Bedrock = GenerateBedrockHeight(ChunkPosX + x, ChunkPosZ + z),
                Stone = GenerateStoneHeight(ChunkPosX + x, ChunkPosZ + z),
                Dirt = GenerateDirtHeight(ChunkPosX + x, ChunkPosZ + z)
            };
        }
    }

    struct BlockTypeJob : IJobParallelFor
    {
        [ReadOnly]
        public int ChunkPosX;
        [ReadOnly]
        public int ChunkPosY;
        [ReadOnly]
        public int ChunkPosZ;
        [ReadOnly]
        public int ChunkSize;
        [ReadOnly]
        public NativeArray<HeightData> Heights;

        // result
        public NativeArray<BlockTypes> Result;

        public void Execute(int i)
        {
            // deflattenization - extract coords from the index
            var index = i;
            var z = index / (ChunkSize * ChunkSize);
            index -= z * ChunkSize * ChunkSize;

            var y = index / ChunkSize;
            index -= y * ChunkSize;

            var x = index;

            Result[i] = DetermineType(x + ChunkPosX, y + ChunkPosY, z + ChunkPosZ, x, z, ChunkSize, Heights);
        }
    }
    
    #region Constants
    // caves should be more erratic so has to be a higher number
    const float CaveProbability = 0.43f;
    const float CaveSmooth = 0.09f;
    const int CaveOctaves = 3; // reduced a bit to lower workload but not to much to maintain randomness
    const int WaterLevel = 65; // inclusive

    // shiny diamonds!
    const float DiamondProbability = 0.38f; // this is not percentage chance because we are using Perlin function
    const float DiamondSmooth = 0.06f;
    const int DiamondOctaves = 3;
    const int DiamondMaxHeight = 50;

    // red stones
    const float RedstoneProbability = 0.41f;
    const float RedstoneSmooth = 0.06f;
    const int RedstoneOctaves = 3;
    const int RedstoneMaxHeight = 30;

    // woodbase
    const float WoodbaseProbability = 0.40f;
    const float WoodbaseSmooth = 0.4f;
    const int WoodbaseOctaves = 2;
    const int TreeHeight = 7;

    const int MaxHeight = 150;
    const float Smooth = 0.01f; // bigger number increases sampling of the function
    const int Octaves = 4;
    const float Persistence = 0.5f;

    const int MaxHeightStone = 145;
    const float SmoothStone = 0.02f;
    const int OctavesStone = 5;
    const float PersistenceStone = 0.75f;

    const int MaxHeightBedrock = 20;
    const float SmoothBedrock = 0.01f;
    const int OctavesBedrock = 2;
    const float PersistenceBedrock = 0.5f;
    #endregion

    readonly int _chunkSize;

    public TerrainGenerator(int chunkSize)
    {
        _chunkSize = chunkSize;
    }

    public BlockData[,,] BuildChunk(Vector3Int chunkPosition)
    {
        var blocks = new BlockData[_chunkSize, _chunkSize, _chunkSize];

        var heights = CalculateHeights(chunkPosition);
        CalculateBlockTypes(ref blocks, chunkPosition, heights);
        AddTrees(ref blocks, chunkPosition);

        return blocks;
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

    static BlockTypes DetermineType(int worldX, int worldY, int worldZ, int localX, int localZ,
        int chunkSize, NativeArray<HeightData> heights)
    {
        BlockTypes type;

        if (worldY <= heights[localX + localZ * chunkSize].Bedrock)
            type = BlockTypes.Bedrock;
        else if (worldY <= heights[localX + localZ * chunkSize].Stone)
        {
            if (FractalFunc(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability
                && worldY < DiamondMaxHeight)
                type = BlockTypes.Diamond;
            else if (FractalFunc(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability
                && worldY < RedstoneMaxHeight)
                type = BlockTypes.Redstone;
            else
                type = BlockTypes.Stone;
        }
        else
        {
            var height = heights[localX + localZ * chunkSize].Dirt;
            if (worldY == height)
                type = BlockTypes.Grass;
            else if (worldY < height)
                type = BlockTypes.Dirt;
            else if (worldY <= WaterLevel)
                type = BlockTypes.Water;
            else
                type = BlockTypes.Air;
        }

        // BUG: caves are sometimes generated under or next to water 
        // generate caves
        if (type != BlockTypes.Water && FractalFunc(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
            type = BlockTypes.Air;

        return type;
    }

    // good noise generator
    // persistence - if < 1 each function is less powerful than the previous one, for > 1 each is more important
    // octaves - number of functions that we sum up
    static float FractalBrownianMotion(float x, float z, int oct, float pers)
    {
        float total = 0, frequency = 1, amplitude = 1, maxValue = 0;

        // Perlin function value of x is equal to its value of -x. Same for y.
        // to avoid it we need an offset, quite large one to be sure.
        const float offset = 32000f;

        for (int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise((x + offset) * frequency, (z + offset) * frequency) * amplitude;

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
    HeightData[] CalculateHeights(Vector3Int chunkPosition)
    {
        // output data
        var heights = new HeightData[_chunkSize * _chunkSize];

        var heightJob = new HeightJob()
        {
            // input
            ChunkPosX = chunkPosition.x,
            ChunkPosZ = chunkPosition.z,
            ChunkSize = _chunkSize,

            // output
            Result = new NativeArray<HeightData>(heights, Allocator.TempJob)
        };

        var heightJobHandle = heightJob.Schedule(_chunkSize * _chunkSize, 8);
        heightJobHandle.Complete();
        heightJob.Result.CopyTo(heights);

        // cleanup
        heightJob.Result.Dispose();

        return heights;
    }
    
    void CalculateBlockTypes(ref BlockData[,,] blocks, Vector3Int chunkPosition, HeightData[] heights)
    {
        // output data
        var types = new BlockTypes[_chunkSize * _chunkSize * _chunkSize];

        var typeJob = new BlockTypeJob()
        {
            // input
            ChunkPosX = chunkPosition.x,
            ChunkPosY = chunkPosition.y,
            ChunkPosZ = chunkPosition.z,
            ChunkSize = _chunkSize,
            Heights = new NativeArray<HeightData>(heights, Allocator.TempJob),

            // output
            Result = new NativeArray<BlockTypes>(types, Allocator.TempJob)
        };

        var typeJobHandle = typeJob.Schedule(_chunkSize * _chunkSize * _chunkSize, 8);
        typeJobHandle.Complete();
        typeJob.Result.CopyTo(types);

        // cleanup
        typeJob.Result.Dispose();
        typeJob.Heights.Dispose();

        // output deflattenization
        for (var z = 0; z < _chunkSize; z++)
            for (var y = 0; y < _chunkSize; y++)
                for (var x = 0; x < _chunkSize; x++)
                {
                    var type = types[x + y * _chunkSize + z * _chunkSize * _chunkSize];
                    blocks[x, y, z].Type = type;
                    blocks[x, y, z].Hp = LookupTables.BlockHealthMax[(int)type];
                }
    }
    
    void AddTrees(ref BlockData[,,] blocks, Vector3 chunkPosition)
    {
        for (var z = 1; z < _chunkSize - 1; z++)
            // trees cannot grow on chunk edges (x, y cannot be 0 or ChunkSize) 
            // simplification - that's because chunks are created in isolation
            // so we cannot put leafes in another chunk
            for (var y = 0; y < _chunkSize - TreeHeight; y++)
                for (var x = 1; x < _chunkSize - 1; x++)
                {
                    if (blocks[x, y, z].Type != BlockTypes.Grass) continue;

                    if (IsThereEnoughSpaceForTree(ref blocks, x, y, z))
                    {
                        int worldX = (int)(x + chunkPosition.x);
                        int worldY = (int)(y + chunkPosition.y);
                        int worldZ = (int)(z + chunkPosition.z);

                        if (FractalFunc(worldX, worldY, worldZ, WoodbaseSmooth, WoodbaseOctaves) < WoodbaseProbability)
                        {
                            BuildTree(ref blocks, x, y, z);
                            x += 2; // no trees can be that close
                        }
                    }
                }
    }

    bool IsThereEnoughSpaceForTree(ref BlockData[,,] blocks, int x, int y, int z)
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

    void BuildTree(ref BlockData[,,] blocks, int x, int y, int z)
    {
        CreateBlock(ref blocks[x, y, z], BlockTypes.Woodbase);
        CreateBlock(ref blocks[x, y + 1, z], BlockTypes.Wood);
        CreateBlock(ref blocks[x, y + 2, z], BlockTypes.Wood);

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
                for (int k = 3; k <= 4; k++)
                    CreateBlock(ref blocks[x + i, y + k, z + j], BlockTypes.Leaves);

        CreateBlock(ref blocks[x, y + 5, z], BlockTypes.Leaves);
    }

    void CreateBlock(ref BlockData block, BlockTypes type)
    {
        block.Type = type;
        block.Hp = LookupTables.BlockHealthMax[(int)type];
    }
}
