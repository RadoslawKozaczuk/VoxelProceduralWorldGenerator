using System.Diagnostics;
using UnityEngine;

public class World : MonoBehaviour
{    
    static Stopwatch Stopwatch = new Stopwatch();
    static long TerrainReadyTime, MeshReadyTime;

    public Chunk[,,] Chunks;
    public Transform TerrainParent;
    public Transform WaterParent;
    public Material TextureAtlas;
    public Material FluidTexture;

    public byte ChunkSize = 32;
    public byte WorldSizeX = 7;
    public byte WorldSizeY = 4;
    public byte WorldSizeZ = 7;

    /// <summary>
    /// If storage variable is equal to null terrain will be generated.
    /// Otherwise, read from the storage.
    /// </summary>
    public void GenerateTerrain()
    {
        Stopwatch.Start();
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

        Stopwatch.Stop();
        TerrainReadyTime = Stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {TerrainReadyTime} ms to generate terrain data.");
    }

    /// <summary>
    /// Mesh calculation need to be done after all chunks are generated as each chunk's mesh depends on surronding chunks.
    /// </summary>
    public void CalculateMesh()
    {
        Stopwatch.Restart();
        var meshGenerator = new MeshGenerator(ChunkSize, WorldSizeX, WorldSizeY, WorldSizeZ);
        
        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    var c = Chunks[x, y, z];

                    MeshData terrainData, waterData;
                    meshGenerator.ExtractMeshData(ref c.Blocks, new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize), 
                        out terrainData, out waterData);
                    
                    c.CreateTerrainObject(meshGenerator.CreateMeshFromData(terrainData));
                    c.CreateWaterObject(meshGenerator.CreateMeshFromData(waterData));

                    c.Status = Chunk.ChunkStatus.Created;
                }

        Stopwatch.Stop();
        MeshReadyTime = Stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {MeshReadyTime} ms to generate mesh data.");
    }

    public void LoadTerrain(SaveGameData save)
    {
        Stopwatch.Start();
        Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];

        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                    Chunks[x, y, z] = new Chunk(new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize),
                        TextureAtlas, FluidTexture, this, new Vector3Int(x, y, z))
                    {
                        Blocks = save.Chunks[x, y, z].Blocks
                    };

        Stopwatch.Stop();
        TerrainReadyTime = Stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {TerrainReadyTime} ms to load terrain data.");
    }
} 