using System;
using System.Diagnostics;
using UnityEngine;

public class Chunk
{
    static Stopwatch _stopwatch = new Stopwatch();
    static long _accumulatedTerrainObjectCreationTime, _accumulatedWaterObjectCreationTime;

    public Material TerrainMaterial;
    public Material WaterMaterial;
    public GameObject Terrain;
    public GameObject Water;
    public BlockData[,,] Blocks;
    public Vector3Int Coord;

    public ChunkMonoBehavior MonoBehavior;
    public bool Changed = false;
    public ChunkStatus Status; // status of the current chunk
    
    Vector3 _position;
    readonly int _chunkSize, _worldSizeX, _worldSizeY, _worldSizeZ;
    readonly string _chunkKey;

    public Chunk(Vector3 position, Material chunkMaterial, Material transparentMaterial, World worldReference, Vector3Int coord)
    {
        Coord = coord;

        _chunkSize = worldReference.ChunkSize;
        _worldSizeX = worldReference.WorldSizeX;
        _worldSizeY = worldReference.WorldSizeY;
        _worldSizeZ = worldReference.WorldSizeZ;
        _position = position;
        _chunkKey = $"{coord.y + coord.z * _worldSizeZ + coord.x * _worldSizeY * _worldSizeZ}";
        
        Terrain = new GameObject(_chunkKey);
        Terrain.transform.position = position;
        Terrain.transform.SetParent(worldReference.TerrainParent);
        TerrainMaterial = chunkMaterial;

        Water = new GameObject(_chunkKey);
        Water.transform.position = position;
        Water.transform.SetParent(worldReference.WaterParent);
        WaterMaterial = transparentMaterial;

        //MonoBehavior = Terrain.AddComponent<ChunkMonoBehavior>();
        //MonoBehavior.SetOwner(this);

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

    public void UpdateChunk()
    {
        for (var z = 0; z < _chunkSize; z++)
            for (var y = 0; y < _chunkSize; y++)
                for (var x = 0; x < _chunkSize; x++)
                {
                    var b = Blocks[x, y, z];
                    //if (b.Type == BlockType.Sand)
                    //MonoBehavior.StartCoroutine(MonoBehavior.Drop(b, BlockType.Sand));
                }
    }

    public void RecreateMeshAndCollider()
    {
        DestroyMeshesAndColliders();

        var meshGenerator = new MeshGenerator(_chunkSize, _worldSizeX, _worldSizeY, _worldSizeZ);

        MeshData t, w;
        meshGenerator.ExtractMeshData(ref Blocks, Coord, out t, out w);
        var tm = meshGenerator.CreateMesh(t);
        var wm = meshGenerator.CreateMesh(w);

        CreateTerrainObject(tm);
        CreateWaterObject(wm);

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
    
    public void CreateTerrainObject(Mesh mesh)
    {
        _stopwatch.Restart();

        var renderer = Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = TerrainMaterial;

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
        renderer.material = WaterMaterial;

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