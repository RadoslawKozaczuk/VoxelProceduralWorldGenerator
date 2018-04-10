using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
	// we don't Block to be MonoBehavior because that would add a lot of additional stuff limiting performance
	public class ChunkMB : MonoBehaviour
	{
		Chunk _owner;
		public ChunkMB() { }
		public void SetOwner(Chunk o)
		{
			_owner = o;
			InvokeRepeating("SaveProgress", 10, 100); // repeats every 100 seconds
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="startingBlock"></param>
		/// <param name="bt"></param>
		/// <param name="strength">Health value of the block - how far we let the water flow out</param>
		/// <param name="maxsize">Restrict recursive algorithm to grow to big</param>
		/// <returns></returns>
		public IEnumerator Flow(Block startingBlock, Block.BlockType bt, int strength, int maxsize)
		{
			// reduce the strength of the fluid block
			// with each new block created
			if (maxsize <= 0) yield break;
			if (startingBlock == null) yield break; // block would be null if the neighboring location does not exist
			if (strength <= 0) yield break;
			if (startingBlock.Type != Block.BlockType.Air) yield break; // water only spread trough the air
			startingBlock.Type = bt; // BUG: This should also change the parent's game object to apply transparency
			startingBlock.CurrentHealth = strength;
			startingBlock.Owner.Redraw();
			yield return new WaitForSeconds(1); // water spread one block per second

			int x = (int)startingBlock.Position.x;
			int y = (int)startingBlock.Position.y;
			int z = (int)startingBlock.Position.z;

			// flow down if air block beneath
			Block below = startingBlock.GetBlock(x, y - 1, z);
			if (below != null && below.Type == Block.BlockType.Air)
			{
				StartCoroutine(Flow(startingBlock.GetBlock(x, y - 1, z), bt, strength, --maxsize));
			}
			else // flow outward
			{
				--strength;
				--maxsize;
				// flow left
				World.Queue.Run(Flow(startingBlock.GetBlock(x - 1, y, z), bt, strength, maxsize));
				yield return new WaitForSeconds(1);

				// flow right
				World.Queue.Run(Flow(startingBlock.GetBlock(x + 1, y, z), bt, strength, maxsize));
				yield return new WaitForSeconds(1);

				// flow forward
				World.Queue.Run(Flow(startingBlock.GetBlock(x, y, z + 1), bt, strength, maxsize));
				yield return new WaitForSeconds(1);

				// flow back
				World.Queue.Run(Flow(startingBlock.GetBlock(x, y, z - 1), bt, strength, maxsize));
				yield return new WaitForSeconds(1);
			}
		}

		// I have been hit please heal me after 3 seconds
		public IEnumerator HealBlock(Vector3 bpos)
		{
			yield return new WaitForSeconds(3);
			int x = (int)bpos.x;
			int y = (int)bpos.y;
			int z = (int)bpos.z;

			// if it hasn't been already destroy reset it
			if (_owner.Blocks[x, y, z].Type != Block.BlockType.Air)
				_owner.Blocks[x, y, z].Reset();
		}

		public void SaveProgress()
		{
			if(_owner.Changed)
			{
				_owner.Save();
				_owner.Changed = false;
			}
		}
	}
}
