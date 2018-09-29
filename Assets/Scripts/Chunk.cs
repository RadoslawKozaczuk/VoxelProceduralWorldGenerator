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

    readonly int _chunkSize, _worldSizeX, _worldSizeY, _worldSizeZ;

    public Chunk(Vector3 position, Material chunkMaterial, Material transparentMaterial, World worldReference, Vector3Int coord)
    {
        _chunkSize = worldReference.ChunkSize;
        _worldSizeX = worldReference.WorldSizeX;
        _worldSizeY = worldReference.WorldSizeY;
        _worldSizeZ = worldReference.WorldSizeZ;

        // TODO: do sprawdzenia czy to ma wogole sens - czy ten klucz odpowiada numerowi czunku w przestrzeni
        var chunkKey = _worldSizeY * _worldSizeY * coord.y + _worldSizeZ * coord.z + coord.x;
        Coord = coord;

        Terrain = new GameObject(chunkKey.ToString());
        Terrain.transform.position = position;
        Terrain.transform.SetParent(worldReference.TerrainParent);
        TerrainMaterial = chunkMaterial;

        Water = new GameObject(chunkKey + "_fluid");
        Water.transform.position = position;
        Water.transform.SetParent(worldReference.WaterParent);
        WaterMaterial = transparentMaterial;

        MonoBehavior = Terrain.AddComponent<ChunkMonoBehavior>();
        MonoBehavior.SetOwner(this);

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
        CalculateMeshes(meshGenerator, Coord);
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

    public void CalculateMeshes(MeshGenerator meshGenerator, Vector3 chunkPos)
    {
        Mesh mesh, waterMesh;
        meshGenerator.CreateMeshes(ref Blocks, Coord, out mesh, out waterMesh);

        // Create terrain object
        if (mesh != null)
        {
            var chunk = new GameObject("Chunk");
            chunk.transform.position = chunkPos;
            chunk.transform.parent = Terrain.transform;

            var renderer = chunk.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material = TerrainMaterial;

            var meshFilter = (MeshFilter)chunk.AddComponent(typeof(MeshFilter));
            meshFilter.mesh = mesh;

            var collider = Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            collider.sharedMesh = mesh;
        }

        // Create water object
        if (waterMesh != null)
        {
            var waterChunk = new GameObject("WaterChunk");
            waterChunk.transform.position = chunkPos;
            waterChunk.transform.parent = Water.transform;

            var waterRenderer = waterChunk.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            waterRenderer.material = WaterMaterial;

            var waterMeshFilter = (MeshFilter)waterChunk.AddComponent(typeof(MeshFilter));
            waterMeshFilter.mesh = waterMesh;
        }

        Status = ChunkStatus.Created;
    }
}