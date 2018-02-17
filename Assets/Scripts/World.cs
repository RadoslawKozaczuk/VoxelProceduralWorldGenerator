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

		private bool firstBuild = true; // true if the first world hasn't been built is yet
		private bool building = false; // true when the building method is running

		public static string BuildChunkName(Vector3 v)
		{
			return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
		}

		IEnumerator BuildWorld()
		{
			building = true;
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

						Chunk c;
						var chunkName = BuildChunkName(chunkPosition);
						if (Chunks.TryGetValue(chunkName, out c))
						{
							// we iterate inside the radius so whatever is found has to be set to KEEP
							c.Status = Chunk.ChunkStatus.Keep;
							break; // if this block is in the dictionary then the rest of the column has to be in the dictionary
						}
						else
						{
							c = new Chunk(chunkPosition, TextureAtlas, chunkName);
							c.ChunkGameObject.transform.parent = transform;
							Chunks.Add(c.ChunkGameObject.name, c); // whatever is new is set to DRAW
						}
						
						// loading bar update
						if (firstBuild)
						{
							processedChunks++;
							loadingAmount.value = processedChunks / totalChunks * 100;
						}
					}

			foreach (KeyValuePair<string, Chunk> c in Chunks)
			{
				if(c.Value.Status == Chunk.ChunkStatus.Draw)
				{
					c.Value.DrawChunk();
					c.Value.Status = Chunk.ChunkStatus.Keep;
				}


				// after drawing whatever is left should be set to DONE
				c.Value.Status = Chunk.ChunkStatus.Done;

				if (firstBuild)
				{
					processedChunks++;
					loadingAmount.value = processedChunks / totalChunks * 100;
				}

				// drawing one by one was cool for testing but it is not in a real game
				// yield return null;
			}

			yield return null;

			if (firstBuild)
			{
				Player.SetActive(true);

				// disable UI
				loadingAmount.gameObject.SetActive(false);
				MainCamera.gameObject.SetActive(false);
				PlayButton.gameObject.SetActive(false);
				firstBuild = false;
			}

			building = false;
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

		private void Update()
		{
			if(!building && !firstBuild)
				StartCoroutine(BuildWorld());
		}
	}
}
