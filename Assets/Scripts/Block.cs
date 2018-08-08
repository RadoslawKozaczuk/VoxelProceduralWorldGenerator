using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        enum Cubeside : byte { Top = 1, Bottom = 2, Left = 4, Right = 8, Front = 16, Back = 32 }

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

        // current health as a number of hit points
        public int CurrentHealth; // start set to maximum health the block can be

        // this corresponds to the BlockType enum, so for example Grass can be hit 3 times
        readonly int[] _blockHealthMax = {
            3, 4, 4, -1, 4, 3, 3, 3, 3,
            8, // water
			3, // grass
			0  // air
		}; // -1 means the block cannot be destroyed

        readonly GameObject _parent;

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
        
        public Block(BlockType type, Vector3 localPos, GameObject p, Chunk o)
        {
            Type = type;
            HealthType = HealthLevel.NoCrack;
            CurrentHealth = _blockHealthMax[(int)_type]; // maximum health
            Owner = o;
            _parent = p;
            LocalPosition = localPos;
        }

        public void Reset()
        {
            HealthType = HealthLevel.NoCrack;
            CurrentHealth = _blockHealthMax[(int)Type];
            Owner.DestroyMeshAndCollider();
            Owner.CreateMeshAndCollider();
        }

        // BUG: If we build where we stand player falls into the block
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
        
        public void CreateQuads()
        {
            if (Type == BlockType.Air) return;
            
            // local position coresponds to indexes in the table
            int x = (int)LocalPosition.x,
                y = (int)LocalPosition.y,
                z = (int)LocalPosition.z;

            Cubeside faces = 0;
            var size = 0;
            if (ShouldCreateQuad(x, y, z + 1)) { faces |= Cubeside.Front;  size += 4; }
            if (ShouldCreateQuad(x, y, z - 1)) { faces |= Cubeside.Back;   size += 4; }
            if (ShouldCreateQuad(x, y + 1, z)) { faces |= Cubeside.Top;    size += 4; }
            if (ShouldCreateQuad(x, y - 1, z)) { faces |= Cubeside.Bottom; size += 4; }
            if (ShouldCreateQuad(x - 1, y, z)) { faces |= Cubeside.Left;   size += 4; }
            if (ShouldCreateQuad(x + 1, y, z)) { faces |= Cubeside.Right;  size += 4; }

            // no faces means no mesh needed
            if (faces == 0) return;

            int typeIndex = (int)Type;
            var differentTop = Type == BlockType.Grass;

            // all possible UVs
            // top uvs are used only if top side of the block is different
            Vector2 uv00, uv10, uv01, uv11, uv00top, uv10top, uv01top, uv11top;
            if (differentTop) // different top
            {
                var restIndex = typeIndex - 10;
                uv00 = _blockUVs[restIndex, 0];
                uv10 = _blockUVs[restIndex, 1];
                uv01 = _blockUVs[restIndex, 2];
                uv11 = _blockUVs[restIndex, 3];

                uv00top = _blockUVs[typeIndex, 0];
                uv10top = _blockUVs[typeIndex, 1];
                uv01top = _blockUVs[typeIndex, 2];
                uv11top = _blockUVs[typeIndex, 3];
            }
            else
            {
                uv00 = _blockUVs[typeIndex, 0];
                uv10 = _blockUVs[typeIndex, 1];
                uv01 = _blockUVs[typeIndex, 2];
                uv11 = _blockUVs[typeIndex, 3];

                uv00top = Vector3.zero;
                uv10top = Vector3.zero;
                uv01top = Vector3.zero;
                uv11top = Vector3.zero;
            }
            
            var suvs = new List<Vector2>(size);
            var mesh = new Mesh();
            var verticies = new Vector3[size];
            var normals = new Vector3[size];
            var uvs = new Vector2[size];
            var triangles = new int[(int)(1.5f * size)];
            var index = 0;
            var triIndex = 0;

            if (faces.HasFlag(Cubeside.Top))
            {
                CalculateTriangles(ref triangles, index, ref triIndex);
                AddSuvs(suvs);

                verticies[index] = _p7;
                normals[index] = Vector3.up;
                uvs[index] = differentTop ? uv11top : uv11;
                index++;

                verticies[index] = _p6;
                normals[index] = Vector3.up;
                uvs[index] = differentTop ? uv01top : uv01;
                index++;

                verticies[index] = _p5;
                normals[index] = Vector3.up;
                uvs[index] = differentTop ? uv00top : uv00;
                index++;

                verticies[index] = _p4;
                normals[index] = Vector3.up;
                uvs[index] = differentTop ? uv10top : uv10;
                index++;
            }

            if (faces.HasFlag(Cubeside.Bottom))
            {
                CalculateTriangles(ref triangles, index, ref triIndex);
                AddSuvs(suvs);

                verticies[index] = _p0;
                normals[index] = Vector3.down;
                uvs[index] = uv11;
                index++;

                verticies[index] = _p1;
                normals[index] = Vector3.down;
                uvs[index] = uv01;
                index++;

                verticies[index] = _p2;
                normals[index] = Vector3.down;
                uvs[index] = uv00;
                index++;

                verticies[index] = _p3;
                normals[index] = Vector3.down;
                uvs[index] = uv10;
                index++;
            }

            if (faces.HasFlag(Cubeside.Left))
            {
                CalculateTriangles(ref triangles, index, ref triIndex);
                AddSuvs(suvs);

                verticies[index] = _p7;
                normals[index] = Vector3.left;
                uvs[index] = uv11;
                index++;

                verticies[index] = _p4;
                normals[index] = Vector3.left;
                uvs[index] = uv01;
                index++;

                verticies[index] = _p0;
                normals[index] = Vector3.left;
                uvs[index] = uv00;
                index++;

                verticies[index] = _p3;
                normals[index] = Vector3.left;
                uvs[index] = uv10;
                index++;
            }

            if (faces.HasFlag(Cubeside.Right))
            {
                CalculateTriangles(ref triangles, index, ref triIndex);
                AddSuvs(suvs);

                verticies[index] = _p5;
                normals[index] = Vector3.right;
                uvs[index] = uv11;
                index++;

                verticies[index] = _p6;
                normals[index] = Vector3.right;
                uvs[index] = uv01;
                index++;

                verticies[index] = _p2;
                normals[index] = Vector3.right;
                uvs[index] = uv00;
                index++;

                verticies[index] = _p1;
                normals[index] = Vector3.right;
                uvs[index] = uv10;
                index++;
            }

            if (faces.HasFlag(Cubeside.Front))
            {
                CalculateTriangles(ref triangles, index, ref triIndex);
                AddSuvs(suvs);

                verticies[index] = _p4;
                normals[index] = Vector3.forward;
                uvs[index] = uv11;
                index++;

                verticies[index] = _p5;
                normals[index] = Vector3.forward;
                uvs[index] = uv01;
                index++;

                verticies[index] = _p1;
                normals[index] = Vector3.forward;
                uvs[index] = uv00;
                index++;

                verticies[index] = _p0;
                normals[index] = Vector3.forward;
                uvs[index] = uv10;
                index++;
            }

            if (faces.HasFlag(Cubeside.Back))
            {
                CalculateTriangles(ref triangles, index, ref triIndex);
                AddSuvs(suvs);

                verticies[index] = _p6;
                normals[index] = Vector3.back;
                uvs[index] = uv11;
                index++;

                verticies[index] = _p7;
                normals[index] = Vector3.back;
                uvs[index] = uv01;
                index++;

                verticies[index] = _p3;
                normals[index] = Vector3.back;
                uvs[index] = uv00;
                index++;

                verticies[index] = _p2;
                normals[index] = Vector3.back;
                uvs[index] = uv10;
                index++;
            }
            
            mesh.vertices = verticies;
            mesh.normals = normals;

            // Uvs maps the texture over the surface
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.SetUVs(1, suvs);

            mesh.RecalculateBounds();

            var cube = new GameObject("Quad");
            cube.transform.position = LocalPosition;
            cube.transform.parent = _parent.transform;

            var meshFilter = (MeshFilter)cube.AddComponent(typeof(MeshFilter));
            meshFilter.mesh = mesh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CalculateTriangles(ref int[] triangles, int index, ref int triIndex)
        {
            triangles[triIndex++] = index + 3;
            triangles[triIndex++] = index + 1;
            triangles[triIndex++] = index;
            triangles[triIndex++] = index + 3;
            triangles[triIndex++] = index + 2;
            triangles[triIndex++] = index + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddSuvs(List<Vector2> suvs)
        {
            suvs.Add(_crackUVs[(int)HealthType, 3]); // top right corner
            suvs.Add(_crackUVs[(int)HealthType, 2]); // top left corner
			suvs.Add(_crackUVs[(int)HealthType, 0]); // bottom left corner
            suvs.Add(_crackUVs[(int)HealthType, 1]); // bottom right corner
        }
    }
}