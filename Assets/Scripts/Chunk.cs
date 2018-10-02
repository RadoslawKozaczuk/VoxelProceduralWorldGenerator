using System;
using System.Diagnostics;
using UnityEngine;

public class Chunk
{
    static Stopwatch _stopwatch = new Stopwatch();
    static long _accumulatedTerrainObjectCreationTime, _accumulatedWaterObjectCreationTime;
    
    public BlockData[,,] Blocks;
    public Vector3Int Coord;
    public ChunkStatus Status;

    public GameObject Terrain;
    public GameObject Water;
    World _world;

    public Chunk(Vector3 position, Vector3Int coord, World world)
    {
        Coord = coord;
        _world = world;
        
        string name = "" + coord.x + coord.y + coord.z;

        Terrain = new GameObject(name + "_terrain");
        Terrain.transform.position = position;

        Water = new GameObject(name + "_water");
        Water.transform.position = position;
        
        Status = ChunkStatus.NotInitialized;
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
    
    public void RecreateMeshAndCollider()
    {
        DestroyMeshesAndColliders();

        var meshGenerator = new MeshGenerator(_world.ChunkSize, _world.WorldSizeX, _world.WorldSizeY, _world.WorldSizeZ);

        MeshData t, w;
        meshGenerator.ExtractMeshData(ref Blocks, Coord, out t, out w);
        var tm = meshGenerator.CreateMesh(t);
        var wm = meshGenerator.CreateMesh(w);

        CreateTerrainObject(tm);
        CreateWaterObject(wm);

        Status = ChunkStatus.Created;
    }

    /// <summary>
    /// Destroys terrain mesh and recreates it.
    /// Used for cracks as they do not change the terrain geometry.
    /// </summary>
    public void RecreateTerrainMesh()
    {
        DestroyTerrainMesh();

        var meshGenerator = new MeshGenerator(_world.ChunkSize, _world.WorldSizeX, _world.WorldSizeY, _world.WorldSizeZ);

        MeshData t, w;
        meshGenerator.ExtractMeshData(ref Blocks, Coord, out t, out w);
        var tm = meshGenerator.CreateMesh(t);

        CreateTerrainMesh(tm);

        Status = ChunkStatus.Created;
    }

    /// <summary>
    /// Destroys Meshes and Colliders
    /// </summary>
    public void DestroyMeshesAndColliders()
    {
        // we cannot use normal destroy because it may wait to the next update loop or something which will break the code
        UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<MeshFilter>());
        UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<MeshRenderer>());
        UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<Collider>());
        UnityEngine.Object.DestroyImmediate(Water.GetComponent<MeshFilter>());
        UnityEngine.Object.DestroyImmediate(Water.GetComponent<MeshRenderer>());
    }

    public void DestroyTerrainMesh()
    {
        UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<MeshFilter>());
        UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<MeshRenderer>());
    }

    public void CreateTerrainMesh(Mesh mesh)
    {
        var renderer = Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = _world.TerrainTexture;

        var meshFilter = (MeshFilter)Terrain.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;
    }
    
    public void CreateTerrainObject(Mesh mesh)
    {
        _stopwatch.Restart();

        var renderer = Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = _world.TerrainTexture;

        var meshFilter = (MeshFilter)Terrain.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;
        
        var collider = Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        collider.sharedMesh = mesh;
        
        _stopwatch.Stop();
        _accumulatedTerrainObjectCreationTime += _stopwatch.ElapsedTicks;
    }

    public void CreateWaterObject(Mesh mesh)
    {
        _stopwatch.Restart();

        var renderer = Water.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = _world.WaterTexture;

        var meshFilter = (MeshFilter)Water.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;

        _stopwatch.Stop();
        _accumulatedWaterObjectCreationTime += _stopwatch.ElapsedTicks;
    }
    
    /// <summary>
    /// Returns true if the block has been destroyed.
    /// </summary>
    public bool BlockHit(int x, int y, int z)
    {
        var retVal = false;

        byte previousHpLevel = Blocks[x, y, z].HealthLevel;
        Blocks[x, y, z].Hp--;
        byte currentHpLevel = CalculateHealthLevel(
            Blocks[x, y, z].Hp, 
            LookupTables.BlockHealthMax[(int)Blocks[x, y, z].Type]);

        if (currentHpLevel != previousHpLevel)
        {
            Blocks[x, y, z].HealthLevel = currentHpLevel;

            if (Blocks[x, y, z].Hp == 0)
            {
                Blocks[x, y, z].Type = BlockTypes.Air;
                retVal = true;
            }

            // TODO: for now lets simply redraw whole chunk to see if it works
            // this is rather expensive and maybe I should look for another solution
            Status = ChunkStatus.NeedToBeRedrawn;
        }

        return retVal;
    }

    /// <summary>
    /// Returns true if a new block has been built.
    /// </summary>
    public bool BuildBlock(int x, int y, int z, BlockTypes type)
    {
        if (Blocks[x, y, z].Type != BlockTypes.Air) return false;

        Blocks[x, y, z].Type = type;
        Blocks[x, y, z].Hp = LookupTables.BlockHealthMax[(int)type];
        Blocks[x, y, z].HealthLevel = 0;
        
        // TODO: for now lets simply redraw whole chunk to see if it works
        // this is rather expensive and maybe I should look for another solution
        Status = ChunkStatus.NeedToBeRedrawn;
        
        return true;
    }

    byte CalculateHealthLevel(int hp, int maxHp)
    {
        float proportion = (float)hp / maxHp; // 0.625f

        // TODO: this require information from MeshGenerator which breaks the encapsulation rule
        float step = (float)1 / 11; // _crackUVs.Length; // 0.09f
        float value = proportion / step; // 6.94f
        int level = Mathf.RoundToInt(value); // 7

        return (byte)(11 - level); // array is in reverse order so we subtract our value from 1
    }
}