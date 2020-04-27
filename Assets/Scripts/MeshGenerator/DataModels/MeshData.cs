using System.Collections.Generic;
using UnityEngine;

namespace Voxels.MeshGenerator.DataModels
{
	public readonly struct MeshData
	{
		public readonly Vector2[] Uvs;
		public readonly List<Vector2> Suvs;
		public readonly Vector3[] Verticies;
		public readonly Vector3[] Normals;
		public readonly int[] Triangles;

		public MeshData(Vector2[] uvs, List<Vector2> suvs, Vector3[] verticies, Vector3[] normals, int[] triangles)
		{
			Uvs = uvs;
			Suvs = suvs;
			Verticies = verticies;
			Normals = normals;
			Triangles = triangles;
		}
	}
}
