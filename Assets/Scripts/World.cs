using System.Diagnostics;
using UnityEngine;

public class World : MonoBehaviour
{
    public Chunk[,,] Chunks;
    public Transform TerrainParent;
    public Transform WaterParent;
    public Material TextureAtlas;
    public Material FluidTexture;

    public byte ChunkSize = 32;
    public byte WorldSizeX = 7;
    public byte WorldSizeY = 4;
    public byte WorldSizeZ = 7;

    Stopwatch _stopwatch = new Stopwatch();
    long _terrainReadyTime;

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

                    c.Status = Chunk.ChunkStatus.Created;
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
}