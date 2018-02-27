using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class BlockInteraction : MonoBehaviour
	{

		public GameObject Cam;

		// Use this for initialization
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				RaycastHit hit;

				//for mouse clicking
				//Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
				//if ( Physics.Raycast (ray,out hit,10)) 
				//{

				//for cross hairs
				if (Physics.Raycast(Cam.transform.position, Cam.transform.forward, out hit, 10))
				{
					Vector3 hitBlock = hit.point - hit.normal / 2.0f;

					int x = (int)(Mathf.Round(hitBlock.x) - hit.collider.gameObject.transform.position.x);
					int y = (int)(Mathf.Round(hitBlock.y) - hit.collider.gameObject.transform.position.y);
					int z = (int)(Mathf.Round(hitBlock.z) - hit.collider.gameObject.transform.position.z);

					var updates = new List<string>();
					float thisChunkx = hit.collider.gameObject.transform.position.x;
					float thisChunky = hit.collider.gameObject.transform.position.y;
					float thisChunkz = hit.collider.gameObject.transform.position.z;

					updates.Add(hit.collider.gameObject.name);

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
						if (World.Chunks.TryGetValue(cname, out c))
						{
							// we cannot use normal destroy because it may wait to the next update loop or something which will break the code
							DestroyImmediate(c.ChunkGameObject.GetComponent<MeshFilter>());
							DestroyImmediate(c.ChunkGameObject.GetComponent<MeshRenderer>());
							DestroyImmediate(c.ChunkGameObject.GetComponent<Collider>());
							c.Blocks[x, y, z]. Type = Block.BlockType.Air;
							c.DrawChunk();
						}
					}
				}
			}
		}
	}
}

