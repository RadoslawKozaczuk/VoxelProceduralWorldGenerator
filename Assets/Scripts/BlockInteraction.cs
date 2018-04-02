using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class BlockInteraction : MonoBehaviour
	{
		public GameObject Cam;

		private const float AttackRange = 4.0f;

		// Use this for initialization
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
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
			if (!World.Chunks.TryGetValue(hit.collider.gameObject.name, out hitc)) // if we hit something
				return;
			
			// DEBUG - calculated coordinates
			//Debug.Log("block hit x" + x + " y" + y + " z" + z 
			//	+ " chunk x" + thisChunkx + " y" + thisChunky + " z" + thisChunkz 
			//	+ "HitColliderName: " + hit.collider.gameObject.name);

			hitc = b.Owner;

			bool update = Input.GetMouseButtonDown(0) 
				? b.HitBlock() // if destroyed
				: b.BuildBlock(Block.BlockType.Stone); // always returns true

			if (!update) return;
			
			RedrawNeighbours(b.Position, thisChunkx, thisChunky, thisChunkz);
		}

		private void RedrawNeighbours(Vector3 position, float chunkX, float chunkY, float chunkZ)
		{
			var updates = new List<string>();

			// if the block is on the edge of the chunk we need to inform neighbor chunk
			if (position.x == 0)
				updates.Add(World.BuildChunkName(new Vector3(chunkX - World.ChunkSize, chunkY, chunkZ)));
			if (position.x == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName(new Vector3(chunkX + World.ChunkSize, chunkY, chunkZ)));
			if (position.y == 0)
				updates.Add(World.BuildChunkName(new Vector3(chunkX, chunkY - World.ChunkSize, chunkZ)));
			if (position.y == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName(new Vector3(chunkX, chunkY + World.ChunkSize, chunkZ)));
			if (position.z == 0)
				updates.Add(World.BuildChunkName(new Vector3(chunkX, chunkY, chunkZ - World.ChunkSize)));
			if (position.z == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName(new Vector3(chunkX, chunkY, chunkZ + World.ChunkSize)));

			foreach (string cname in updates)
			{
				Chunk c;
				if (!World.Chunks.TryGetValue(cname, out c))
					continue;

				c.Redraw();
			}
		}
	}
}
