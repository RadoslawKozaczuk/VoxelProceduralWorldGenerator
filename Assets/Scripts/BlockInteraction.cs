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

			int x = (int)(Mathf.Round(hitBlock.x) - hit.collider.gameObject.transform.position.x);
			int y = (int)(Mathf.Round(hitBlock.y) - hit.collider.gameObject.transform.position.y);
			int z = (int)(Mathf.Round(hitBlock.z) - hit.collider.gameObject.transform.position.z);
			
			float thisChunkx = hit.collider.gameObject.transform.position.x;
			float thisChunky = hit.collider.gameObject.transform.position.y;
			float thisChunkz = hit.collider.gameObject.transform.position.z;

			// DEBUG - calculated coordinates
			//Debug.Log("block hit x" + x + " y" + y + " z" + z 
			//	+ " chunk x" + thisChunkx + " y" + thisChunky + " z" + thisChunkz 
			//	+ "HitColliderName: " + hit.collider.gameObject.name);

			// we keep performing check if the block is completely gone
			Chunk hitc;
			if (!World.Chunks.TryGetValue(hit.collider.gameObject.name, out hitc)) return; // if we hit something

			bool update = Input.GetMouseButtonDown(0) 
				? hitc.Blocks[x, y, z].HitBlock() // if destroyed
				: hitc.Blocks[x,y,z].BuildBlock(Block.BlockType.Stone); // always return true

			if (!update) return;

			var updates = new List<string>();
			
			// the collider will be destroyed at this moment
			//updates.Add(hit.collider.gameObject.name);

			// if the block is on the edge of the chunk we need to inform neighbor chunk
			//update neighbors?
			if (x == 0)
				updates.Add(World.BuildChunkName(new Vector3(thisChunkx - World.ChunkSize, thisChunky, thisChunkz)));
			if (x == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName(new Vector3(thisChunkx + World.ChunkSize, thisChunky, thisChunkz)));
			if (y == 0)
				updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky - World.ChunkSize, thisChunkz)));
			if (y == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky + World.ChunkSize, thisChunkz)));
			if (z == 0)
				updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky, thisChunkz - World.ChunkSize)));
			if (z == World.ChunkSize - 1)
				updates.Add(World.BuildChunkName(new Vector3(thisChunkx, thisChunky, thisChunkz + World.ChunkSize)));

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
