using UnityEngine;

public class Chunk {

	public Material cubeMaterial;
	public Block[,,] chunkData;
	public GameObject chunk;

	public Chunk(Vector3 position, Material c)
	{
		chunk = new GameObject(World.BuildChunkName(position));
		chunk.transform.position = position;
		cubeMaterial = c;
		BuildChunk();
	}

	void BuildChunk()
	{
		chunkData = new Block[World.chunkSize, World.chunkSize, World.chunkSize];

		for(var z = 0; z < World.chunkSize; z++)
			for(var y = 0; y < World.chunkSize; y++)
				for(var x = 0; x < World.chunkSize; x++)
				{
					var pos = new Vector3(chunk.transform.position.x + x, chunk.transform.position.y + y, chunk.transform.position.z + z);

					var type = Random.Range(0, 100) < 50 
						? Block.BlockType.AIR 
						: Block.BlockType.DIRT;

					chunkData[x,y,z] = new Block(type, pos, this);
				}
		
		CombineQuads();
	}

	public void DrawChunk()
	{
		for (var z = 0; z < World.chunkSize; z++)
			for (var y = 0; y < World.chunkSize; y++)
				for (var x = 0; x < World.chunkSize; x++)
				{
					chunkData[x,y,z].Draw();
				}
	}



	void CombineQuads()
	{
		//1. Combine all children meshes
		var meshFilters = chunk.GetComponentsInChildren<MeshFilter>();
		var combine = new CombineInstance[meshFilters.Length];
		var i = 0;
		while (i < meshFilters.Length)
		{
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			i++;
		}

		//2. Create a new mesh on the parent object
		var mf = (MeshFilter) chunk.AddComponent(typeof(MeshFilter));
		mf.mesh = new Mesh();

		//3. Add combined meshes on children as the parent's mesh
		mf.mesh.CombineMeshes(combine);

		//4. Create a renderer for the parent
		var renderer = chunk.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = cubeMaterial;

		//5. Delete all uncombined children
		foreach (Transform quad in chunk.transform)
			GameObject.Destroy(quad.gameObject);
	}
}
