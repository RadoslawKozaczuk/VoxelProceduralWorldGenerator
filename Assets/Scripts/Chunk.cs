using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Assets.Scripts
{
	[Serializable]
	class BlockData
	{
		// we store only block type
		public Block.BlockType[,,] Matrix;

		public BlockData() { }

		public BlockData(Block[,,] b)
		{
			Matrix = new Block.BlockType[World.ChunkSize, World.ChunkSize, World.ChunkSize];
			for (int z = 0; z < World.ChunkSize; z++)
			{
				for (int y = 0; y < World.ChunkSize; y++)
				{
					for (int x = 0; x < World.ChunkSize; x++)
					{
						Matrix[x, y, z] = b[x, y, z].Type;
					}
				}
			}
		}
	}

	public class Chunk
	{
		public Material CubeMaterial;
		public Block[,,] Blocks;
		public GameObject ChunkGameObject;
		public ChunkMB MonoBehavior;
		public bool Changed = false;

		// caves should be more erratic so has to be a higher number
		private const float CaveProbability = 0.43f;
		private const float CaveSmooth = 0.09f;
		private const int CaveOctaves = 3; // reduced a bit to lower workload but not to much to maintain randomness

		// shiny diamonds!
		private const float DiamondProbability = 0.38f; // this is not percentage chance because we are using Perlin function
		private const float DiamondSmooth = 0.06f;
		private const int DiamondOctaves = 3;
		private const int DiamondMaxHeight = 50;

		// red stones
		private const float RedstoneProbability = 0.41f;
		private const float RedstoneSmooth = 0.06f;
		private const int RedstoneOctaves = 3;
		private const int RedstoneMaxHeight = 30;

		public enum ChunkStatus { Draw, Done, Keep }

		public ChunkStatus Status; // status of the current chunk
		private BlockData _blockData;

		public Chunk(Vector3 position, Material c, string chunkName)
		{
			ChunkGameObject = new GameObject(chunkName);
			ChunkGameObject.transform.position = position;
			MonoBehavior = ChunkGameObject.AddComponent<ChunkMB>();
			MonoBehavior.SetOwner(this);
			CubeMaterial = c;
			BuildChunk();
		}

		void BuildChunk()
		{
			bool dataFromFile = false;
			dataFromFile = Load();

			Blocks = new Block[World.ChunkSize, World.ChunkSize, World.ChunkSize];

			for (var z = 0; z < World.ChunkSize; z++)
				for (var y = 0; y < World.ChunkSize; y++)
					for (var x = 0; x < World.ChunkSize; x++)
					{
						var pos = new Vector3(x, y, z);

						// taking into consideration the noise generator
						int worldX = (int) (x + ChunkGameObject.transform.position.x);
						int worldY = (int) (y + ChunkGameObject.transform.position.y);
						int worldZ = (int) (z + ChunkGameObject.transform.position.z);

						if (dataFromFile)
						{
							Blocks[x, y, z] = new Block(_blockData.Matrix[x, y, z], pos, ChunkGameObject.gameObject, this);
							continue;
						}

						// old random way
						//var type = Random.Range(0, 100) < 50 ? Block.BlockType.Dirt : Block.BlockType.Air;

						// generate height
						Block.BlockType type;

						if (Utils.FractalBrownianMotion3D(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
						{
							type = Block.BlockType.Air;
						}
						else if (worldY <= Utils.GenerateBedrockHeight(worldX, worldZ))
						{
							type = Block.BlockType.Bedrock;
						}
						else if (worldY <= Utils.GenerateStoneHeight(worldX, worldZ))
						{
							if (Utils.FractalBrownianMotion3D(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability 
								&& worldY < DiamondMaxHeight)
							{
								type = Block.BlockType.Diamond;
							}
							else if (Utils.FractalBrownianMotion3D(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability
								&& worldY < RedstoneMaxHeight)
							{
								type = Block.BlockType.Redstone;
							}
							else
							{
								type = Block.BlockType.Stone;
							}
						}
						else if (worldY == Utils.GenerateHeight(worldX, worldZ))
						{
							type = Block.BlockType.Grass;
						}
						else if (worldY <= Utils.GenerateHeight(worldX, worldZ))
						{
							type = Block.BlockType.Dirt;
						}
						else
						{
							type = Block.BlockType.Air;
						}
						
						Blocks[x, y, z] = new Block(type, pos, ChunkGameObject.gameObject, this);
					}

			// chunk just has been created and it is ready to be drawn
			Status = ChunkStatus.Draw;
		}

		public void Redraw()
		{
			// we cannot use normal destroy because it may wait to the next update loop or something which will break the code
			UnityEngine.Object.DestroyImmediate(ChunkGameObject.GetComponent<MeshFilter>());
			UnityEngine.Object.DestroyImmediate(ChunkGameObject.GetComponent<MeshRenderer>());
			UnityEngine.Object.DestroyImmediate(ChunkGameObject.GetComponent<Collider>());
			DrawChunk();
		}

		public void DrawChunk()
		{
			for (var z = 0; z < World.ChunkSize; z++)
				for (var y = 0; y < World.ChunkSize; y++)
					for (var x = 0; x < World.ChunkSize; x++)
					{
						Blocks[x, y, z].Draw();
					}
			CombineQuads();

			// adding collision
			var collider = ChunkGameObject.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
			collider.sharedMesh = ChunkGameObject.transform.GetComponent<MeshFilter>().mesh;
			Status = ChunkStatus.Done;
		}
		
		void CombineQuads()
		{
			//1. Combine all children meshes
			var meshFilters = ChunkGameObject.GetComponentsInChildren<MeshFilter>();
			var combine = new CombineInstance[meshFilters.Length];
			var i = 0;
			while (i < meshFilters.Length)
			{
				combine[i].mesh = meshFilters[i].sharedMesh;
				combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
				i++;
			}

			//2. Create a new mesh on the parent object
			var mf = (MeshFilter)ChunkGameObject.gameObject.AddComponent(typeof(MeshFilter));
			mf.mesh = new Mesh();

			//3. Add combined meshes on children as the parent's mesh
			mf.mesh.CombineMeshes(combine);

			//4. Create a renderer for the parent
			var renderer = ChunkGameObject.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
			renderer.material = CubeMaterial;

			//5. Delete all uncombined children
			foreach (Transform quad in ChunkGameObject.transform)
				UnityEngine.Object.Destroy(quad.gameObject);
		}

		bool Load()
		{
			string chunkFile = World.BuildChunkFileName(ChunkGameObject.transform.position);
			if (File.Exists(chunkFile))
			{
				var bf = new BinaryFormatter();
				FileStream file = File.Open(chunkFile, FileMode.Open);
				_blockData = new BlockData();
				_blockData = (BlockData) bf.Deserialize(file);
				file.Close();

				// Debug.Log("Loading chunk from file: " + chunkFile);
				return true;
			}

			return false;
		}

		public void Save()
		{
			string chunkFile = World.BuildChunkFileName(ChunkGameObject.transform.position);

			if (!File.Exists(chunkFile))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));
			}

			var bf = new BinaryFormatter();
			FileStream file = File.Open(chunkFile, FileMode.OpenOrCreate);
			_blockData = new BlockData(Blocks);
			bf.Serialize(file, _blockData);
			file.Close();
			//Debug.Log("Saving chunk from file: " + chunkFile);
		}
	}
}