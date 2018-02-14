using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

		public Slider loadingAmount;
		public Camera MainCamera;
		public Button PlayButton;

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


			float totalChunks = (Mathf.Pow(Radius * 2 + 1, 2) * ColumnHeight) * 2;
			
			int processedChunks = 0;

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

						// loading bar update
						processedChunks++;
						loadingAmount.value = processedChunks / totalChunks * 100;
					}

			foreach (var c in Chunks)
			{
				c.Value.DrawChunk();
				processedChunks++;
				loadingAmount.value = processedChunks / totalChunks * 100;

				// drawing one by one was cool for testing but it is not in a real game
				// yield return null;
			}

			yield return null;
			Player.SetActive(true);

			// disable UI
			loadingAmount.gameObject.SetActive(false);
			MainCamera.gameObject.SetActive(false);
			PlayButton.gameObject.SetActive(false);
		}

		// need to be public so PlayButton can access it
		public void StartBuild()
		{
			StartCoroutine(BuildWorld());
		}

		// Use this for initialization
		void Start()
		{
			// to be sure player won't fall through the world that hasn't been build yet
			Player.SetActive(false);

			Chunks = new Dictionary<string, Chunk>();
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
			
		}
	}
}
