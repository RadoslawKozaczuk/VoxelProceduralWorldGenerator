using System;
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

    static long _accumulatedTerrainObjectCreationTime, _accumulatedWaterObjectCreationTime;

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
    /// createObjects equals
    /// </summary>
    public IEnumerator GenerateWorld(bool firstRun = false, SaveGameData save = null)
    {
        // === generating terrain ===
        _stopwatch.Start();
        Status = WorldGeneratorStatus.Generating;

        AlreadyGenerated = 0;

        Scene scene = SceneManager.GetSceneByName("World");
        if (scene.name != "World") // check if the scene was found
            _worldScene = SceneManager.CreateScene(name);

        if (save == null)
        {
            Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];

            for (int x = 0; x < WorldSizeX; x++)
                for (int z = 0; z < WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                    {
                        var chunkPosition = new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize);

                        var c = new Chunk()
                        {
                            Blocks = TerrainGenerator.BuildChunk(chunkPosition),
                            Coord = new Vector3Int(x, y, z)
                        };

                        if (firstRun)
                        {
                            string name = "" + chunkPosition.x + chunkPosition.y + chunkPosition.z;
                            c.Terrain = new GameObject(name + "_terrain");
                            c.Terrain.transform.position = chunkPosition;
                            c.Water = new GameObject(name + "_water");
                            c.Water.transform.position = chunkPosition;
                            c.Status = ChunkStatus.NeedToBeRedrawn;

                            SceneManager.MoveGameObjectToScene(c.Terrain.gameObject, _worldScene);
                            SceneManager.MoveGameObjectToScene(c.Water.gameObject, _worldScene);
                        }

                        Chunks[x, y, z] = c;

                        AlreadyGenerated++;
                        yield return null; // give back control
                    }
        }
        else
        {
            for (int x = 0; x < WorldSizeX; x++)
                for (int z = 0; z < WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                    {
                        var loaded = save.Chunks[x, y, z];
                        var c = new Chunk()
                        {
                            Blocks = loaded.Blocks,
                            Coord = loaded.Coord,
                            Status = ChunkStatus.NeedToBeRedrawn
                        };

                        AlreadyGenerated++;
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
                    _stopwatch.Restart();

                    var c = Chunks[x, y, z];
                    MeshData terrainData, waterData;
                    MeshGenerator.ExtractMeshData(ref c.Blocks, new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize),
                        out terrainData, out waterData);
                    CreateRenderingComponents(c, terrainData, waterData);
                    c.Status = ChunkStatus.Created;

                    AlreadyGenerated += 2;
                    _stopwatch.Stop();
                    _accumulatedWaterObjectCreationTime += _stopwatch.ElapsedTicks;

                    yield return null; // give back control
                }

        MeshGenerator.LogTimeSpent();
        LogTimeSpent();
        Status = WorldGeneratorStatus.Ready;
    }

    void CreateRenderingComponents(Chunk chunk, MeshData terrainData, MeshData waterData)
    {
        var meshT = MeshGenerator.CreateMesh(terrainData);
        var rt = chunk.Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        rt.material = TerrainTexture;

        var mft = (MeshFilter)chunk.Terrain.AddComponent(typeof(MeshFilter));
        mft.mesh = meshT;

        var ct = chunk.Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        ct.sharedMesh = meshT;

        var meshW = MeshGenerator.CreateMesh(waterData);
        var rw = chunk.Water.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        rw.material = WaterTexture;

        var mfw = (MeshFilter)chunk.Water.AddComponent(typeof(MeshFilter));
        mfw.mesh = meshW;
    }

    public static void LogTimeSpent()
    {
        UnityEngine.Debug.Log("It took "
            + _accumulatedTerrainObjectCreationTime / TimeSpan.TicksPerMillisecond
            + " ms to create all terrain objects.");

        UnityEngine.Debug.Log("It took "
            + _accumulatedWaterObjectCreationTime / TimeSpan.TicksPerMillisecond
            + " ms to create all water objects.");
    }
}
