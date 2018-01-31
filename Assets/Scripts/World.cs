using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class World : MonoBehaviour
	{
		public Material TextureAtlas;
		public static int ColumnHeight = 6; // number of chunks
		public static int ChunkSize = 6;
		public static Dictionary<string, Chunk> Chunks;

		public static string BuildChunkName(Vector3 v)
		{
			return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
		}

		IEnumerator BuildChunkColumn()
		{
			for (var i = 0; i < ColumnHeight; i++)
			{
				var chunkPosition = new Vector3(transform.position.x, i * ChunkSize, transform.position.z);
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
			StartCoroutine(BuildChunkColumn());
		}
	}
}
