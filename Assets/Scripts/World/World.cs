using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class World : ScriptableObject
{
    public TerrainGenerator TerrainGenerator { get; private set; }
    public MeshGenerator MeshGenerator { get; private set; }
    public Chunk[,,] Chunks;
    public byte ChunkSize = 32;
    public byte WorldSizeX = 7;
    public byte WorldSizeY = 4;
    public byte WorldSizeZ = 7;
    public float ChunkTerrainToGenerate { get; private set; }
    public float ChunkObjectsToGenerate { get; private set; }
    public float AlreadyGenerated { get; private set; }
    public WorldGeneratorStatus Status { get; private set; }

    [SerializeField] Material _terrainTexture;
    [SerializeField] Material _waterTexture;

    Stopwatch _stopwatch = new Stopwatch();
    Scene _worldScene;
    long _terrainReadyTime;
    
    long _accumulatedTerrainGenerationTime, _accumulatedMeshCreationTime;

    void OnEnable()
    {
        TerrainGenerator = new TerrainGenerator(ChunkSize);
        MeshGenerator = new MeshGenerator(ChunkSize, WorldSizeX, WorldSizeY, WorldSizeZ);

        ChunkTerrainToGenerate = WorldSizeX * WorldSizeY * WorldSizeZ;
        ChunkObjectsToGenerate = ChunkTerrainToGenerate; // each chunk has to game objects
        AlreadyGenerated = 0;

        Status = WorldGeneratorStatus.Idle;
    }

    public IEnumerator GenerateWorld(bool firstRun)
    {
        _accumulatedTerrainGenerationTime = 0;
        _stopwatch.Restart();
        Status = WorldGeneratorStatus.GeneratingTerrain;
        AlreadyGenerated = 0;

        Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];

        if (firstRun)
            _worldScene = SceneManager.CreateScene(name);
        
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
                        CreateGameObjects(c, chunkPosition);

                        SceneManager.MoveGameObjectToScene(c.Terrain.gameObject, _worldScene);
                        SceneManager.MoveGameObjectToScene(c.Water.gameObject, _worldScene);
                    }

                    Chunks[x, y, z] = c;
                    AlreadyGenerated++;
                    
                    yield return null; // give back control
                }

        Status = WorldGeneratorStatus.TerrainReady;
        _stopwatch.Stop();
        _accumulatedTerrainGenerationTime += _stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {_accumulatedTerrainGenerationTime} ms to generate all terrain.");
    }

    public IEnumerator RedrawChunksIfNecessaryAsync()
    {
        _stopwatch.Restart();
        Status = WorldGeneratorStatus.GeneratingMeshes;

        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    Chunk c = Chunks[x, y, z];
                    if (c.Status == ChunkStatus.NeedToBeRecreated)
                        RecreateMeshAndCollider(c);
                    else if (c.Status == ChunkStatus.NeedToBeRedrawn) // used only for cracks
                        RecreateTerrainMesh(c);

                    AlreadyGenerated++;

                    yield return null; // give back control
                }
        
        Status = WorldGeneratorStatus.AllReady;
        _stopwatch.Stop();
        _accumulatedMeshCreationTime += _stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {_accumulatedTerrainGenerationTime} ms to redraw all meshes.");
    }

    public void RedrawChunksIfNecessary()
    {
        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    Chunk c = Chunks[x, y, z];
                    if (c.Status == ChunkStatus.NeedToBeRecreated)
                        RecreateMeshAndCollider(c);
                    else if (c.Status == ChunkStatus.NeedToBeRedrawn) // used only for cracks
                        RecreateTerrainMesh(c);
                }
    }

    public IEnumerator LoadWorld(SaveGameData save, bool firstRun)
    {
        _accumulatedTerrainGenerationTime = 0;
        _stopwatch.Restart();
        Status = WorldGeneratorStatus.GeneratingTerrain;
        AlreadyGenerated = 0;

        if (firstRun)
            _worldScene = SceneManager.CreateScene(name);

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

                    if (firstRun)
                    {
                        var chunkPosition = new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                        CreateGameObjects(c, chunkPosition);

                        SceneManager.MoveGameObjectToScene(c.Terrain.gameObject, _worldScene);
                        SceneManager.MoveGameObjectToScene(c.Water.gameObject, _worldScene);
                    }

                    AlreadyGenerated++;

                    yield return null; // give back control
                }

        Status = WorldGeneratorStatus.TerrainReady;
        _stopwatch.Stop();
        _accumulatedTerrainGenerationTime += _stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"It took {_accumulatedTerrainGenerationTime} ms to load all terrain.");
    }

    public IEnumerator GenerateMeshes()
    {
        _accumulatedMeshCreationTime = 0;
        _stopwatch.Restart();
        Status = WorldGeneratorStatus.GeneratingMeshes;

        for (int x = 0; x < WorldSizeX; x++)
            for (int z = 0; z < WorldSizeZ; z++)
                for (int y = 0; y < WorldSizeY; y++)
                {
                    var c = Chunks[x, y, z];
                    MeshData terrainData, waterData;
                    MeshGenerator.ExtractMeshData(ref c.Blocks, new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize),
                        out terrainData, out waterData);
                    CreateRenderingComponents(c, terrainData, waterData);
                    c.Status = ChunkStatus.Created;

                    AlreadyGenerated++;

                    yield return null; // give back control
                }

        Status = WorldGeneratorStatus.AllReady;
        _stopwatch.Stop();
        _accumulatedMeshCreationTime += _stopwatch.ElapsedMilliseconds;

        UnityEngine.Debug.Log("It took "
            + _accumulatedMeshCreationTime
            + " ms to create all meshes.");
    }

    void RecreateMeshAndCollider(Chunk c)
    {
        DestroyImmediate(c.Terrain.GetComponent<Collider>());

        MeshData t, w;
        MeshGenerator.ExtractMeshData(ref c.Blocks, c.Coord, out t, out w);
        var tm = MeshGenerator.CreateMesh(t);
        var wm = MeshGenerator.CreateMesh(w);

        var terrainFilter = c.Terrain.GetComponent<MeshFilter>();
        terrainFilter.mesh = tm;
        var collider = c.Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        collider.sharedMesh = tm;

        var waterFilter = c.Water.GetComponent<MeshFilter>();
        waterFilter.mesh = wm;

        c.Status = ChunkStatus.Created;
    }

    /// <summary>
    /// Destroys terrain mesh and recreates it.
    /// Used for cracks as they do not change the terrain geometry.
    /// </summary>
    void RecreateTerrainMesh(Chunk c)
    {
        MeshData t, w;
        MeshGenerator.ExtractMeshData(ref c.Blocks, c.Coord, out t, out w);
        var tm = MeshGenerator.CreateMesh(t);

        var meshFilter = c.Terrain.GetComponent<MeshFilter>();
        meshFilter.mesh = tm;

        c.Status = ChunkStatus.Created;
    }
    
    void CreateGameObjects(Chunk c, Vector3Int chunkPosition)
    {
        string name = "" + chunkPosition.x + chunkPosition.y + chunkPosition.z;
        c.Terrain = new GameObject(name + "_terrain");
        c.Terrain.transform.position = chunkPosition;
        c.Water = new GameObject(name + "_water");
        c.Water.transform.position = chunkPosition;
        c.Status = ChunkStatus.NeedToBeRedrawn;
    }
    
    void CreateRenderingComponents(Chunk chunk, MeshData terrainData, MeshData waterData)
    {
        var meshT = MeshGenerator.CreateMesh(terrainData);
        var rt = chunk.Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        rt.material = _terrainTexture;

        var mft = (MeshFilter)chunk.Terrain.AddComponent(typeof(MeshFilter));
        mft.mesh = meshT;

        var ct = chunk.Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        ct.sharedMesh = meshT;

        var meshW = MeshGenerator.CreateMesh(waterData);
        var rw = chunk.Water.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        rw.material = _waterTexture;

        var mfw = (MeshFilter)chunk.Water.AddComponent(typeof(MeshFilter));
        mfw.mesh = meshW;
    }
}
