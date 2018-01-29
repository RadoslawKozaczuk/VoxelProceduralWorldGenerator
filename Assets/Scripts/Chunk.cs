using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {

	public Material cubeMaterial;
	public Block[,,] chunkData;

	IEnumerator BuildChunk(int sizeX, int sizeY, int sizeZ)
	{
		chunkData = new Block[sizeX, sizeY, sizeZ];

		for(int z = 0; z < sizeZ; z++)
			for(int y = 0; y < sizeY; y++)
				for(int x = 0; x < sizeX; x++)
				{
					Vector3 pos = new Vector3(x,y,z);
					chunkData[x,y,z] = new Block(Block.BlockType.DIRT, pos, this.gameObject, cubeMaterial);
				}


		for (int z = 0; z < sizeZ; z++)
			for (int y = 0; y < sizeY; y++)
				for (int x = 0; x < sizeX; x++)
				{
					chunkData[x,y,z].Draw();
					yield return null;
				}

		CombineQuads();
	}

	// Use this for initialization
	void Start () {
		StartCoroutine(BuildChunk(4,4,4));
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void CombineQuads()
	{
		//1. Combine all children meshes
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length) {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            i++;
        }

        //2. Create a new mesh on the parent object
        MeshFilter mf = (MeshFilter) this.gameObject.AddComponent(typeof(MeshFilter));
        mf.mesh = new Mesh();

        //3. Add combined meshes on children as the parent's mesh
        mf.mesh.CombineMeshes(combine);

        //4. Create a renderer for the parent
		MeshRenderer renderer = this.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = cubeMaterial;

		//5. Delete all uncombined children
		foreach (Transform quad in this.transform) {
     		Destroy(quad.gameObject);
 		}

	}

}
