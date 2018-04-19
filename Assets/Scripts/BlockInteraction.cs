using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class BlockInteraction : MonoBehaviour
	{
		public GameObject Cam;

		private const float AttackRange = 4.0f;

		private Block.BlockType _buildBlockType = Block.BlockType.Stone;

		// Use this for initialization
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			if (!Input.anyKey) return;

			CheckForBuildBlockType();

			// left mouse click is going to destroy block and the right mouse click will add a block
			if (!(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
				return;

			RaycastHit hit;

			//for mouse clicking
			//Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
			//if ( Physics.Raycast (ray,out hit,10)) 
			//{

			// DEBUG - visible ray
			//Debug.DrawRay(Cam.transform.position, Cam.transform.forward, Color.red, 1000, true);

			//for cross hairs
			if (!Physics.Raycast(Cam.transform.position, Cam.transform.forward, out hit, AttackRange))
				return;

			Vector3 hitBlock = Input.GetMouseButtonDown(0)
				? hit.point - hit.normal / 2.0f // central point
				: hit.point + hit.normal / 2.0f; // next to the one that we are pointing at

			// the absolute value of the block we are after
			Block b = World.GetWorldBlock(hitBlock);
			
			float thisChunkx = hit.collider.gameObject.transform.position.x;
			float thisChunky = hit.collider.gameObject.transform.position.y;
			float thisChunkz = hit.collider.gameObject.transform.position.z;

			// we keep performing check if the block is completely gone
			Chunk hitc;
			if (!World.Chunks.TryGetValue(int.Parse(hit.collider.gameObject.name), out hitc)) // if we hit something
				return;

			// DEBUG - calculated coordinates
			Debug.Log("block hit x" + hitBlock.x + " y" + hitBlock.y + " z" + hitBlock.z
				+ " chunk x" + thisChunkx + " y" + thisChunky + " z" + thisChunkz
				+ " HitColliderName: " + hit.collider.gameObject.name + "block type: " + b.Type);

			hitc = b.Owner;

			bool update = Input.GetMouseButtonDown(0) 
				? b.HitBlock() // if destroyed
				: b.BuildBlock(_buildBlockType); // always returns true

			if (!update) return;

			hitc.Changed = true;
			RedrawNeighbours(b.Position, thisChunkx, thisChunky, thisChunkz);
		}

		void CheckForBuildBlockType()
		{
			if (Input.GetKeyDown("1"))
			{
				_buildBlockType = Block.BlockType.Grass;
				Debug.Log("Change build block type to Grass");
			}
			else if (Input.GetKeyDown("2"))
			{
				_buildBlockType = Block.BlockType.Dirt;
				Debug.Log("Change build block type to Dirt");
			}
			else if (Input.GetKeyDown("3"))
			{
				_buildBlockType = Block.BlockType.Stone;
				Debug.Log("Change build block type to Stone");
			}
			else if (Input.GetKeyDown("4"))
			{
				_buildBlockType = Block.BlockType.Diamond;
				Debug.Log("Change build block type to Diamond");
			}
			else if (Input.GetKeyDown("5"))
			{
				_buildBlockType = Block.BlockType.Bedrock;
				Debug.Log("Change build block type to Bedrock");
			}
			else if (Input.GetKeyDown("6"))
			{
				_buildBlockType = Block.BlockType.Redstone;
				Debug.Log("Change build block type to Redstone");
			}
			else if (Input.GetKeyDown("7"))
			{
				_buildBlockType = Block.BlockType.Sand;
				Debug.Log("Change build block type to Sand");
			}
			else if (Input.GetKeyDown("8"))	
			{
				_buildBlockType = Block.BlockType.Water;
				Debug.Log("Change build block type to Water");
			}
		}

		private void RedrawNeighbours(Vector3 position, float chunkX, float chunkY, float chunkZ)
		{
			var updates = new List<int>();

			// if the block is on the edge of the chunk we need to inform neighbor chunk
			if (position.x == 0)
				updates.Add(World.BuildChunkName((int)chunkX - World.ChunkSize, (int)chunkY, (int)chunkZ));
			if (position.x == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName((int)chunkX + World.ChunkSize, (int)chunkY, (int)chunkZ));
			if (position.y == 0)
				updates.Add(World.BuildChunkName((int)chunkX, (int)chunkY - World.ChunkSize, (int)chunkZ));
			if (position.y == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName((int)chunkX, (int)chunkY + World.ChunkSize, (int)chunkZ));
			if (position.z == 0)
				updates.Add(World.BuildChunkName((int)chunkX, (int)chunkY, (int)chunkZ - World.ChunkSize));
			if (position.z == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName((int)chunkX, (int)chunkY, (int)chunkZ + World.ChunkSize));

			foreach (int cname in updates)
			{
				Chunk c;
				if (!World.Chunks.TryGetValue(cname, out c))
					continue;
				
				c.Redraw();
			}
		}
	}
}
