using UnityEngine;

public class Chunk
{
    public enum ChunkStatus { NotInitialized, Created, NeedToBeRedrawn, Keep }

    public Material TerrainMaterial;
    public Material WaterMaterial;
    public GameObject Terrain;
    public GameObject Water;
    public BlockData[,,] Blocks;
    public Vector3Int Coord;

    public ChunkMonoBehavior MonoBehavior;
    public bool Changed = false;
    public ChunkStatus Status; // status of the current chunk

    // this corresponds to the BlockType enum, so for example Grass can be hit 3 times
    public static readonly int[] BlockHealthMax = {
            3, 4, 4, -1, 4, 3, 3, 3, 3,
            8, // water
			3, // grass
			0  // air
		}; // -1 means the block cannot be destroyed

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
        DestroyMeshAndCollider();
        var meshGenerator = new MeshGenerator(_chunkSize, _worldSizeX, _worldSizeY, _worldSizeZ);
        //CalculateMeshes(meshGenerator, Coord);
    }

    /// <summary>
    /// Destroys Meshes and Colliders
    /// </summary>
    public void DestroyMeshAndCollider()
    {
        // we cannot use normal destroy because it may wait to the next update loop or something which will break the code
        Object.DestroyImmediate(Terrain.GetComponent<MeshFilter>());
        Object.DestroyImmediate(Terrain.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(Terrain.GetComponent<Collider>());
        Object.DestroyImmediate(Water.GetComponent<MeshFilter>());
        Object.DestroyImmediate(Water.GetComponent<MeshRenderer>());
    }
    
    public void CreateTerrainObject(Mesh mesh)
    {
        var renderer = Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = TerrainMaterial;

        var meshFilter = (MeshFilter)Terrain.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;

        var collider = Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        collider.sharedMesh = mesh;
    }

    public void CreateWaterObject(Mesh mesh)
    {
        var waterRenderer = Water.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        waterRenderer.material = WaterMaterial;

        var waterMeshFilter = (MeshFilter)Water.AddComponent(typeof(MeshFilter));
        waterMeshFilter.mesh = mesh;
    }
}