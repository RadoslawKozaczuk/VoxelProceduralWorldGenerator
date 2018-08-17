using Assets.Scripts;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TerrainGenerator
{
    struct BlockTypeJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> Indexes;
        [ReadOnly]
        public float ChunkPosX;
        [ReadOnly]
        public float ChunkPosY;
        [ReadOnly]
        public float ChunkPosZ;
        [ReadOnly]
        public int ChunkSize;

        // result
        public NativeArray<BlockType> Result;

        public void Execute(int i)
        {
            // deflattenization - extract coords from the index
            var index = Indexes[i];
            var z = index / (ChunkSize * ChunkSize);
            index -= z * ChunkSize * ChunkSize;

            var y = index / ChunkSize;
            index -= y * ChunkSize;

            var x = index;

            Result[i] = DetermineType((int)(x + ChunkPosX), (int)(y + ChunkPosY), (int)(z + ChunkPosZ));
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

    // parallelism
    static BlockTypeJob _typeJob;
    static JobHandle _typeJobHandle;
    public static readonly int[] TypeJobIndexes = InitializeJobIndexes();
    public static int[] InitializeJobIndexes()
    {
        var size = World.ChunkSize * World.ChunkSize * World.ChunkSize;
        var table = new int[size];
        for (int i = 0; i < size; i++)
            table[i] = i;
        return table;
    }

    public static BlockData[,,] BuildChunk(Vector3 chunkPosition)
    {
        //bool dataFromFile = Load();
        var blocks = new BlockData[World.ChunkSize, World.ChunkSize, World.ChunkSize];

        // output data
        var types = new BlockType[World.ChunkSize * World.ChunkSize * World.ChunkSize];

        _typeJob = new BlockTypeJob()
        {
            // input data
            ChunkPosX = chunkPosition.x,
            ChunkPosY = chunkPosition.y,
            ChunkPosZ = chunkPosition.z,
            ChunkSize = World.ChunkSize,
            Indexes = new NativeArray<int>(TypeJobIndexes, Allocator.TempJob),
            Result = new NativeArray<BlockType>(types, Allocator.TempJob)
        };

        // schedule jobs (as many as TypeJobIndexes.Length)
        _typeJobHandle = _typeJob.Schedule(TypeJobIndexes.Length, 5);
        _typeJobHandle.Complete();

        _typeJob.Result.CopyTo(types);

        // clean up
        _typeJob.Indexes.Dispose();
        _typeJob.Result.Dispose();

        for (var z = 0; z < World.ChunkSize; z++)
            for (var y = 0; y < World.ChunkSize; y++)
                for (var x = 0; x < World.ChunkSize; x++)
                {
                    var pos = new Vector3(x, y, z);
                    var type = types[x + y * World.ChunkSize + z * World.ChunkSize * World.ChunkSize];

                    blocks[x, y, z] = new BlockData { Type = type };
                }

        // I will add it later
        AddTrees(ref blocks, chunkPosition);

        // chunk just has been created and it is ready to be drawn
        return blocks;
    }
    
    static void AddTrees(ref BlockData[,,] blocks, Vector3 chunkPosition)
    {
        for (var z = 1; z < World.ChunkSize - 1; z++)
            // trees cannot grow on chunk edges (x, y cannot be 0 or ChunkSize) 
            // simplification - that's because chunks are created in isolation
            // so we cannot put leafes in another chunk
            for (var y = 0; y < World.ChunkSize - TreeHeight; y++)
                for (var x = 1; x < World.ChunkSize - 1; x++)
                {
                    if (blocks[x, y, z].Type != BlockType.Grass) continue;

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

    static bool IsThereEnoughSpaceForTree(ref BlockData[,,] blocks, int x, int y, int z)
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

    static void BuildTree(ref BlockData[,,] blocks, int x, int y, int z)
    {
        blocks[x, y, z].Type = BlockType.Woodbase;
        blocks[x, y + 1, z].Type = BlockType.Wood;
        blocks[x, y + 2, z].Type = BlockType.Wood;

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
                for (int k = 3; k <= 4; k++)
                    blocks[x + i, y + k, z + j].Type = BlockType.Leaves;
        blocks[x, y + 5, z].Type = BlockType.Leaves;
    }

    public static BlockType DetermineType(int worldX, int worldY, int worldZ)
    {
        BlockType type;

        if (worldY <= GenerateBedrockHeight(worldX, worldZ))
            type = BlockType.Bedrock;
        else if (worldY <= GenerateStoneHeight(worldX, worldZ))
        {
            if (FractalFunc(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability && worldY < DiamondMaxHeight)
                type = BlockType.Diamond;
            else if (FractalFunc(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability && worldY < RedstoneMaxHeight)
                type = BlockType.Redstone;
            else
                type = BlockType.Stone;
        }
        else if (worldY == GenerateHeight(worldX, worldZ))
            type = BlockType.Grass;
        else if (worldY <= GenerateHeight(worldX, worldZ))
            type = BlockType.Dirt;
        else if (worldY <= WaterLevel)
            type = BlockType.Water;
        else
            type = BlockType.Air;

        // generate caves
        if (type != BlockType.Water && FractalFunc(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
            type = BlockType.Air;

        return type;
    }
    
    static int GenerateBedrockHeight(float x, float z)
    {
        float height = Map(0, MaxHeightBedrock, 0, 1, FractalBrownianMotion(x * SmoothBedrock, z * SmoothBedrock, OctavesBedrock, PersistenceBedrock));
        return (int)height;
    }

    static int GenerateStoneHeight(float x, float z)
    {
        float height = Map(0, MaxHeightStone, 0, 1, FractalBrownianMotion(x * SmoothStone, z * SmoothStone, OctavesStone, PersistenceStone));
        return (int)height;
    }

    public static int GenerateHeight(float x, float z)
    {
        float height = Map(0, MaxHeight, 0, 1, FractalBrownianMotion(x * Smooth, z * Smooth, Octaves, Persistence));
        return (int)height;
    }

    static float Map(float newmin, float newmax, float origmin, float origmax, float value)
    {
        return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));
    }

    // good noise generator
    // persistence - if < 1 each function is less powerful than the previous one, for > 1 each is more important
    // octaves - number of functions that we sum up
    static float FractalBrownianMotion(float x, float z, int oct, float pers)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

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
}
