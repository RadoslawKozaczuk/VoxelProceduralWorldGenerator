using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    internal class BlockSaveData
    {
        // we store only block type
        public BlockType[,,] Types;

        public BlockSaveData() { }

        public BlockSaveData(BlockData[,,] b)
        {
            Types = new BlockType[World.ChunkSize, World.ChunkSize, World.ChunkSize];

            for (int z = 0; z < World.ChunkSize; z++)
                for (int y = 0; y < World.ChunkSize; y++)
                    for (int x = 0; x < World.ChunkSize; x++)
                        Types[x, y, z] = b[x, y, z].Type;
        }
    }

    public class Chunk
    {
        public enum ChunkStatus { NotInitialized, Created, NeedToBeRedrawn, Keep }
        
        public Material TerrainMaterial;
        public Material WaterMaterial;
        public GameObject Terrain;
        public GameObject Water;

        public BlockData[,,] Blocks;

        public int ChunkName;
        public int X;
        public int Y;
        public int Z;

        public ChunkMonoBehavior MonoBehavior;
        public UVScroller TextureScroller;
        public bool Changed = false;
        public ChunkStatus Status; // status of the current chunk
        
        BlockSaveData _blockSaveData;
        
        // this corresponds to the BlockType enum, so for example Grass can be hit 3 times
        public static readonly int[] BlockHealthMax = {
            3, 4, 4, -1, 4, 3, 3, 3, 3,
            8, // water
			3, // grass
			0  // air
		}; // -1 means the block cannot be destroyed
        
        public Chunk(Vector3 position, Material chunkMaterial, Material transparentMaterial, int chunkKey, World worldReference, int x, int y, int z)
        {
            ChunkName = chunkKey;
            X = x;
            Y = y;
            Z = z;

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

            // BUG: This is extremely slow
            //TextureScroller = FluidObject.AddComponent<UVScroller>();
            
            BuildChunk();
        }

        public void UpdateChunk()
        {
            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                    {
                        var b = Blocks[x, y, z];
                        //if (b.Type == BlockType.Sand)
                            //MonoBehavior.StartCoroutine(MonoBehavior.Drop(b, BlockType.Sand));
                    }  
        }

        public void RecreateMeshAndCollider()
        {
            DestroyMeshAndCollider();
            CreateChunkObject();
        }

        /// <summary>
		/// Destroys Meshes and Colliders
		/// </summary>
		public void DestroyMeshAndCollider()
        {
            // we cannot use normal destroy because it may wait to the next update loop or something which will break the code
            UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<MeshFilter>());
            UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(Terrain.GetComponent<Collider>());
            UnityEngine.Object.DestroyImmediate(Water.GetComponent<MeshFilter>());
            UnityEngine.Object.DestroyImmediate(Water.GetComponent<MeshRenderer>());
        }

        public void CreateChunkObject()
        {
            var chunkPosition = new Vector3(X * World.ChunkSize, Y * World.ChunkSize, Z * World.ChunkSize);

            Mesh mesh, waterMesh;
            MeshGenerator.CreateMeshes(ref Blocks, out mesh, out waterMesh);

            // Create terrain object
            if (mesh != null)
            {
                var chunk = new GameObject("Chunk");
                chunk.transform.position = chunkPosition;
                chunk.transform.parent = Terrain.transform;

                var renderer = chunk.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
                renderer.material = TerrainMaterial;

                var meshFilter = (MeshFilter)chunk.AddComponent(typeof(MeshFilter));
                meshFilter.mesh = mesh;

                var collider = Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
                collider.sharedMesh = mesh;
            }
            
            // Create water object
            if(waterMesh != null)
            {
                var waterChunk = new GameObject("WaterChunk");
                waterChunk.transform.position = chunkPosition;
                waterChunk.transform.parent = Water.transform;

                var waterRenderer = waterChunk.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
                waterRenderer.material = WaterMaterial;

                var waterMeshFilter = (MeshFilter)waterChunk.AddComponent(typeof(MeshFilter));
                waterMeshFilter.mesh = waterMesh;
            }
            
            Status = ChunkStatus.Created;
        }

        void BuildChunk()
        {
            //bool dataFromFile = Load();
            Blocks = TerrainGenerator.BuildChunk(Terrain.transform.position);
            Status = ChunkStatus.NotInitialized;
        }
        
        bool Load()
        {
            string chunkFile = World.BuildChunkFileName(Terrain.transform.position);
            if (!File.Exists(chunkFile)) return false;

            var bf = new BinaryFormatter();
            FileStream file = File.Open(chunkFile, FileMode.Open);
            _blockSaveData = new BlockSaveData();
            _blockSaveData = (BlockSaveData)bf.Deserialize(file);
            file.Close();

            return true;
        }

        public void Save()
        {
            string chunkFile = World.BuildChunkFileName(Terrain.transform.position);

            if (!File.Exists(chunkFile))
                Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));

            var bf = new BinaryFormatter();
            FileStream file = File.Open(chunkFile, FileMode.OpenOrCreate);
            _blockSaveData = new BlockSaveData(Blocks);
            bf.Serialize(file, _blockSaveData);
            file.Close();
        }
    }
}