using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
	// we don't Block to be MonoBehavior because that would add a lot of additional stuff limiting performance
	public class ChunkMB : MonoBehaviour
	{
		Chunk owner;
		public ChunkMB() { }
		public void SetOwner(Chunk o)
		{
			owner = o;
			InvokeRepeating("SaveProgress", 10, 100); // repeats every 100 seconds
		}

		// I have been hit please heal me after 3 seconds
		public IEnumerator HealBlock(Vector3 bpos)
		{
			yield return new WaitForSeconds(3);
			int x = (int)bpos.x;
			int y = (int)bpos.y;
			int z = (int)bpos.z;

			// if it hasn't been already destroy reset it
			if (owner.Blocks[x, y, z].Type != Block.BlockType.Air)
				owner.Blocks[x, y, z].Reset();
		}

		public void SaveProgress()
		{
			if(owner.Changed)
			{
				owner.Save();
				owner.Changed = false;
			}
		}
	}
}
