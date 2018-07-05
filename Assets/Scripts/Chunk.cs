using System;
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

    public struct MyParallelJob : IJobParallelFor
    {
        // Native Array is basically just an array
        // but it has bunch of restriction and it integrates with the safty system

        // everything is struct - this is for safty purposes

        // Jobs declare all data that will be accessed in the job
        // For quaranteeing job safety, it is also required to declare if data is only read.
        [ReadOnly]
        public NativeArray<Vector3Int> WorldCoords;

        // result
        public NativeArray<Block.BlockType> Result;

        public void Execute(int i)
        {
            var coord = WorldCoords[i];
            Result[i] = Chunk.DetermineType(coord.x, coord.y, coord.z);
        }
    }

    public class Chunk
    {
        public enum ChunkStatus { NotInitialized, Created, NeedToBeRedrawn, Keep }

        [SerializeField] World _worldReference;

        public Material CubeMaterial;
        public Material FluidMaterial;
        public GameObject ChunkObject;
        public GameObject FluidObject;

        public Block[,,] Blocks;
        public Block GetBlock(int x, int y, int z)
        {
            return Blocks[x, y * World.ChunkSize, z * World.ChunkSize * World.ChunkSize];
        }
        
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
        MyParallelJob myJob;
        JobHandle myJobHandle;
        
        public Chunk(Vector3 position, Material chunkMaterial, Material transparentMaterial, int chunkKey, World worldReference)
        {
            _worldReference = worldReference;

            ChunkObject = new GameObject(chunkKey.ToString());
            ChunkObject.transform.position = position;
            CubeMaterial = chunkMaterial;

            FluidObject = new GameObject(chunkKey + "_fluid");
            FluidObject.transform.position = position;
            FluidMaterial = transparentMaterial;

            MonoBehavior = ChunkObject.AddComponent<ChunkMonoBehavior>();
            MonoBehavior.SetOwner(this);

            // BUG: This is extremely slow
            //TextureScroller = FluidObject.AddComponent<UVScroller>();
            
            if (_worldReference.UseJobSystem)
                BuildChunk();
            else
                BuildChunkOld();

            // BUG: It doesn't really work as intended 
            // For some reason recreated chunks lose their transparency
            InformSurroundingChunks(chunkKey);
        }

        public void UpdateChunk()
        {
            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                        if (Blocks[x, y, z].Type == Block.BlockType.Sand)
                            MonoBehavior.StartCoroutine(MonoBehavior.Drop(
                                Blocks[x, y, z],
                                Block.BlockType.Sand));
        }

        /// <summary>
		/// Destroys Meshes and Colliders
		/// </summary>
		public void Clean()
        {
            // we cannot use normal destroy because it may wait to the next update loop or something which will break the code
            UnityEngine.Object.DestroyImmediate(ChunkObject.GetComponent<MeshFilter>());
            UnityEngine.Object.DestroyImmediate(ChunkObject.GetComponent<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(ChunkObject.GetComponent<Collider>());
            UnityEngine.Object.DestroyImmediate(FluidObject.GetComponent<MeshFilter>());
            UnityEngine.Object.DestroyImmediate(FluidObject.GetComponent<MeshRenderer>());
        }

        public void CreateMesh()
        {
            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                        Blocks[x, y, z].CreateQuads();

            CombineQuads(ChunkObject.gameObject, CubeMaterial);

            // adding collision
            var collider = ChunkObject.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            collider.sharedMesh = ChunkObject.transform.GetComponent<MeshFilter>().mesh;

            CombineQuads(FluidObject.gameObject, FluidMaterial);
            Status = ChunkStatus.Created;
        }

        /* New Build Chunk (async) */
        void BuildChunk()
        {
            bool dataFromFile = Load();
            Blocks = new Block[World.ChunkSize, World.ChunkSize, World.ChunkSize];
            
            // input data
            var worldCoords = new Vector3Int[World.ChunkSize * World.ChunkSize * World.ChunkSize];

            // output data
            var types = new Block.BlockType[World.ChunkSize * World.ChunkSize * World. ChunkSize];

            // create terrain
            
            // prepare data for pararell
            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                    {
                        if(dataFromFile)
                        {
                            var pos = new Vector3(x, y, z);

                            Block.BlockType type;
                            if (dataFromFile)
                            {
                                type = _blockData.BlockTypes[x, y, z];
                            }
                            else
                            {
                                int worldX = (int)(x + ChunkObject.transform.position.x);
                                int worldY = (int)(y + ChunkObject.transform.position.y);
                                int worldZ = (int)(z + ChunkObject.transform.position.z);
                                type = DetermineType(worldX, worldY, worldZ);
                            }

                            GameObject gameObject = type == Block.BlockType.Water
                                ? FluidObject.gameObject
                                : ChunkObject.gameObject;

                            Blocks[x, y, z] = new Block(type, pos, gameObject, this);
                        }
                        else
                        {
                            // flattering the table
                            var vector = new Vector3Int(
                                (int)(x + ChunkObject.transform.position.x),
                                (int)(y + ChunkObject.transform.position.y),
                                (int)(z + ChunkObject.transform.position.z));

                            worldCoords[x + y * World.ChunkSize + z * World.ChunkSize * World.ChunkSize] = vector;
                        }
                    }


            // creating a job - pararell
            if(!dataFromFile)
            {
                DetermineTypes(worldCoords, types);

                for (var z = 0; z < World.ChunkSize; z++)
                    for (var y = 0; y < World.ChunkSize; y++)
                        for (var x = 0; x < World.ChunkSize; x++)
                        {
                            var pos = new Vector3(x, y, z);
                            var type = types[x + y * World.ChunkSize + z * World.ChunkSize * World.ChunkSize];

                            GameObject gameObject = type == Block.BlockType.Water
                                ? FluidObject.gameObject
                                : ChunkObject.gameObject;

                            Blocks[x, y, z] = new Block(type, pos, gameObject, this);
                        }

                AddTrees();
            }
                
            // chunk just has been created and it is ready to be drawn
            Status = ChunkStatus.NotInitialized;
        }
        
        void DetermineTypes(Vector3Int[] worldCoords, Block.BlockType[] types)
        {
            var coordArray = new NativeArray<Vector3Int>(worldCoords, Allocator.TempJob);
            var typeArray = new NativeArray<Block.BlockType>(types, Allocator.TempJob);
            myJob = new MyParallelJob()
            {
                WorldCoords = coordArray,
                Result = typeArray
            };

            myJobHandle = myJob.Schedule(worldCoords.Length, 5);
            myJobHandle.Complete();

            myJob.Result.CopyTo(types);

            coordArray.Dispose();
            typeArray.Dispose();
        }

        /* Old Build Chunk */
        void BuildChunkOld()
        {
            bool dataFromFile = Load();
            Blocks = new Block[World.ChunkSize, World.ChunkSize, World.ChunkSize];

            // create terrain
            for (var z = 0; z < World.ChunkSize; z++)
                for (var y = 0; y < World.ChunkSize; y++)
                    for (var x = 0; x < World.ChunkSize; x++)
                    {
                        var pos = new Vector3(x, y, z);
                        
                        Block.BlockType type;
                        if (dataFromFile)
                        {
                            type = _blockData.BlockTypes[x, y, z];
                        }
                        else
                        {
                            int worldX = (int)(x + ChunkObject.transform.position.x);
                            int worldY = (int)(y + ChunkObject.transform.position.y);
                            int worldZ = (int)(z + ChunkObject.transform.position.z);
                            type = DetermineType(worldX, worldY, worldZ);
                        }
                        
                        GameObject gameObject = type == Block.BlockType.Water
                            ? FluidObject.gameObject
                            : ChunkObject.gameObject;
                        
                        Blocks[x, y, z] = new Block(type, pos, gameObject, this);
                    }

            if (!dataFromFile)
                AddTrees();

            // chunk just has been created and it is ready to be drawn
            Status = ChunkStatus.NotInitialized;
        }
        
        void AddTrees()
        {
            for (var z = 1; z < World.ChunkSize - 1; z++)
                // trees cannot grow on chunk edges (x, y cannot be 0 or ChunkSize) 
                // simplification - that's because chunks are created in isolation
                // so we cannot put leafes in another chunk
                for (var y = 0; y < World.ChunkSize - TreeHeight; y++)
                    for (var x = 1; x < World.ChunkSize - 1; x++)
                    {
                        if (Blocks[x, y, z].Type != Block.BlockType.Grass) continue;

                        if (IsThereEnoughSpaceForTree(x, y, z))
                        {
                            int worldX = (int)(x + ChunkObject.transform.position.x);
                            int worldY = (int)(y + ChunkObject.transform.position.y);
                            int worldZ = (int)(z + ChunkObject.transform.position.z);

                            if (Utils.FractalFunc(worldX, worldY, worldZ, WoodbaseSmooth, WoodbaseOctaves) < WoodbaseProbability)
                            {
                                BuildTree(Blocks[x, y, z], x, y, z);
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

        void InformSurroundingChunks(int chunkKey)
        {
            // BUG: In future I should encapsulate key arithmetic logic and move it somewhere else

            // front
            SetChunkToBeDrawn(chunkKey + World.ChunkSize);

            // back
            SetChunkToBeDrawn(chunkKey - World.ChunkSize);

            // up
            SetChunkToBeDrawn(chunkKey + World.ChunkSize * 1000);

            // down
            SetChunkToBeDrawn(chunkKey - World.ChunkSize * 1000);

            // left
            SetChunkToBeDrawn(chunkKey + World.ChunkSize * 1000000);

            // right
            SetChunkToBeDrawn(chunkKey - World.ChunkSize * 1000000);

            // BUG: the above does not take into consideration the edge scenario in the middle of the coordinate system
        }

        void SetChunkToBeDrawn(int targetChunkKey)
        {
            Chunk c;
            World.Chunks.TryGetValue(targetChunkKey, out c);

            if (c != null)
                c.Status = ChunkStatus.NeedToBeRedrawn;
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
            trunk.GetBlock(x, y + 1, z).Type = Block.BlockType.Wood;
            trunk.GetBlock(x, y + 2, z).Type = Block.BlockType.Wood;

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    for (int k = 3; k <= 4; k++)
                        trunk.GetBlock(x + i, y + k, z + j).Type = Block.BlockType.Leaves;
            trunk.GetBlock(x, y + 5, z).Type = Block.BlockType.Leaves;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj">Any game object that have all of the quads attached to it</param>
        /// <param name="mat"></param>
        void CombineQuads(GameObject obj, Material mat)
        {
            //1. Combine all children meshes
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            var combine = new CombineInstance[meshFilters.Length];

            var i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                i++;
            }

            //2. Create a new mesh on the parent object
            var mf = (MeshFilter)obj.gameObject.AddComponent(typeof(MeshFilter));
            mf.mesh = new Mesh();

            //3. Add combined meshes on children as the parent's mesh
            mf.mesh.CombineMeshes(combine);

            //4. Create a renderer for the parent
            var renderer = obj.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            renderer.material = mat;

            //5. Delete all uncombined children
            foreach (Transform quad in ChunkObject.transform)
                UnityEngine.Object.Destroy(quad.gameObject);
        }

        bool Load()
        {
            string chunkFile = World.BuildChunkFileName(ChunkObject.transform.position);
            if (!File.Exists(chunkFile)) return false;

            var bf = new BinaryFormatter();
            FileStream file = File.Open(chunkFile, FileMode.Open);
            _blockData = new BlockData();
            _blockData = (BlockData)bf.Deserialize(file);
            file.Close();

            // Debug.Log("Loading chunk from file: " + chunkFile);
            return true;
        }

        public void Save()
        {
            string chunkFile = World.BuildChunkFileName(ChunkObject.transform.position);

            if (!File.Exists(chunkFile))
                Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));

            var bf = new BinaryFormatter();
            FileStream file = File.Open(chunkFile, FileMode.OpenOrCreate);
            _blockData = new BlockData(Blocks);
            bf.Serialize(file, _blockData);
            file.Close();
            
            //Debug.Log("Saving chunk from file: " + chunkFile);
        }
    }
}