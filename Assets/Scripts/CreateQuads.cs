using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateQuads : MonoBehaviour {

	public Material cubeMaterial;
	enum Cubeside { BOTTOM, TOP, LEFT, RIGHT, FRONT, BACK };

	void CreateQuad(Cubeside side)
	{
		Mesh mesh = new Mesh();
		mesh.name = "ScriptedMesh" + side;

		// we define all the arrays we are going to need
		// "Anatomy of a Cube" says: Cube is made out of 4 arrays
		Vector3[] vertices = new Vector3[4]; // 4 vertices because quad has 4 corners
		Vector3[] normals = new Vector3[4];
		Vector2[] uvs = new Vector2[4];
		int[] triangles = new int[6];

		//all possible UVs - four corners of the texture
		Vector2 uv00 = new Vector2( 0f, 0f );
		Vector2 uv10 = new Vector2( 1f, 0f );
		Vector2 uv01 = new Vector2( 0f, 1f );
		Vector2 uv11 = new Vector2( 1f, 1f );

		//all possible vertices
		Vector3 p0 = new Vector3( -0.5f,  -0.5f,  0.5f );
		Vector3 p1 = new Vector3(  0.5f,  -0.5f,  0.5f );
		Vector3 p2 = new Vector3(  0.5f,  -0.5f, -0.5f );
		Vector3 p3 = new Vector3( -0.5f,  -0.5f, -0.5f );		 
		Vector3 p4 = new Vector3( -0.5f,   0.5f,  0.5f );
		Vector3 p5 = new Vector3(  0.5f,   0.5f,  0.5f );
		Vector3 p6 = new Vector3(  0.5f,   0.5f, -0.5f );
		Vector3 p7 = new Vector3( -0.5f,   0.5f, -0.5f );

		switch (side)
		{
			case Cubeside.BOTTOM:
				vertices = new[] { p0, p1, p2, p3 };
				normals = new[] {Vector3.down, Vector3.down,
					Vector3.down, Vector3.down};
				uvs = new[] { uv11, uv01, uv00, uv10 };
				triangles = new[] { 3, 1, 0, 3, 2, 1 };
				break;
			case Cubeside.TOP:
				vertices = new[] { p7, p6, p5, p4 };
				normals = new[] {Vector3.up, Vector3.up,
					Vector3.up, Vector3.up};
				uvs = new[] { uv11, uv01, uv00, uv10 };
				triangles = new[] { 3, 1, 0, 3, 2, 1 };
				break;
			case Cubeside.LEFT:
				vertices = new[] { p7, p4, p0, p3 };
				normals = new[] {Vector3.left, Vector3.left,
					Vector3.left, Vector3.left};
				uvs = new[] { uv11, uv01, uv00, uv10 };
				triangles = new[] { 3, 1, 0, 3, 2, 1 };
				break;
			case Cubeside.RIGHT:
				vertices = new[] { p5, p6, p2, p1 };
				normals = new[] {Vector3.right, Vector3.right,
					Vector3.right, Vector3.right};
				uvs = new[] { uv11, uv01, uv00, uv10 };
				triangles = new[] { 3, 1, 0, 3, 2, 1 };
				break;
			case Cubeside.FRONT:
				vertices = new[] { p4, p5, p1, p0 };
				normals = new[] {Vector3.forward, Vector3.forward,
					Vector3.forward, Vector3.forward};
				uvs = new[] { uv11, uv01, uv00, uv10 };
				triangles = new[] { 3, 1, 0, 3, 2, 1 };
				break;
			case Cubeside.BACK:
				vertices = new[] { p6, p7, p3, p2 };
				normals = new[] {Vector3.back, Vector3.back,
					Vector3.back, Vector3.back};
				uvs = new[] { uv11, uv01, uv00, uv10 };
				triangles = new[] { 3, 1, 0, 3, 2, 1 };
				break;
		}

		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		 
		// its a good idea to recalculate bounds each time we play around with meshes
		mesh.RecalculateBounds();
		
		GameObject quad = new GameObject("Quad");
	    quad.transform.parent = this.gameObject.transform;
     	MeshFilter meshFilter = (MeshFilter) quad.AddComponent(typeof(MeshFilter));
		meshFilter.mesh = mesh;
		MeshRenderer renderer = quad.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = cubeMaterial;
	}

	void CombineQuads()
	{
		//1. Combine all children meshes
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
		CombineInstance[] combine = new CombineInstance[meshFilters.Length];
		int i = 0;
		while (i < meshFilters.Length)
		{
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			i++;
		}

		//2. Create a new mesh on the parent object
		MeshFilter mf = (MeshFilter)this.gameObject.AddComponent(typeof(MeshFilter));
		mf.mesh = new Mesh();

		//3. Add combined meshes on children as the parent's mesh
		mf.mesh.CombineMeshes(combine);

		//4. Create a renderer for the parent
		MeshRenderer renderer = this.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = cubeMaterial;

		//5. Delete all uncombined children
		foreach (Transform quad in this.transform)
			Destroy(quad.gameObject);
	}

	void CreateCube()
	{
		CreateQuad(Cubeside.FRONT);
		CreateQuad(Cubeside.BACK);
		CreateQuad(Cubeside.TOP);
		CreateQuad(Cubeside.BOTTOM);
		CreateQuad(Cubeside.LEFT);
		CreateQuad(Cubeside.RIGHT);
		CombineQuads();
	}

	// Use this for initialization
	void Start () {
		CreateCube();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
