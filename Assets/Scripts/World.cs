using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class World : MonoBehaviour
	{
		public GameObject Player;
		public Material TextureAtlas;
		public static int ColumnHeight = 16; // number of chunks in column
		public static int ChunkSize = 8; // number of blocks in x and y
		public static int WorldSize = 8; // number of columns in x and y
		public static int Radius = 2; // radius tell us how many blocks around the layer needs to be generated
		public static Dictionary<string, Chunk> Chunks;

		public int posx;
		public int posz;

		public static string BuildChunkName(Vector3 v)
		{
			return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
		}

		IEnumerator BuildWorld()
		{
			posx = (int)Mathf.Floor(Player.transform.position.x / ChunkSize);
			posz = (int)Mathf.Floor(Player.transform.position.z / ChunkSize);

			for (var z = -Radius; z <= Radius; z++)
				for (var x = -Radius; x <= Radius; x++)
					for (var y = 0; y < ColumnHeight; y++)
					{
						// player position needs to be converted into chunk position
						var chunkPosition = new Vector3((x + posx) * ChunkSize,
														y * ChunkSize,
														(posz + z) * ChunkSize);

						var c = new Chunk(chunkPosition, TextureAtlas);
						c.ChunkGameObject.transform.parent = transform;
						Chunks.Add(c.ChunkGameObject.name, c);
					}

			foreach (var c in Chunks)
			{
				c.Value.DrawChunk();
				yield return null;
			}
			Player.SetActive(true);
		}

		// Use this for initialization
		void Start()
		{
			// to be sure player won't fall through the world that hasn't been build yet
			Player.SetActive(false);

			Chunks = new Dictionary<string, Chunk>();
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
			StartCoroutine(BuildWorld());
		}
	}
}
