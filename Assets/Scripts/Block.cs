using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class Block
    {
        // INFO: I constantly lose a lot of time trying to solve out the same freaking error over and over again!
        // So to avoid this crap once for all - every time a new value is added to this enum all saved chunk data becomes INCOMPATIBLE
        // and needs to be DELETED otherwise weird rendering errors will occur and I will lose another hour debugging the crap out of this game!
        public enum BlockType
        {
            Dirt, Stone, Diamond, Bedrock, Redstone, Sand, Leaves, Wood, Woodbase,
            Water,
            Grass, // types that have different textures on different sides are moved at the end just before air
            Air
        }
        public enum HealthLevel { NoCrack, Crack1, Crack2, Crack3, Crack4 }
        [Flags]
        public enum Cubeside : byte { Top = 1, Bottom = 2, Left = 4, Right = 8, Front = 16, Back = 32 }

        BlockType _type;
        public BlockType Type
        {
            get { return _type; }
            set
            {
                _type = value;
                IsSolid = _type != BlockType.Air && _type != BlockType.Water;
            }
        }

        // crack texture
        public HealthLevel HealthType;
        public bool IsSolid;
        public readonly Chunk Owner;
        public Vector3 LocalPosition;
        public Cubeside Faces;

        // current health as a number of hit points
        public int CurrentHealth; // start set to maximum health the block can be

        // this corresponds to the BlockType enum, so for example Grass can be hit 3 times
        readonly int[] _blockHealthMax = {
            3, 4, 4, -1, 4, 3, 3, 3, 3,
            8, // water
			3, // grass
			0  // air
		}; // -1 means the block cannot be destroyed
        
        // assumptions used:
        // coordination start left down corner
        readonly Vector2[,] _blockUVs = { 
								// left-bottom, right-bottom, left-top, right-top
			/*DIRT*/			{new Vector2(0.125f, 0.9375f), new Vector2(0.1875f, 0.9375f),
                                    new Vector2(0.125f, 1.0f), new Vector2(0.1875f, 1.0f)},
			/*STONE*/			{new Vector2(0, 0.875f), new Vector2(0.0625f, 0.875f),
                                    new Vector2(0, 0.9375f), new Vector2(0.0625f, 0.9375f)},
			/*DIAMOND*/			{new Vector2 (0.125f, 0.75f), new Vector2(0.1875f, 0.75f),
                                    new Vector2(0.125f, 0.8125f), new Vector2(0.1875f, 0.81f)},
			/*BEDROCK*/			{new Vector2(0.3125f, 0.8125f), new Vector2(0.375f, 0.8125f),
                                    new Vector2(0.3125f, 0.875f), new Vector2(0.375f, 0.875f)},
			/*REDSTONE*/		{new Vector2(0.1875f, 0.75f), new Vector2(0.25f, 0.75f),
                                    new Vector2(0.1875f, 0.8125f), new Vector2(0.25f, 0.8125f)},
			/*SAND*/			{new Vector2(0.125f, 0.875f), new Vector2(0.1875f, 0.875f),
                                    new Vector2(0.125f, 0.9375f), new Vector2(0.1875f, 0.9375f)},
			/*LEAVES*/			{new Vector2(0.0625f,0.375f), new Vector2(0.125f,0.375f),
                                    new Vector2(0.0625f,0.4375f), new Vector2(0.125f,0.4375f)},
			/*WOOD*/			{new Vector2(0.375f,0.625f), new Vector2(0.4375f,0.625f),
                                    new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f)},
			/*WOODBASE*/		{new Vector2(0.375f,0.625f), new Vector2(0.4375f,0.625f),
                                    new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f)},	    
			
			/*WATER*/			{new Vector2(0.875f,0.125f), new Vector2(0.9375f,0.125f),
                                    new Vector2(0.875f,0.1875f), new Vector2(0.9375f,0.1875f)},

			/*GRASS*/		    {new Vector2(0.125f, 0.375f), new Vector2(0.1875f, 0.375f),
                                    new Vector2(0.125f, 0.4375f), new Vector2(0.1875f, 0.4375f)}
			
			// BUG: Tile sheet provided is broken and some tiles overlaps each other
		};

        readonly Vector2[,] _crackUVs = { 
								// left-bottom, right-bottom, left-top, right-top
			/*NOCRACK*/			{new Vector2(0.6875f,0f), new Vector2(0.75f,0f),
                                    new Vector2(0.6875f,0.0625f), new Vector2(0.75f,0.0625f)},
			/*CRACK1*/			{new Vector2(0.0625f,0f), new Vector2(0.125f,0f),
                                    new Vector2(0.0625f,0.0625f), new Vector2(0.125f,0.0625f)},
			/*CRACK2*/			{new Vector2(0.1875f,0f), new Vector2(0.25f,0f),
                                    new Vector2(0.1875f,0.0625f), new Vector2(0.25f,0.0625f)},
			/*CRACK3*/			{new Vector2(0.3125f,0f), new Vector2(0.375f,0f),
                                    new Vector2(0.3125f,0.0625f), new Vector2(0.375f,0.0625f)},
			/*CRACK4*/			{new Vector2(0.4375f,0f), new Vector2(0.5f,0f),
                                    new Vector2(0.4375f,0.0625f), new Vector2(0.5f,0.0625f)}
        };

        // calculation constants
        // all possible vertices
        static readonly Vector3 _p0 = new Vector3(-0.5f, -0.5f, 0.5f),
                                _p1 = new Vector3(0.5f, -0.5f, 0.5f),
                                _p2 = new Vector3(0.5f, -0.5f, -0.5f),
                                _p3 = new Vector3(-0.5f, -0.5f, -0.5f),
                                _p4 = new Vector3(-0.5f, 0.5f, 0.5f),
                                _p5 = new Vector3(0.5f, 0.5f, 0.5f),
                                _p6 = new Vector3(0.5f, 0.5f, -0.5f),
                                _p7 = new Vector3(-0.5f, 0.5f, -0.5f);
        
        public Block(BlockType type, Vector3 localPos, Chunk o)
        {
            Type = type;
            HealthType = HealthLevel.NoCrack;
            CurrentHealth = _blockHealthMax[(int)_type]; // maximum health
            Owner = o;
            LocalPosition = localPos;
        }

        public void Reset()
        {
            HealthType = HealthLevel.NoCrack;
            CurrentHealth = _blockHealthMax[(int)Type];
            Owner.DestroyMeshAndCollider();
            Owner.CreateMeshAndCollider();
        }
        
        public bool BuildBlock(BlockType type)
        {
            if (type == BlockType.Water)
            {
                Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.Flow(
                    this,
                    BlockType.Water,
                    _blockHealthMax[(int)BlockType.Water],
                    10));
            }
            else if (type == BlockType.Sand)
            {
                Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.Drop(
                    this,
                    BlockType.Sand));
            }
            else
            {
                Type = type;
                CurrentHealth = _blockHealthMax[(int)_type]; // maximum health
                Owner.DestroyMeshAndCollider();
                Owner.CreateMeshAndCollider();
            }

            return true;
        }

        /// <summary>
        /// returns true if the block has been destroyed and false if it has not
        /// </summary>
        public bool HitBlock()
        {
            if (CurrentHealth == -1) return false;
            CurrentHealth--;
            HealthType++;

            // if the block was hit for the first time start the coroutine
            if (CurrentHealth == _blockHealthMax[(int)Type] - 1)
                Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.HealBlock(LocalPosition));

            if (CurrentHealth <= 0)
            {
                _type = BlockType.Air;
                IsSolid = false;
                HealthType = HealthLevel.NoCrack; // we change it to NoCrack because we don't want cracks to appear on air
                Owner.DestroyMeshAndCollider();
                Owner.CreateMeshAndCollider();
                Owner.UpdateChunk();
                return true;
            }
            
            return false;
        }
        
        // convert x, y or z to what it is in the neighboring block
        int ConvertBlockIndexToLocal(int i)
        {
            if (i <= -1)
                return World.ChunkSize + i;
            if (i >= World.ChunkSize)
                return i - World.ChunkSize;
            return i;
        }

        /// <summary>
        /// Returns the block from the chunk
        /// Returns null in case the chunk that the block supposed to be in does no exists
        /// </summary>
        public Block GetBlock(int localX, int localY, int localZ)
        {
            // block is in this chunk
            if (localX >= 0 && localX < World.ChunkSize &&
                localY >= 0 && localY < World.ChunkSize &&
                localZ >= 0 && localZ < World.ChunkSize)
                return Owner.Blocks[localX, localY, localZ];

            // this chunk xyz
            int thisChunkX, thisChunkY, thisChunkZ;

            // deterining the chuml
            if (localX < 0)
                thisChunkX = Owner.X - 1; // pod warunkiem że Owner.X istnieje
            else if (localX >= World.ChunkSize)
                thisChunkX = Owner.X + 1;
            else
                thisChunkX = Owner.X;

            if (localY < 0)
                thisChunkY = Owner.Y - 1; // pod warunkiem że Owner.Z istnieje
            else if (localY >= World.ChunkSize)
                thisChunkY = Owner.Y + 1;
            else
                thisChunkY = Owner.Y;

            if (localZ < 0)
                thisChunkZ = Owner.Z - 1; // pod warunkiem że Owner.Z istnieje
            else if (localZ >= World.ChunkSize)
                thisChunkZ = Owner.Z + 1;
            else
                thisChunkZ = Owner.Z;

            if (localX < 0 || localX >= World.WorldSizeX 
                || localY < 0 || localY >= World.WorldSizeY 
                || localZ < 0 || localZ >= World.WorldSizeZ)
            {
                // coordinates point at out side of the world
                return null; // block does not exist
            }
            
            Chunk chunk = World.Chunks[thisChunkX, thisChunkY, thisChunkZ];
			return chunk.Blocks[
                ConvertBlockIndexToLocal(localX),
                ConvertBlockIndexToLocal(localY),
                ConvertBlockIndexToLocal(localZ)];
		}

		bool ShouldCreateQuad(int x, int y, int z)
		{
			var target = GetBlock(x, y, z);
			if (target == null) return true;
			if (Type == BlockType.Water && target.Type == BlockType.Water) return false;

			return !(target.IsSolid && IsSolid);
		}
        
        public void CalculateMeshSize(ref int terrainMeshSize, ref int waterMeshSize)
        {
            if (Type == BlockType.Air) return;
            
            // local position coresponds to indexes in the table
            int x = (int)LocalPosition.x,
                y = (int)LocalPosition.y,
                z = (int)LocalPosition.z;

            var size = 0;
            if (ShouldCreateQuad(x, y, z + 1)) { Faces |= Cubeside.Front; size += 4; }
            if (ShouldCreateQuad(x, y, z - 1)) { Faces |= Cubeside.Back; size += 4; }
            if (ShouldCreateQuad(x, y + 1, z)) { Faces |= Cubeside.Top; size += 4; }
            if (ShouldCreateQuad(x, y - 1, z)) { Faces |= Cubeside.Bottom; size += 4; }
            if (ShouldCreateQuad(x - 1, y, z)) { Faces |= Cubeside.Left; size += 4; }
            if (ShouldCreateQuad(x + 1, y, z)) { Faces |= Cubeside.Right; size += 4; }

            if (Type == BlockType.Water)
                waterMeshSize += size;
            else
                terrainMeshSize += size;
        }

        public void CreateQuads(ref int index, ref int triIndex,
            ref Vector3[] verticies, ref Vector3[] normals, ref Vector2[] uvs, ref List<Vector2> suvs, ref int[] triangles,
            Vector3 offset)
        {
            int typeIndex = (int)Type;
            
            var differentTop = Type == BlockType.Grass;
            // all possible UVs
            // top uvs are used only if top side of the block is different
            Vector2 uv00, uv10, uv01, uv11;
            if (differentTop) // different top
            {
                // typeIndex in this case referes only to the top side
                Vector2 uv00top = _blockUVs[typeIndex, 0],
                        uv10top = _blockUVs[typeIndex, 1],
                        uv01top = _blockUVs[typeIndex, 2],
                        uv11top = _blockUVs[typeIndex, 3];

                // rest sides have to be determined
                var restIndex = typeIndex - 10;
                uv00 = _blockUVs[restIndex, 0];
                uv10 = _blockUVs[restIndex, 1];
                uv01 = _blockUVs[restIndex, 2];
                uv11 = _blockUVs[restIndex, 3];
                
                if (Faces.HasFlag(Cubeside.Top))
                {
                    AddQuadComponents(ref index,
                        ref normals, Vector3.up,
                        ref uvs, uv11top, uv01top, uv00top, uv10top,
                        ref verticies, _p7 + offset, _p6 + offset, _p5 + offset, _p4 + offset,
                        suvs,
                        ref triangles, ref triIndex);
                }
            }
            else
            {
                uv00 = _blockUVs[typeIndex, 0];
                uv10 = _blockUVs[typeIndex, 1];
                uv01 = _blockUVs[typeIndex, 2];
                uv11 = _blockUVs[typeIndex, 3];

                if (Faces.HasFlag(Cubeside.Top))
                {
                    AddQuadComponents(ref index,
                        ref normals, Vector3.up,
                        ref uvs, uv11, uv01, uv00, uv10,
                        ref verticies, _p7 + offset, _p6 + offset, _p5 + offset, _p4 + offset,
                        suvs,
                        ref triangles, ref triIndex);
                }
            }
            
            if (Faces.HasFlag(Cubeside.Bottom))
            {
                AddQuadComponents(ref index,
                    ref normals, Vector3.down,
                    ref uvs, uv11, uv01, uv00, uv10,
                    ref verticies, _p0 + offset, _p1 + offset, _p2 + offset, _p3 + offset,
                    suvs,
                    ref triangles, ref triIndex);
            }

            if (Faces.HasFlag(Cubeside.Left))
            {
                AddQuadComponents(ref index,
                    ref normals, Vector3.left,
                    ref uvs, uv11, uv01, uv00, uv10,
                    ref verticies, _p7 + offset, _p4 + offset, _p0 + offset, _p3 + offset,
                    suvs,
                    ref triangles, ref triIndex);
            }

            if (Faces.HasFlag(Cubeside.Right))
            {
                AddQuadComponents(ref index,
                    ref normals, Vector3.right,
                    ref uvs, uv11, uv01, uv00, uv10,
                    ref verticies, _p5 + offset, _p6 + offset, _p2 + offset, _p1 + offset,
                    suvs,
                    ref triangles, ref triIndex);
            }

            if (Faces.HasFlag(Cubeside.Front))
            {
                AddQuadComponents(ref index,
                    ref normals, Vector3.forward,
                    ref uvs, uv11, uv01, uv00, uv10,
                    ref verticies, _p4 + offset, _p5 + offset, _p1 + offset, _p0 + offset,
                    suvs,
                    ref triangles, ref triIndex);
            }

            if (Faces.HasFlag(Cubeside.Back))
            {
                AddQuadComponents(ref index, 
                    ref normals, Vector3.back, 
                    ref uvs, uv11, uv01, uv00, uv10, 
                    ref verticies, _p6 + offset, _p7 + offset, _p3 + offset, _p2 + offset,
                    suvs,
                    ref triangles, ref triIndex);
            }
        }
        
        void AddQuadComponents(ref int index, ref Vector3[] normals, Vector3 normal, 
            ref Vector2[] uvs, Vector2 uv11, Vector2 uv01, Vector2 uv00, Vector2 uv10,
            ref Vector3[] verticies, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p4,
            List<Vector2> suvs,
            ref int[] triangles, ref int triIndex)
        {
            // add normals
            normals[index] = normal;
            normals[index + 1] = normal;
            normals[index + 2] = normal;
            normals[index + 3] = normal;

            // add uvs
            uvs[index] = uv11;
            uvs[index + 1] = uv01;
            uvs[index + 2] = uv00;
            uvs[index + 3] = uv10;
            
            // add verticies
            verticies[index] = p0;
            verticies[index + 1] = p1;
            verticies[index + 2] = p2;
            verticies[index + 3] = p4;
            
            // add suvs
            suvs.Add(_crackUVs[(int)HealthType, 3]); // top right corner
            suvs.Add(_crackUVs[(int)HealthType, 2]); // top left corner
            suvs.Add(_crackUVs[(int)HealthType, 0]); // bottom left corner
            suvs.Add(_crackUVs[(int)HealthType, 1]); // bottom right corner

            // add triangles
            triangles[triIndex++] = index + 3;
            triangles[triIndex++] = index + 1;
            triangles[triIndex++] = index;
            triangles[triIndex++] = index + 3;
            triangles[triIndex++] = index + 2;
            triangles[triIndex++] = index + 1;

            index += 4;
        }
    }
}