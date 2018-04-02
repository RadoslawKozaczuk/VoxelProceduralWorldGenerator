using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Realtime.Messaging.Internal;
using System;

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
			BuildChunkAt(
				(int)(Player.transform.position.x / ChunkSize),
				(int)(Player.transform.position.y / ChunkSize),
				(int)(Player.transform.position.z / ChunkSize));

			//draw it
			_queue.Run(DrawChunks());

			//create a bigger world
			_queue.Run(BuildRecursiveWorld(
				(int)(Player.transform.position.x / ChunkSize),
				(int)(Player.transform.position.y / ChunkSize),
				(int)(Player.transform.position.z / ChunkSize),
				Radius));
		}

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
		
		/// <summary>
		/// Returns the block we hit.
		/// </summary>
		/// <param name="pos">hit point coordinates</param>
		public static Block GetWorldBlock(Vector3 pos)
		{
			// with chuck we are talking about
			// for example if we are in block 4 we round it down to 0 which means we are in chunk 0
			int chunkX = pos.x < 0
				// this plus one is here to avoid problem with rounding which occurs around the value -16
				? (int)((Mathf.Round(pos.x - ChunkSize) + 1) / ChunkSize) * ChunkSize
				: (int)(Mathf.Round(pos.x) / ChunkSize) * ChunkSize;

			int chunkY = pos.y < 0 
				? (int)((Mathf.Round(pos.y - ChunkSize) + 1) / ChunkSize) * ChunkSize 
				: (int)(Mathf.Round(pos.y) / ChunkSize) * ChunkSize;
			
			int chunkZ = pos.z < 0 
				? (int)((Mathf.Round(pos.z - ChunkSize) + 1) / ChunkSize) * ChunkSize 
				: (int)(Mathf.Round(pos.z) / ChunkSize) * ChunkSize;
			
			string chunkName = BuildChunkName(new Vector3(chunkX, chunkY, chunkZ));
			Chunk c;
			if (Chunks.TryGetValue(chunkName, out c))
			{
				// we need to make absolute for all of these because there is no chunk with negative index
				int blx = (int)Mathf.Abs((float)Math.Round(pos.x) - chunkX);
				int bly = (int)Mathf.Abs((float)Math.Round(pos.y) - chunkY);
				int blz = (int)Mathf.Abs((float)Math.Round(pos.z) - chunkZ);
				
				return c.Blocks[blx, bly, blz];
			}
			else return null;
		}

		public static string BuildChunkName(Vector3 v)
		{
			return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
		}

		public static string BuildChunkFileName(Vector3 v)
		{
			return Application.persistentDataPath + "/savedata/Chunk_" 
			                                      + (int) v.x + "_" + (int) v.y + "_" + (int) v.z + "_" 
			                                      + ChunkSize + "_" + Radius + ".dat";
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
					c.Value.DrawChunk();

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

		void BuildNearPlayer()
		{
			// we stop any existing build that is going on - this will stop all the recursive calls as well
			StopCoroutine("BuildRecursiveWorld");
			_queue.Run(BuildRecursiveWorld(
				(int)(Player.transform.position.x / ChunkSize),
				(int)(Player.transform.position.y / ChunkSize),
				(int)(Player.transform.position.z / ChunkSize),
				Radius));
		}


		#region Loading And Menu (not implemented yet)
		private void DisableUI()
		{
			LoadingAmount.gameObject.SetActive(false);
			//MainCamera.gameObject.SetActive(false);
			PlayButton.gameObject.SetActive(false);
		}

		// need to be public so PlayButton can access it
		public void StartBuild()
		{
			if (_stillLoading)
				StartCoroutine(Wait());
		}

		private float _loadingValue = 0;
		private const float DummyLoadingTime = 2.0f;
		private bool _stillLoading = true;
		IEnumerator Wait()
		{
			_stillLoading = true;
			_loadingValue += Time.deltaTime;
			LoadingAmount.value += _loadingValue;
			yield return new WaitForSeconds(DummyLoadingTime);
			_stillLoading = false;
		}
		#endregion

	}
}