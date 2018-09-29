using System.Diagnostics;
using UnityEngine;

public class World : MonoBehaviour
{    
    public static Stopwatch sw = new Stopwatch();
    public static int numChunks = 0;
    public static long milisec = 0;

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
    public void GenerateTerrain(SaveGameData saveGame = null)
    {
        sw.Start();

        var terrainGenerator = new TerrainGenerator(ChunkSize);
        Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];
        
        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    var chunkPosition = new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                    var c = new Chunk(chunkPosition, TextureAtlas, FluidTexture, this, new Vector3Int(x, y, z));
                    c.Blocks = saveGame == null 
                        ? terrainGenerator.BuildChunk(chunkPosition) 
                        : saveGame.Chunks[x, y, z].Blocks;
                    
                    Chunks[x, y, z] = c;
                }

        sw.Stop();
        UnityEngine.Debug.Log("It took " + sw.ElapsedMilliseconds + "ms to generate terrain data.");
    }

    /// <summary>
    /// Mesh calculation need to be done after all chunks are generated as each chunk's mesh depends on surronding chunks.
    /// </summary>
    public void CalculateMesh()
    {
        var meshGenerator = new MeshGenerator(ChunkSize, WorldSizeX, WorldSizeY, WorldSizeZ);

        sw.Restart();
        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                    Chunks[x, y, z].CalculateMeshes(
                        meshGenerator,
                        new Vector3(x * ChunkSize, y * ChunkSize, z * ChunkSize));

        sw.Stop();
        UnityEngine.Debug.Log("It took " + sw.ElapsedMilliseconds + "ms to generate mesh data.");
    }
}