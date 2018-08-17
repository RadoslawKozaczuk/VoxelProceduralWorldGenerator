using System;
using UnityEngine;

namespace Assets.Scripts
{
    public class Block
    {
        public Block(BlockType type, Vector3 localPos, Chunk o)
        {
            //Type = type;
            //HealthType = HealthLevel.NoCrack;
            //CurrentHealth = _blockHealthMax[(int)Type]; // maximum health
            //Owner = o;
            //LocalPosition = localPos;
        }

        //public void Reset()
        //{
        //    HealthType = HealthLevel.NoCrack;
        //    CurrentHealth = _blockHealthMax[(int)Type];
        //    Owner.DestroyMeshAndCollider();
        //    Owner.CreateMeshAndCollider();
        //}
        
        //public bool BuildBlock(TerrainGenerator.BlockType type)
        //{
        //    if (type == TerrainGenerator.BlockType.Water)
        //    {
        //        Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.Flow(
        //            this,
        //            TerrainGenerator.BlockType.Water,
        //            _blockHealthMax[(int)TerrainGenerator.BlockType.Water],
        //            10));
        //    }
        //    else if (type == TerrainGenerator.BlockType.Sand)
        //    {
        //        Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.Drop(
        //            this,
        //            TerrainGenerator.BlockType.Sand));
        //    }
        //    else
        //    {
        //        Type = type;
        //        CurrentHealth = _blockHealthMax[(int)Type]; // maximum health
        //        Owner.DestroyMeshAndCollider();
        //        Owner.CreateMeshAndCollider();
        //    }

        //    return true;
        //}

        /// <summary>
        /// returns true if the block has been destroyed and false if it has not
        /// </summary>
        //public bool HitBlock()
        //{
        //    if (CurrentHealth == -1) return false;
        //    CurrentHealth--;
        //    HealthType++;

        //    // if the block was hit for the first time start the coroutine
        //    if (CurrentHealth == _blockHealthMax[(int)Type] - 1)
        //        Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.HealBlock(LocalPosition));

        //    if (CurrentHealth <= 0)
        //    {
        //        Type = TerrainGenerator.BlockType.Air;
        //        HealthType = HealthLevel.NoCrack; // we change it to NoCrack because we don't want cracks to appear on air
        //        Owner.DestroyMeshAndCollider();
        //        Owner.CreateMeshAndCollider();
        //        Owner.UpdateChunk();
        //        return true;
        //    }
            
        //    return false;
        //}
        
        // convert x, y or z to what it is in the neighboring block
        int ConvertBlockIndexToLocal(int i)
        {
            if (i <= -1)
                return World.ChunkSize + i;
            if (i >= World.ChunkSize)
                return i - World.ChunkSize;
            return i;
        }
    }
}