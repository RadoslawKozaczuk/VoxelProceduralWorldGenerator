using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class World : MonoBehaviour
	{
		public Material TextureAtlas;
		public static int ColumnHeight = 16; // number of chunks in column
		public static int ChunkSize = 8;
		public static int WorldSize = 2; // number of columns in x and y
		public static Dictionary<string, Chunk> Chunks;

		public static string BuildChunkName(Vector3 v)
		{
			return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
		}

		IEnumerator BuildWorld()
		{
			for (var z = 0; z < WorldSize; z++)
				for (var x = 0; x < WorldSize; x++)
					for (var y = 0; y < ColumnHeight; y++)
					{
						var chunkPosition = new Vector3(x * ChunkSize, y * ChunkSize, z * ChunkSize);
						var c = new Chunk(chunkPosition, TextureAtlas);
						c.ChunkGameObject.transform.parent = transform;
						Chunks.Add(c.ChunkGameObject.name, c);
					}

			foreach (var c in Chunks)
			{
				c.Value.DrawChunk();
				yield return null;
			}
		}

		// Use this for initialization
		void Start()
		{
			Chunks = new Dictionary<string, Chunk>();
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
			StartCoroutine(BuildWorld());
		}
	}
}
