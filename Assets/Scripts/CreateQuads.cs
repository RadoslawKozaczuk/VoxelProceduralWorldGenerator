using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateQuads : MonoBehaviour {

	public Material cubeMaterial;
	
	void CreateQuad()
	{
		Mesh mesh = new Mesh();
	    mesh.name = "ScriptedMesh";

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

		
		vertices = new Vector3[] {p4, p5, p1, p0};
		normals = new Vector3[] {Vector3.forward, 
								 Vector3.forward, 
								 Vector3.forward, 
								 Vector3.forward};
		
		uvs = new Vector2[] {uv11, uv01, uv00, uv10};
		triangles = new int[] {3, 1, 0, 3, 2, 1};

		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		 
		// its a good idea to recalculate bounds each time we play around with meshes
		mesh.RecalculateBounds();
		
		GameObject quad = new GameObject("quad");
	    quad.transform.parent = this.gameObject.transform;
     	MeshFilter meshFilter = (MeshFilter) quad.AddComponent(typeof(MeshFilter));
		meshFilter.mesh = mesh;
		MeshRenderer renderer = quad.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = cubeMaterial;
	}

	// Use this for initialization
	void Start () {
		CreateQuad();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
