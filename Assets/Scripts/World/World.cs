using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum WorldGeneratorStatus { Idle, Generating, Ready }

[CreateAssetMenu]
public class World : ScriptableObject
{
    public TerrainGenerator TerrainGenerator { get; private set; }
    public MeshGenerator MeshGenerator { get; private set; }

    public Chunk[,,] Chunks;
    public Material TerrainTexture;
    public Material WaterTexture;
    
    public byte ChunkSize = 32;
    public byte WorldSizeX = 7;
    public byte WorldSizeY = 4;
    public byte WorldSizeZ = 7;
    
    Stopwatch _stopwatch = new Stopwatch();
    Scene _worldScene;
    long _terrainReadyTime;

    public float ChunkTerrainToGenerate { get; private set; }
    public float ChunkObjectsToGenerate { get; private set; }
    public float AlreadyGenerated { get; private set; }
    public WorldGeneratorStatus Status { get; private set; }

    private void OnEnable()
    {
        TerrainGenerator = new TerrainGenerator(ChunkSize);
        MeshGenerator = new MeshGenerator(ChunkSize, WorldSizeX, WorldSizeY, WorldSizeZ);

        ChunkTerrainToGenerate = WorldSizeX * WorldSizeY * WorldSizeZ;
        ChunkObjectsToGenerate = ChunkTerrainToGenerate * 2; // each chunk has to game objects
        AlreadyGenerated = 0;

        Status = WorldGeneratorStatus.Idle;
    }

    /// <summary>
    /// Generate terrain and meshes.
    /// </summary>
    public IEnumerator GenerateWorld(SaveGameData save = null)
    {
        // === generating terrain ===
        _stopwatch.Start();
        Status = WorldGeneratorStatus.Generating;

        AlreadyGenerated = 0;
        
        Scene scene = SceneManager.GetSceneByName("World");
        if(scene.name != "World") // check if the scene was found
            _worldScene = SceneManager.CreateScene(name);

        Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];

        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    if (save == null)
                    { 
                        var chunkPosition = new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                        var c = new Chunk(chunkPosition, new Vector3Int(x, y, z), this)
                        {
                            Blocks = TerrainGenerator.BuildChunk(chunkPosition)
                        };

                        Chunks[x, y, z] = c;

                        SceneManager.MoveGameObjectToScene(c.Terrain.gameObject, _worldScene);
                        SceneManager.MoveGameObjectToScene(c.Water.gameObject, _worldScene);

                        AlreadyGenerated++;
                        yield return null; // give back control
                    }
                    else
                    {
                        Chunks[x, y, z] = new Chunk(new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize),
                        new Vector3Int(x, y, z), this)
                        {
                            Blocks = save.Chunks[x, y, z].Blocks
                        };

                        AlreadyGenerated++;
                        yield return null; // give back control
                    }
                }

        _stopwatch.Stop();
        _terrainReadyTime = _stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {_terrainReadyTime} ms to generate terrain data.");
        
        // === calculating meshes ===
        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    var c = Chunks[x, y, z];

                    MeshData terrainData, waterData;
                    MeshGenerator.ExtractMeshData(ref c.Blocks, new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize),
                        out terrainData, out waterData);

                    c.CreateTerrainObject(MeshGenerator.CreateMesh(terrainData));
                    c.CreateWaterObject(MeshGenerator.CreateMesh(waterData));

                    c.Status = ChunkStatus.Created;

                    AlreadyGenerated += 2;
                    yield return null; // give back control
                }

        MeshGenerator.LogTimeSpent();
        Chunk.LogTimeSpent();
        Status = WorldGeneratorStatus.Ready;
    }
}
