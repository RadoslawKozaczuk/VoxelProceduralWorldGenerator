using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.World
{
	public struct MeshData
	{
		public Vector2[] Uvs;
		public List<Vector2> Suvs;
		public Vector3[] Verticies;
		public Vector3[] Normals;
		public int[] Triangles;
	}
}
