using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateQuads : MonoBehaviour {

	enum Cubeside {BOTTOM, TOP, LEFT, RIGHT, FRONT, BACK};
	public enum BlockType {GRASS, DIRT, STONE};

	public Material cubeMaterial;
	public BlockType bType;

	readonly Vector2[,] blockUVs = { 
		/*GRASS TOP*/		{new Vector2( 0.125f, 0.375f ), new Vector2( 0.1875f, 0.375f),
								new Vector2( 0.125f, 0.4375f ),new Vector2( 0.1875f, 0.4375f )},
		/*GRASS SIDE*/		{new Vector2( 0.1875f, 0.9375f ), new Vector2( 0.25f, 0.9375f),
								new Vector2( 0.1875f, 1.0f ),new Vector2( 0.25f, 1.0f )},
		/*DIRT*/			{new Vector2( 0.125f, 0.9375f ), new Vector2( 0.1875f, 0.9375f),
								new Vector2( 0.125f, 1.0f ),new Vector2( 0.1875f, 1.0f )},
		/*STONE*/			{new Vector2( 0, 0.875f ), new Vector2( 0.0625f, 0.875f),
								new Vector2( 0, 0.9375f ),new Vector2( 0.0625f, 0.9375f )}
						}; 

	void CreateQuad(Cubeside side)
	{
		var mesh = new Mesh();
	    mesh.name = "ScriptedMesh" + side.ToString(); 

		var vertices = new Vector3[4];
		var normals = new Vector3[4];
		var uvs = new Vector2[4];
		var triangles = new int[6];

		//all possible UVs
		Vector2 uv00;
		Vector2 uv10;
		Vector2 uv01;
		Vector2 uv11;

		if(bType == BlockType.GRASS && side == Cubeside.TOP)
		{
			uv00 = blockUVs[0,0];
			uv10 = blockUVs[0,1];
			uv01 = blockUVs[0,2];
			uv11 = blockUVs[0,3];
		}
		else if(bType == BlockType.GRASS && side == Cubeside.BOTTOM)
		{
			uv00 = blockUVs[(int)(BlockType.DIRT+1),0];
			uv10 = blockUVs[(int)(BlockType.DIRT+1),1];
			uv01 = blockUVs[(int)(BlockType.DIRT+1),2];
			uv11 = blockUVs[(int)(BlockType.DIRT+1),3];
		}
		else
		{
			uv00 = blockUVs[(int)(bType+1),0];
			uv10 = blockUVs[(int)(bType+1),1];
			uv01 = blockUVs[(int)(bType+1),2];
			uv11 = blockUVs[(int)(bType+1),3];
		}

		//all possible vertices 
		var p0 = new Vector3( -0.5f,  -0.5f,  0.5f );
		var p1 = new Vector3(  0.5f,  -0.5f,  0.5f );
		var p2 = new Vector3(  0.5f,  -0.5f, -0.5f );
		var p3 = new Vector3( -0.5f,  -0.5f, -0.5f );		 
		var p4 = new Vector3( -0.5f,   0.5f,  0.5f );
		var p5 = new Vector3(  0.5f,   0.5f,  0.5f );
		var p6 = new Vector3(  0.5f,   0.5f, -0.5f );
		var p7 = new Vector3( -0.5f,   0.5f, -0.5f );

		switch(side)
		{
			case Cubeside.BOTTOM:
				vertices = new[] {p0, p1, p2, p3};
				normals = new[] {Vector3.down, Vector3.down, Vector3.down, Vector3.down};
				uvs = new[] {uv11, uv01, uv00, uv10};
				triangles = new[] { 3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.TOP:
				vertices = new[] {p7, p6, p5, p4};
				normals = new[] {Vector3.up, Vector3.up, Vector3.up, Vector3.up};
				uvs = new[] {uv11, uv01, uv00, uv10};
				triangles = new[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.LEFT:
				vertices = new[] {p7, p4, p0, p3};
				normals = new[] {Vector3.left, Vector3.left, Vector3.left, Vector3.left};
				uvs = new[] {uv11, uv01, uv00, uv10};
				triangles = new[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.RIGHT:
				vertices = new[] {p5, p6, p2, p1};
				normals = new[] {Vector3.right, Vector3.right, Vector3.right, Vector3.right};
				uvs = new[] {uv11, uv01, uv00, uv10};
				triangles = new[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.FRONT:
				vertices = new[] {p4, p5, p1, p0};
				normals = new[] {Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward};
				uvs = new[] {uv11, uv01, uv00, uv10};
				triangles = new[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.BACK:
				vertices = new[] {p6, p7, p3, p2};
				normals = new[] {Vector3.back, Vector3.back, Vector3.back, Vector3.back};
				uvs = new[] {uv11, uv01, uv00, uv10};
				triangles = new[] {3, 1, 0, 3, 2, 1};
			break;
		}

		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		 
		mesh.RecalculateBounds();
		
		var quad = new GameObject("Quad");
	    quad.transform.parent = this.gameObject.transform;
     	var meshFilter = (MeshFilter) quad.AddComponent(typeof(MeshFilter));
		meshFilter.mesh = mesh;
		var renderer = quad.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = cubeMaterial;
	}

	void CombineQuads()
	{
		
		//1. Combine all children meshes
		var meshFilters = GetComponentsInChildren<MeshFilter>();
        var combine = new CombineInstance[meshFilters.Length];
        var i = 0;
        while (i < meshFilters.Length) {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            i++;
        }

        //2. Create a new mesh on the parent object
        var mf = (MeshFilter) this.gameObject.AddComponent(typeof(MeshFilter));
        mf.mesh = new Mesh();

        //3. Add combined meshes on children as the parent's mesh
        mf.mesh.CombineMeshes(combine);

        //4. Create a renderer for the parent
		var renderer = this.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = cubeMaterial;

		//5. Delete all uncombined children
		foreach (Transform quad in this.transform) {
     		Destroy(quad.gameObject);
 		}

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
