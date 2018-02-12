using UnityEngine;

namespace Assets.Scripts
{
	public class Chunk
	{
		public Material CubeMaterial;
		public Block[,,] Blocks;
		public GameObject ChunkGameObject;

		private float CaveProbability = 0.43f;

		public Chunk(Vector3 position, Material c)
		{
			ChunkGameObject = new GameObject(World.BuildChunkName(position));
			ChunkGameObject.transform.position = position;
			CubeMaterial = c;
			BuildChunk();
		}

		void BuildChunk()
		{
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

						// old random way
						//var type = Random.Range(0, 100) < 50 ? Block.BlockType.Dirt : Block.BlockType.Air;

						// generate height
						Block.BlockType type;

						if (Utils.FractalBrownianMotion3D(worldX, worldY, worldZ) < CaveProbability)
						{
							type = Block.BlockType.Air;
						}
						else if (worldY <= Utils.GenerateStoneHeight(worldX, worldZ))
						{
							type = Block.BlockType.Stone;
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
				Object.Destroy(quad.gameObject);
		}
	}
}
