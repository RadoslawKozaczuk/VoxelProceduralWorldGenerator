using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class Block
	{
		public enum BlockType
		{
			Dirt, Stone, Diamond, Bedrock, Redstone, Sand,
			Water,
			Grass, // types that have different textures on different sides are moved at the end just before air
			Air
		}
		public enum HealthLevel { NoCrack, Crack1, Crack2, Crack3, Crack4 }
		enum Cubeside { Bottom, Top, Left, Right, Front, Back }
		
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
		public Vector3 Position;

		// current health as a number of hit points
		public int CurrentHealth; // start set to maximum health the block can be

		// this corresponds to the BlockType enum, so for example Grass can be hit 3 times
		readonly int[] _blockHealthMax = { 
			3, 4, 4, -1, 4, 3,
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

			/*WATER*/			{new Vector2(0.875f,0.125f), new Vector2(0.9375f,0.125f),
									new Vector2(0.875f,0.1875f), new Vector2(0.9375f,0.1875f)},

			/*GRASS TOP*/		{new Vector2(0.125f, 0.375f), new Vector2(0.1875f, 0.375f),
									new Vector2(0.125f, 0.4375f), new Vector2(0.1875f, 0.4375f)},
			/*GRASS SIDE*/		{new Vector2(0.1875f, 0.9375f), new Vector2(0.25f, 0.9375f),
									new Vector2(0.1875f, 1.0f), new Vector2(0.25f, 1.0f)}
			
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
		//all possible vertices
		static readonly Vector3 _p0 = new Vector3(-0.5f, -0.5f, 0.5f),
								_p1 = new Vector3(0.5f, -0.5f, 0.5f),
								_p2 = new Vector3(0.5f, -0.5f, -0.5f),
								_p3 = new Vector3(-0.5f, -0.5f, -0.5f),
								_p4 = new Vector3(-0.5f, 0.5f, 0.5f),
								_p5 = new Vector3(0.5f, 0.5f, 0.5f),
								_p6 = new Vector3(0.5f, 0.5f, -0.5f),
								_p7 = new Vector3(-0.5f, 0.5f, -0.5f);
		
		static readonly Vector3[] _downNormals = {Vector3.down, Vector3.down, Vector3.down, Vector3.down},
								  _upNormals = {Vector3.up, Vector3.up, Vector3.up, Vector3.up},
								  _leftNormals = {Vector3.left, Vector3.left, Vector3.left, Vector3.left},
								  _rightNormals = {Vector3.right, Vector3.right, Vector3.right, Vector3.right},
								  _forwardNormals = {Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward},
								  _backNormals = {Vector3.back, Vector3.back, Vector3.back, Vector3.back};

		
		public Block(BlockType type, Vector3 pos, GameObject p, Chunk o)
		{
			Type = type;
			HealthType = HealthLevel.NoCrack;
			CurrentHealth = _blockHealthMax[(int)_type]; // maximum health
			Owner = o;
			_parent = p;
			Position = pos;
		}

		public void Reset()
		{
			HealthType = HealthLevel.NoCrack;
			CurrentHealth = _blockHealthMax[(int)Type];
			Owner.Redraw();
		}

		// BUG: If we build where we stand player falls into the block
		public bool BuildBlock(BlockType type)
		{
			if (type == BlockType.Water)
			{
				Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.Flow(
					this, 
					BlockType.Water,
					_blockHealthMax[(int) BlockType.Water],
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
				Owner.Redraw();
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
				Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.HealBlock(Position));

			if (CurrentHealth <= 0)
			{
				_type = BlockType.Air;
				IsSolid = false;
				HealthType = HealthLevel.NoCrack; // we change it to NoCrack because we don't want cracks to appear on air
				Owner.Redraw();
				return true;
			}

			Owner.Redraw();
			return false;
		}

		void CreateQuad(Cubeside side)
		{
			// BUG: This could be moved one level higher because for all types but grass the result is always the same
			int typeIndex;
			if (Type == BlockType.Grass)
			{
				if (side == Cubeside.Top)
				{
					typeIndex = (int)BlockType.Grass;
				}
				else if (side == Cubeside.Bottom)
				{
					typeIndex = (int)BlockType.Dirt;
				}
				else // any side
				{
					typeIndex = (int)BlockType.Grass + 1;
				}
			}
			else
			{
				typeIndex = (int)Type;
			}

			//all possible UVs
			Vector2 uv00 = _blockUVs[typeIndex, 0],
					uv10 = _blockUVs[typeIndex, 1],
					uv01 = _blockUVs[typeIndex, 2],
					uv11 = _blockUVs[typeIndex, 3];

			// second uvs - this holds cracks
			// secondary uvs need to be stored in a List - this is required by the Unity engine
			// set cracks - this need to be add with the correct order - why this order is different than above, I don't know
			var suvs = new List<Vector2>
			{
				_crackUVs[(int) HealthType, 3],	// top right corner
				_crackUVs[(int) HealthType, 2],	// top left corner
				_crackUVs[(int) HealthType, 0],	// bottom left corner
				_crackUVs[(int) HealthType, 1]	// bottom right corner
			};

			// Normals are vectors projected from the polygon (triangle) at the angle of 90 degrees,
			// they the engine which side it should treat as the side on which textures and shaders should be rendered.
			// Verticies also can have their own normal and this is the case here so each vertex has its own normal vector.
			var mesh = new Mesh();
			switch (side)
			{
				case Cubeside.Bottom:
					mesh.vertices = new[] { _p0, _p1, _p2, _p3 };
					mesh.normals = _downNormals;
					break;
				case Cubeside.Top:
					mesh.vertices = new[] { _p7, _p6, _p5, _p4 };
					mesh.normals = _upNormals;
					break;
				case Cubeside.Left:
					mesh.vertices = new[] { _p7, _p4, _p0, _p3 };
					mesh.normals = _leftNormals;
					break;
				case Cubeside.Right:
					mesh.vertices = new[] { _p5, _p6, _p2, _p1 };
					mesh.normals = _rightNormals;
					break;
				case Cubeside.Front:
					mesh.vertices = new[] { _p4, _p5, _p1, _p0 };
					mesh.normals = _forwardNormals;
					break;
				case Cubeside.Back:
					mesh.vertices = new[] { _p6, _p7, _p3, _p2 };
					mesh.normals = _backNormals;
					break;
			}
			
			// Uvs maps the texture over the surface
			mesh.uv = new[] { uv11, uv01, uv00, uv10 };

			// channel 1 relates to the UV1 we set in the editor
			mesh.SetUVs(1, suvs);
			mesh.triangles = new[] { 3, 1, 0, 3, 2, 1 };

			mesh.RecalculateBounds();

			var quad = new GameObject("Quad");
			quad.transform.position = Position;
			quad.transform.parent = _parent.transform;

			var meshFilter = (MeshFilter)quad.AddComponent(typeof(MeshFilter));
			meshFilter.mesh = mesh;
		}

		// convert x, y or z to what it is in the neighboring block
		int ConvertBlockIndexToLocal(int i)
		{
			if (i == -1)
				return World.ChunkSize - 1;
			if (i == World.ChunkSize)
				return 0;
			return i;
		}

		/// <summary>
		/// Returns the block from the chunk
		/// Returns null in case the chunk that the block supposed to be in does no exists
		/// </summary>
		public Block GetBlock(int x, int y, int z)
		{
			// block is in this chunk
			if (x >= 0 && x < World.ChunkSize && 
				y >= 0 && y < World.ChunkSize && 
				z >= 0 && z < World.ChunkSize)
				return Owner.Blocks[x, y, z];
			
			// the other chunk name based on its position
			var chunkName = World.BuildChunkName(
				(int)_parent.transform.position.x + (x - (int)Position.x) * World.ChunkSize, 
				(int)_parent.transform.position.y + (y - (int)Position.y) * World.ChunkSize, 
				(int)_parent.transform.position.z + (z - (int)Position.z) * World.ChunkSize);

			Chunk chunk;
			if (World.Chunks.TryGetValue(chunkName, out chunk))
				return chunk.Blocks[
					ConvertBlockIndexToLocal(x), 
					ConvertBlockIndexToLocal(y), 
					ConvertBlockIndexToLocal(z)]; // block is in the other chunk
			
			return null; // block is outside of the world
		}

		bool ShouldCreateQuad(int x, int y, int z)
		{
			try
			{
				var target = GetBlock(x, y, z);
				if (target != null)
				{
					if (Type == BlockType.Water && target.Type == BlockType.Water) return false;
					return !(target.IsSolid && IsSolid);
				}
			}
			catch (IndexOutOfRangeException) { } // BUG: I am not sure if this is the correct way - exception handling may be very slow

			return true;
		}

		public void Draw()
		{
			if (Type == BlockType.Air) return;

			int castedX = (int)Position.x,
				castedY = (int)Position.y,
				castedZ = (int)Position.z;
			
			// BUG: ShouldCreateQuad is called hundreds times per second and each time we call GetBlock method six times
			// the result of the check should be stored preferably in a table for the whole chunk
			// and changed each time block was destroyed or added
			if (ShouldCreateQuad(castedX, castedY, castedZ + 1))
				CreateQuad(Cubeside.Front);
			if (ShouldCreateQuad(castedX, castedY, castedZ - 1))
				CreateQuad(Cubeside.Back);
			if (ShouldCreateQuad(castedX, castedY + 1, castedZ))
				CreateQuad(Cubeside.Top);
			if (ShouldCreateQuad(castedX, castedY - 1, castedZ))
				CreateQuad(Cubeside.Bottom);
			if (ShouldCreateQuad(castedX - 1, castedY, castedZ))
				CreateQuad(Cubeside.Left);
			if (ShouldCreateQuad(castedX + 1, castedY, castedZ))
				CreateQuad(Cubeside.Right);
		}
	}
}