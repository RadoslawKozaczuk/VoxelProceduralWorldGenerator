using UnityEngine;

public class Chunk
{
	public Material CubeMaterial;
	public Block[,,] ChunkData;
	public GameObject ChunkObject;

	public Chunk(Vector3 position, Material c)
	{
		ChunkObject = new GameObject(World.BuildChunkName(position));
		ChunkObject.transform.position = position;
		CubeMaterial = c;
		BuildChunk();
	}

	void BuildChunk()
	{
		ChunkData = new Block[World.ChunkSize, World.ChunkSize, World.ChunkSize];

		for (var z = 0; z < World.ChunkSize; z++)
			for (var y = 0; y < World.ChunkSize; y++)
				for (var x = 0; x < World.ChunkSize; x++)
				{
					var pos = new Vector3(ChunkObject.transform.position.x + x,
						ChunkObject.transform.position.y + y,
						ChunkObject.transform.position.z + z);

					var type = Random.Range(0, 100) < 50
						? Block.BlockType.Air
						: Block.BlockType.Dirt;

					ChunkData[x, y, z] = new Block(type, pos, this);
				}

		CombineQuads();
	}

	public void DrawChunk()
	{
		for (var z = 0; z < World.ChunkSize; z++)
			for (var y = 0; y < World.ChunkSize; y++)
				for (var x = 0; x < World.ChunkSize; x++)
				{
					ChunkData[x, y, z].Draw();
				}
	}



	void CombineQuads()
	{
		//1. Combine all children meshes
		var meshFilters = ChunkObject.GetComponentsInChildren<MeshFilter>();
		var combine = new CombineInstance[meshFilters.Length];
		var i = 0;
		while (i < meshFilters.Length)
		{
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			i++;
		}

		//2. Create a new mesh on the parent object
		var mf = (MeshFilter)ChunkObject.AddComponent(typeof(MeshFilter));
		mf.mesh = new Mesh();

		//3. Add combined meshes on children as the parent's mesh
		mf.mesh.CombineMeshes(combine);

		//4. Create a renderer for the parent
		var renderer = ChunkObject.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = CubeMaterial;

		//5. Delete all uncombined children
		foreach (Transform quad in ChunkObject.transform)
			GameObject.Destroy(quad.gameObject);
	}
}
