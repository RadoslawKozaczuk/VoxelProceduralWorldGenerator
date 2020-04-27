using System.Collections.Generic;
using UnityEngine;

namespace Voxels.MeshGenerator.DataModels
{
	readonly struct MeshData
	{
		internal readonly Vector2[] Uvs;
		internal readonly List<Vector2> Suvs;
		internal readonly Vector3[] Verticies;
		internal readonly Vector3[] Normals;
		internal readonly int[] Triangles;

		internal MeshData(Vector2[] uvs, List<Vector2> suvs, Vector3[] verticies, Vector3[] normals, int[] triangles)
		{
			Uvs = uvs;
			Suvs = suvs;
			Verticies = verticies;
			Normals = normals;
			Triangles = triangles;
		}
	}
}
