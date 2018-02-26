using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Realtime.Messaging.Internal;

namespace Assets.Scripts
{
	public class World : MonoBehaviour
	{
		public GameObject Player;
		public Material TextureAtlas;
		public static int ColumnHeight = 16; // number of chunks in column
		public static int ChunkSize = 8; // number of blocks in x and y
		public static int WorldSize = 8; // number of columns in x and y
		public static int Radius = 6; // radius tell us how many chunks around the layer needs to be generated

		public static List<string> ToRemove = new List<string>();

		// this is equivalent to Microsoft's concurrent dictionary - Unity simply does not support high enough .Net Framework
		public static ConcurrentDictionary<string, Chunk> Chunks;
		
		public Slider LoadingAmount;
		public Camera MainCamera;
		public Button PlayButton;

		public int Posx;
		public int Posz;

		private bool _firstBuild = true; // true if the first world hasn't been built is yet
		private bool _building = false; // true when the building method is running

		// this is necessary to avoid building world when the player does not move
		public Vector3 LastBuildPos;

		private CoroutineQueue _queue;
		public static uint MaxCoroutines = 1000;

		public static string BuildChunkName(Vector3 v)
		{
			return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
		}

		IEnumerator BuildWorld()
		{
			_building = true;
			Posx = (int)Mathf.Floor(Player.transform.position.x / ChunkSize);
			Posz = (int)Mathf.Floor(Player.transform.position.z / ChunkSize);
			
			float totalChunks = Mathf.Pow(Radius * 2 + 1, 2) * ColumnHeight * 2;
			int processedChunks = 0;

			for (var z = -Radius; z <= Radius; z++)
				for (var x = -Radius; x <= Radius; x++)
					for (var y = 0; y < ColumnHeight; y++)
					{
						// player position needs to be converted into chunk position
						var chunkPosition = new Vector3((x + Posx) * ChunkSize,
														y * ChunkSize,
														(Posz + z) * ChunkSize);

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
							Chunks.TryAdd(c.ChunkGameObject.name, c); // whatever is new is set to DRAW
						}
						
						// loading bar update
						if (_firstBuild)
						{
							processedChunks++;
							LoadingAmount.value = processedChunks / totalChunks * 100;
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

				if (_firstBuild)
				{
					processedChunks++;
					LoadingAmount.value = processedChunks / totalChunks * 100;
				}

				// drawing one by one was cool for testing but it is not in a real game
				// yield return null;
			}

			yield return null;

			if (_firstBuild)
			{
				Player.SetActive(true);

				// disable UI
				LoadingAmount.gameObject.SetActive(false);
				MainCamera.gameObject.SetActive(false);
				PlayButton.gameObject.SetActive(false);
				_firstBuild = false;
			}

			_building = false;
		}

		void BuildChunkAt(int x, int y, int z)
		{
			Vector3 chunkPosition = new Vector3(x * ChunkSize,
				y * ChunkSize,
				z * ChunkSize);

			string chunkName = BuildChunkName(chunkPosition);
			Chunk c;

			if (!Chunks.TryGetValue(chunkName, out c))
			{
				c = new Chunk(chunkPosition, TextureAtlas, chunkName);
				c.ChunkGameObject.transform.parent = transform;
				Chunks.TryAdd(c.ChunkGameObject.name, c);
			}
		}

		IEnumerator BuildRecursiveWorld(int x, int y, int z, int radius)
		{
			radius--;
			if (radius <= 0) yield break;

			// build chunk front
			BuildChunkAt(x, y, z + 1);
			StartCoroutine(BuildRecursiveWorld(x, y, z + 1, radius));
			yield return null;

			// build chunk back
			BuildChunkAt(x, y, z - 1);
			StartCoroutine(BuildRecursiveWorld(x, y, z - 1, radius));
			yield return null;

			// build chunk left
			BuildChunkAt(x - 1, y, z);
			StartCoroutine(BuildRecursiveWorld(x - 1, y, z, radius));
			yield return null;

			// build chunk right
			BuildChunkAt(x + 1, y, z);
			StartCoroutine(BuildRecursiveWorld(x + 1, y, z, radius));
			yield return null;

			// build chunk up
			BuildChunkAt(x, y + 1, z);
			StartCoroutine(BuildRecursiveWorld(x, y + 1, z, radius));
			yield return null;

			// build chunk down
			BuildChunkAt(x, y - 1, z);
			StartCoroutine(BuildRecursiveWorld(x, y - 1, z, radius));
			yield return null;
		}

		IEnumerator DrawChunks()
		{
			foreach (KeyValuePair<string, Chunk> c in Chunks)
			{
				if (c.Value.Status == Chunk.ChunkStatus.Draw)
				{
					c.Value.DrawChunk();
				}

				if(c.Value.ChunkGameObject 
				   && Vector3.Distance(Player.transform.position, c.Value.ChunkGameObject.transform.position) > Radius * ChunkSize)
					ToRemove.Add(c.Key);

				yield return null;
			}
		}

		void RemoveOldChunks()
		{
			foreach (var chunkName in ToRemove)
			{
				Chunk c;
				if (!Chunks.TryGetValue(chunkName, out c)) continue;
				Destroy(c.ChunkGameObject);
				Chunks.TryRemove(chunkName, out c);
				//yield return null;
			}
		}

		private void DisableUI()
		{
			LoadingAmount.gameObject.SetActive(false);
			//MainCamera.gameObject.SetActive(false);
			PlayButton.gameObject.SetActive(false);
		}

		// need to be public so PlayButton can access it
		public void StartBuild()
		{
			_queue.Run(BuildWorld());
		}

		public void BuildNearPlayer()
		{
			// we stop any existing build that is going on - this will stop all the recursive calls as well
			StopCoroutine("BuildRecursiveWorld");
			_queue.Run(BuildRecursiveWorld(
				(int)(Player.transform.position.x / ChunkSize),
				(int)(Player.transform.position.y / ChunkSize),
				(int)(Player.transform.position.z / ChunkSize),
				Radius));
		}

		void Start()
		{
			// temporary solution to avoid pointless clicking
			DisableUI();

			Vector3 playerPos = Player.transform.position;
			Player.transform.position = new Vector3(playerPos.x, 
				Utils.GenerateHeight(playerPos.x, playerPos.z) + 1,
				playerPos.z);

			LastBuildPos = Player.transform.position;

			// to be sure player won't fall through the world that hasn't been build yet
			Player.SetActive(false);

			_firstBuild = true;
			Chunks = new ConcurrentDictionary<string, Chunk>();
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;

			_queue = new CoroutineQueue(MaxCoroutines, StartCoroutine);

			//build starting chunk
			BuildChunkAt((int)(Player.transform.position.x / ChunkSize),
				(int)(Player.transform.position.y / ChunkSize),
				(int)(Player.transform.position.z / ChunkSize));

			//draw it
			_queue.Run(DrawChunks());

			//create a bigger world
			_queue.Run(BuildRecursiveWorld((int)(Player.transform.position.x / ChunkSize),
				(int)(Player.transform.position.y / ChunkSize),
				(int)(Player.transform.position.z / ChunkSize), Radius));
		}

		// previous one
		//private void Update()
		//{
		//	if(!_building && !_firstBuild)
		//		StartCoroutine(BuildWorld());
		//}

		void Update()
		{
			// test how far the player has moved
			Vector3 movement = LastBuildPos - Player.transform.position;
			if (movement.magnitude > ChunkSize)
			{
				LastBuildPos = Player.transform.position;
				BuildNearPlayer();
			}

			if (!Player.activeSelf)
			{
				Player.SetActive(true);
				_firstBuild = false;
			}

			_queue.Run(DrawChunks());
			RemoveOldChunks();
		}
	}
}