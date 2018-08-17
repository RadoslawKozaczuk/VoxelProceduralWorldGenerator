using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    internal class BlockData
    {
        // we store only block type
        public Block.BlockType[,,] BlockTypes;

        public BlockData() { }

        public BlockData(Block[,,] b)
        {
            BlockTypes = new Block.BlockType[World.ChunkSize, World.ChunkSize, World.ChunkSize];

            for (int z = 0; z < World.ChunkSize; z++)
                for (int y = 0; y < World.ChunkSize; y++)
                    for (int x = 0; x < World.ChunkSize; x++)
                        BlockTypes[x, y, z] = b[x, y, z].Type;
        }
    }

    public struct BlockTypeJob : IJobParallelFor
    {
        // Native Array is basically just an array
        // but it has bunch of restriction and it integrates with the safty system

        // everything is struct - this is for safty purposes

        // Jobs declare all data that will be accessed in the job
        // For quaranteeing job safety, it is also required to declare if data is only read.

        [ReadOnly]
        public NativeArray<int> Indexes;
        [ReadOnly]
        public float ChunkPosX;
        [ReadOnly]
        public float ChunkPosY;
        [ReadOnly]
        public float ChunkPosZ;

        // result
        public NativeArray<Block.BlockType> Result;

        public void Execute(int i)
        {
            // deflattenization - extract coords from the index
            var index = Indexes[i];
            var z = index / (World.ChunkSize * World.ChunkSize);
            index -= z * World.ChunkSize * World.ChunkSize;

            var y = index / World.ChunkSize;
            index -= y * World.ChunkSize;

            var x = index;
            
            Result[i] = Chunk.DetermineType((int)(x + ChunkPosX), (int)(y + ChunkPosY), (int)(z + ChunkPosZ));
        }
    }

    public class Chunk
    {
        public enum ChunkStatus { NotInitialized, Created, NeedToBeRedrawn, Keep }

        [SerializeField] World _worldReference;

        public Material TerrainMaterial;
        public Material WaterMaterial;
        public GameObject Terrain;
        public GameObject Water;

        public Block[,,] Blocks;

        public int ChunkName;
        public int X;
        public int Y;
        public int Z;

        public ChunkMonoBehavior MonoBehavior;
        public UVScroller TextureScroller;
        public bool Changed = false;
        public ChunkStatus Status; // status of the current chunk

        // caves should be more erratic so has to be a higher number
        const float CaveProbability = 0.43f;
        const float CaveSmooth = 0.09f;
        const int CaveOctaves = 3; // reduced a bit to lower workload but not to much to maintain randomness
        const int WaterLevel = 65; // inclusive

        // shiny diamonds!
        const float DiamondProbability = 0.38f; // this is not percentage chance because we are using Perlin function
        const float DiamondSmooth = 0.06f;
        const int DiamondOctaves = 3;
        const int DiamondMaxHeight = 50;

        // red stones
        const float RedstoneProbability = 0.41f;
        const float RedstoneSmooth = 0.06f;
        const int RedstoneOctaves = 3;
        const int RedstoneMaxHeight = 30;

        // woodbase
        // TODO: these values are very counterintuitive and at some point needs to be converted to percentage values
        const float WoodbaseProbability = 0.40f;
        const float WoodbaseSmooth = 0.4f;
        const int WoodbaseOctaves = 2;
        const int TreeHeight = 7;

        BlockData _blockData;
        
        // parallelism
        BlockTypeJob _typeJob;
        JobHandle _typeJobHandle;

        public static readonly int[] TypeJobIndexes = InitializeJobIndexes();

        public static int[] InitializeJobIndexes()
        {
            var size = World.ChunkSize * World.ChunkSize * World.ChunkSize;
            var table = new int[size];
            for (int i = 0; i < size; i++)
                table[i] = i;
            return table;
        }

        public Chunk(Vector3 position, Material chunkMaterial, Material transparentMaterial, int chunkKey, World worldReference, int x, int y, int z)
        {
            ChunkName = chunkKey;
            X = x;
            Y = y;
            Z = z;

            _worldReference = worldReference;

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
                        if (b.Type == Block.BlockType.Sand)
                            MonoBehavior.StartCoroutine(MonoBehavior.Drop(b, Block.BlockType.Sand));
                    }  
        }

        public void RecreateMeshAndCollider()
        {
            DestroyMeshAndCollider();
            CreateMeshAndCollider();
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

        public void CreateMeshAndCollider()
        {
            // Determining mesh size
            int size = 0, waterSize = 0;
            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                        Blocks[x, y, z].CalculateMeshSize(ref size, ref waterSize);

            var mesh = new Mesh();
            var uvs = new Vector2[size];
            var suvs = new List<Vector2>(size);
            var verticies = new Vector3[size];
            var normals = new Vector3[size];
            var triangles = new int[(int)(1.5f * size)];
            var index = 0;
            var triIndex = 0;

            var waterMesh = new Mesh();
            var waterUvs = new Vector2[waterSize];
            var waterSuvs = new List<Vector2>(waterSize);
            var waterVerticies = new Vector3[waterSize];
            var waterNormals = new Vector3[waterSize];
            var waterTriangles = new int[(int)(1.5f * waterSize)];
            var waterIndex = 0;
            var waterTriIndex = 0;

            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                    {
                        var b = Blocks[x, y, z];

                        if (b.Faces == 0 || b.Type == Block.BlockType.Air)
                            continue;

                        if(b.Type == Block.BlockType.Water)
                            Blocks[x, y, z].CreateQuads(ref waterIndex, ref waterTriIndex,
                                                        ref waterVerticies, ref waterNormals, ref waterUvs, ref waterSuvs, ref waterTriangles,
                                                        new Vector3(x, y, z));
                        else
                            Blocks[x, y, z].CreateQuads(ref index, ref triIndex,
                                                        ref verticies, ref normals, ref uvs, ref suvs, ref triangles,
                                                        new Vector3(x, y, z));
                    }

            var chunkPosition = new Vector3(X * World.ChunkSize, Y * World.ChunkSize, Z * World.ChunkSize);

            // Create terrain mesh
            if (size > 0)
            {
                mesh.vertices = verticies;
                mesh.normals = normals;
                mesh.uv = uvs; // Uvs maps the texture over the surface
                mesh.triangles = triangles;
                mesh.SetUVs(1, suvs); // secondary uvs
                mesh.RecalculateBounds();

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
            
            // Create water mesh
            if(waterSize > 0)
            {
                waterMesh.vertices = waterVerticies;
                waterMesh.normals = waterNormals;
                waterMesh.uv = waterUvs; // Uvs maps the texture over the surface
                waterMesh.triangles = waterTriangles;
                waterMesh.SetUVs(1, waterSuvs); // secondary uvs
                waterMesh.RecalculateBounds();

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
            Blocks = new Block[World.ChunkSize, World.ChunkSize, World.ChunkSize];
            
            // output data
            var types = new Block.BlockType[World.ChunkSize * World.ChunkSize * World.ChunkSize];

            _typeJob = new BlockTypeJob()
            {
                // input data
                ChunkPosX = Terrain.transform.position.x,
                ChunkPosY = Terrain.transform.position.y,
                ChunkPosZ = Terrain.transform.position.z,
                Indexes = new NativeArray<int>(TypeJobIndexes, Allocator.TempJob),
                Result = new NativeArray<Block.BlockType>(types, Allocator.TempJob)
            };

            // schedule jobs (as many as TypeJobIndexes.Length)
            _typeJobHandle = _typeJob.Schedule(TypeJobIndexes.Length, 5);
            _typeJobHandle.Complete();

            _typeJob.Result.CopyTo(types);

            // clean up
            _typeJob.Indexes.Dispose();
            _typeJob.Result.Dispose();

            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                    {
                        var pos = new Vector3(x, y, z);
                        var type = types[x + y * World.ChunkSize + z * World.ChunkSize * World.ChunkSize];

                        GameObject gameObject = type == Block.BlockType.Water
                            ? Water.gameObject
                            : Terrain.gameObject;

                        Blocks[x, y, z] = new Block(type, pos, this);
                    }

            AddTrees();

            // chunk just has been created and it is ready to be drawn
            Status = ChunkStatus.NotInitialized;
        }
        
        // no part of a tree can be in another chunk
        void AddTrees()
        {
            for (var z = 1; z < World.ChunkSize - 1; z++)
                // trees cannot grow on chunk edges (x, y cannot be 0 or ChunkSize) 
                // simplification - that's because chunks are created in isolation
                // so we cannot put leafes in another chunk
                for (var y = 0; y < World.ChunkSize - TreeHeight; y++)
                    for (var x = 1; x < World.ChunkSize - 1; x++)
                    {
                        var trunk = Blocks[x, y, z];
                        if (trunk.Type != Block.BlockType.Grass) continue;

                        if (IsThereEnoughSpaceForTree(x, y, z))
                        {
                            int worldX = (int)(x + Terrain.transform.position.x);
                            int worldY = (int)(y + Terrain.transform.position.y);
                            int worldZ = (int)(z + Terrain.transform.position.z);

                            if (Utils.FractalFunc(worldX, worldY, worldZ, WoodbaseSmooth, WoodbaseOctaves) < WoodbaseProbability)
                            {
                                BuildTree(trunk, x, y, z);
                                x += 2; // no trees can be that close
                            }
                        }
                    }
        }

        bool IsThereEnoughSpaceForTree(int x, int y, int z)
        {
            // Coordinates system
            /*
              | y
              |____z
             /
            / x
            */

            for (int i = 2; i < TreeHeight; i++)
            {
                if (Blocks[x + 1, y + i, z].Type != Block.BlockType.Air
                    || Blocks[x - 1, y + i, z].Type != Block.BlockType.Air
                    || Blocks[x, y + i, z + 1].Type != Block.BlockType.Air
                    || Blocks[x, y + i, z - 1].Type != Block.BlockType.Air
                    || Blocks[x + 1, y + i, z + 1].Type != Block.BlockType.Air
                    || Blocks[x + 1, y + i, z - 1].Type != Block.BlockType.Air
                    || Blocks[x - 1, y + i, z + 1].Type != Block.BlockType.Air
                    || Blocks[x - 1, y + i, z - 1].Type != Block.BlockType.Air)
                    return false;
            }

            return true;
        }
        
        public static Block.BlockType DetermineType(int worldX, int worldY, int worldZ)
        {
            Block.BlockType type;

            if (worldY <= Utils.GenerateBedrockHeight(worldX, worldZ))
                type = Block.BlockType.Bedrock;
            else if (worldY <= Utils.GenerateStoneHeight(worldX, worldZ))
            {
                if (Utils.FractalFunc(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability && worldY < DiamondMaxHeight)
                    type = Block.BlockType.Diamond;
                else if (Utils.FractalFunc(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability && worldY < RedstoneMaxHeight)
                    type = Block.BlockType.Redstone;
                else
                    type = Block.BlockType.Stone;
            }
            else if (worldY == Utils.GenerateHeight(worldX, worldZ))
                type = Block.BlockType.Grass;
            else if (worldY <= Utils.GenerateHeight(worldX, worldZ))
                type = Block.BlockType.Dirt;
            else if (worldY <= WaterLevel)
                type = Block.BlockType.Water;
            else
                type = Block.BlockType.Air;

            // generate caves
            if (type != Block.BlockType.Water && Utils.FractalFunc(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
                type = Block.BlockType.Air;

            return type;
        }
        
        void BuildTree(Block trunk, int x, int y, int z)
        {
            trunk.Type = Block.BlockType.Woodbase;
            Blocks[x, y + 1, z].Type = Block.BlockType.Wood;
            Blocks[x, y + 2, z].Type = Block.BlockType.Wood;

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    for (int k = 3; k <= 4; k++)
                        Blocks[x + i, y + k, z + j].Type = Block.BlockType.Leaves;
            Blocks[x, y + 5, z].Type = Block.BlockType.Leaves;
        }
        
        bool Load()
        {
            string chunkFile = World.BuildChunkFileName(Terrain.transform.position);
            if (!File.Exists(chunkFile)) return false;

            var bf = new BinaryFormatter();
            FileStream file = File.Open(chunkFile, FileMode.Open);
            _blockData = new BlockData();
            _blockData = (BlockData)bf.Deserialize(file);
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
            _blockData = new BlockData(Blocks);
            bf.Serialize(file, _blockData);
            file.Close();
        }
    }
}