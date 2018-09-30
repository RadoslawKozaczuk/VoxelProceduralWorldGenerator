using System.Diagnostics;
using UnityEngine;

public class World : MonoBehaviour
{
    public Chunk[,,] Chunks;
    public Transform TerrainParent;
    public Transform WaterParent;
    public Material TextureAtlas;
    public Material FluidTexture;

    public bool WorldCreated = false;
    public byte ChunkSize = 32;
    public byte WorldSizeX = 7;
    public byte WorldSizeY = 4;
    public byte WorldSizeZ = 7;

    Stopwatch _stopwatch = new Stopwatch();
    long _terrainReadyTime;
    
    void Update()
    {
        if(WorldCreated)
            RedrawChunksIfNecessary();
    }

    void RedrawChunksIfNecessary()
    {
        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    Chunk c = Chunks[x, y, z];
                    if (c.Status == ChunkStatus.NeedToBeRedrawn)
                        c.RecreateMeshAndCollider();
                }
    }

    /// <summary>
    /// If storage variable is equal to null terrain will be generated.
    /// Otherwise, read from the storage.
    /// </summary>
    public void GenerateTerrain()
    {
        _stopwatch.Start();
        var terrainGenerator = new TerrainGenerator(ChunkSize);
        Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];

        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    var chunkPosition = new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                    Chunks[x, y, z] = new Chunk(chunkPosition, TextureAtlas, FluidTexture, this, new Vector3Int(x, y, z))
                    {
                        Blocks = terrainGenerator.BuildChunk(chunkPosition)
                    };
                }

        _stopwatch.Stop();
        _terrainReadyTime = _stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {_terrainReadyTime} ms to generate terrain data.");
    }

    /// <summary>
    /// Mesh calculation need to be done after all chunks are generated as each chunk's mesh depends on surronding chunks.
    /// </summary>
    public void CalculateMesh()
    {
        var meshGenerator = new MeshGenerator(ChunkSize, WorldSizeX, WorldSizeY, WorldSizeZ);

        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    var c = Chunks[x, y, z];

                    MeshData terrainData, waterData;
                    meshGenerator.ExtractMeshData(ref c.Blocks, new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize),
                        out terrainData, out waterData);

                    c.CreateTerrainObject(meshGenerator.CreateMesh(terrainData));
                    c.CreateWaterObject(meshGenerator.CreateMesh(waterData));

                    c.Status = ChunkStatus.Created;
                }

        meshGenerator.LogTimeSpent();
        Chunk.LogTimeSpent();
    }

    public void LoadTerrain(SaveGameData save)
    {
        _stopwatch.Start();
        Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];

        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                    Chunks[x, y, z] = new Chunk(new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize),
                        TextureAtlas, FluidTexture, this, new Vector3Int(x, y, z))
                    {
                        Blocks = save.Chunks[x, y, z].Blocks
                    };

        _stopwatch.Stop();
        _terrainReadyTime = _stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {_terrainReadyTime} ms to load terrain data.");
    }

    public void ProcessBlockHit(Vector3 hitBlock)
    {
        int chunkX = hitBlock.x < 0 ? 0 : (int)(hitBlock.x / ChunkSize);
        int chunkY = hitBlock.y < 0 ? 0 : (int)(hitBlock.y / ChunkSize);
        int chunkZ = hitBlock.z < 0 ? 0 : (int)(hitBlock.z / ChunkSize);
        
        int blockX = (int)hitBlock.x - chunkX * ChunkSize;
        int blockY = (int)hitBlock.y - chunkY * ChunkSize;
        int blockZ = (int)hitBlock.z - chunkZ * ChunkSize;

        // inform chunk
        var wasBlockDestroyed = Chunks[chunkX, chunkY, chunkZ].BlockHit(blockX, blockY, blockZ);
        
        if (wasBlockDestroyed)
            CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
    }

    /// <summary>
    /// Check if neighboring chunks need to be redrawn and change their status if necessary.
    /// </summary>
    void CheckNeighboringChunks(int blockX, int blockY, int blockZ, int chunkX, int chunkY, int chunkZ)
    {
        // right check
        if (blockX == ChunkSize - 1 && chunkX + 1 < WorldSizeX)
            Chunks[chunkX + 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // BUG: there are sometimes faces not beinf rendered on this axis - dunno why
        // left check
        if (blockX == 0 && chunkX - 1 >= 0)
            Chunks[chunkX - 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // top check
        if (blockY == ChunkSize - 1 && chunkY + 1 < WorldSizeY)
            Chunks[chunkX, chunkY + 1, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // bottom check
        if (blockY == 0 && chunkY - 1 >= 0)
            Chunks[chunkX, chunkY - 1, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // front check
        if (blockZ == ChunkSize - 1 && chunkZ + 1 < WorldSizeZ)
            Chunks[chunkX, chunkY, chunkZ + 1].Status = ChunkStatus.NeedToBeRedrawn;

        // back check
        if (blockZ == 0 && chunkZ - 1 >= 0)
            Chunks[chunkX, chunkY, chunkZ - 1].Status = ChunkStatus.NeedToBeRedrawn;
    }
}