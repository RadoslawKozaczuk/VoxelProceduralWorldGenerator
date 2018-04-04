using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	public class Block
	{
		public enum BlockType
		{
			Grass, Dirt, Stone, Diamond, Bedrock, Redstone,
			//Water,
			Air
		};

		public enum HealthLevel
		{
			NoCrack, Crack1, Crack2, Crack3, Crack4
		}

		enum Cubeside { Bottom, Top, Left, Right, Front, Back };

		public bool IsSolid;

		private BlockType _type;
		public BlockType Type
		{
			get { return _type; }
			set
			{
				_type = value;
				IsSolid = _type != BlockType.Air;
			}
		}

		// crack texture
		public HealthLevel HealthType;

		// current health as a number of hit points
		private int _currentHealth; // start set to maximum health the block can be

		// this corresponds to the BlockType enum, so for example Grass can be hit 3 times
		private readonly int[] _blockHealthMax = {
			3, 3, 4, 4, -1, 4,
			//8, // water
			0 // air
		}; // -1 means the block cannot be destroyed

		public readonly Chunk Owner;
		readonly GameObject _parent;
		public Vector3 Position;

		// assumptions used:
		// coordination start left down corner
		readonly Vector2[,] _blockUVs = { 
								// left-bottom, right-bottom, left-top, right-top
			/*GRASS TOP*/		{new Vector2( 0.125f, 0.375f ), new Vector2( 0.1875f, 0.375f),
									new Vector2( 0.125f, 0.4375f ), new Vector2( 0.1875f, 0.4375f )},
			/*GRASS SIDE*/		{new Vector2( 0.1875f, 0.9375f ), new Vector2( 0.25f, 0.9375f),
									new Vector2( 0.1875f, 1.0f ), new Vector2( 0.25f, 1.0f )},
			/*DIRT*/			{new Vector2( 0.125f, 0.9375f ), new Vector2( 0.1875f, 0.9375f),
									new Vector2( 0.125f, 1.0f ), new Vector2( 0.1875f, 1.0f )},
			/*STONE*/			{new Vector2( 0, 0.875f ), new Vector2( 0.0625f, 0.875f),
									new Vector2( 0, 0.9375f ), new Vector2( 0.0625f, 0.9375f )},
			/*DIAMOND*/			{new Vector2( 0.125f, 0.75f ), new Vector2( 0.1875f, 0.75f),
									new Vector2( 0.125f, 0.8125f ), new Vector2( 0.1875f, 0.81f )},
			/*BEDROCK*/			{new Vector2( 0.3125f, 0.8125f ), new Vector2( 0.375f, 0.8125f),
									new Vector2( 0.3125f, 0.875f ), new Vector2( 0.375f, 0.875f )},
			/*REDSTONE*/		{new Vector2( 0.1875f, 0.75f ), new Vector2( 0.25f, 0.75f),
									new Vector2( 0.1875f, 0.8125f ), new Vector2( 0.25f, 0.8125f )}//,

			/*WATER*/			//{new Vector2(0.875f,0.125f), new Vector2(0.9375f,0.125f),
									//new Vector2(0.875f,0.1875f), new Vector2(0.9375f,0.1875f)},
			
			// BUG - tile sheet provided is broken and some tiles overlaps each other
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
		
		public Block(BlockType type, Vector3 pos, GameObject p, Chunk o)
		{
			Type = type;
			HealthType = HealthLevel.NoCrack;
			_currentHealth = _blockHealthMax[(int)_type]; // maximum health
			Owner = o;
			_parent = p;
			Position = pos;
		}

		public void Reset()
		{
			HealthType = HealthLevel.NoCrack;
			_currentHealth = _blockHealthMax[(int)Type];
			Owner.Redraw();
		}

		// BUG - if we build where we stand player falls into the block
		public bool BuildBlock(BlockType type)
		{
			Type = type;
			_currentHealth = _blockHealthMax[(int)_type]; // maximum health
			Owner.Redraw();
			return true;
		}

		/// <summary>
		/// returns true if the block has been destroyed and false if it has not
		/// </summary>
		public bool HitBlock()
		{
			if (_currentHealth == -1) return false;
			_currentHealth--;
			HealthType++;

			// if the block was hit for the first time start the coroutine
			if (_currentHealth == _blockHealthMax[(int)Type] - 1)
				Owner.MonoBehavior.StartCoroutine(Owner.MonoBehavior.HealBlock(Position));

			if (_currentHealth <= 0)
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
			var mesh = new Mesh();
			mesh.name = "ScriptedMesh" + side;

			var vertices = new Vector3[4];

			// Normals are vectors projected from the polygon (triangle) at the angle of 90 degrees,
			// they the engine which side it should treat as the side on which textures and shaders should be rendered.
			// Verticies also can have their own normal and this is the case here so each vertex has its own normal vector.
			var normals = new Vector3[4];
			              
			// Uvs maps the texture over the surface
			var uvs = new Vector2[4];
			var triangles = new int[6];

			// second uvs - this holds cracks
			var suvs = new List<Vector2>(); // secondary uvs need to be stored in a List - this is required by the Unity engine

			//all possible UVs
			Vector2 uv00;
			Vector2 uv10;
			Vector2 uv01;
			Vector2 uv11;

			if (Type == BlockType.Grass && side == Cubeside.Top)
			{
				uv00 = _blockUVs[0, 0];
				uv10 = _blockUVs[0, 1];
				uv01 = _blockUVs[0, 2];
				uv11 = _blockUVs[0, 3];
			}
			else if (Type == BlockType.Grass && side == Cubeside.Bottom)
			{
				// first param gets plus one because grass has two types and because of that it does not match enum anymore
				uv00 = _blockUVs[(int)(BlockType.Dirt + 1), 0];
				uv10 = _blockUVs[(int)(BlockType.Dirt + 1), 1];
				uv01 = _blockUVs[(int)(BlockType.Dirt + 1), 2];
				uv11 = _blockUVs[(int)(BlockType.Dirt + 1), 3];
			}
			else
			{
				// first param gets plus one because grass has two types and because of that it does not match enum anymore
				uv00 = _blockUVs[(int)(Type + 1), 0];
				uv10 = _blockUVs[(int)(Type + 1), 1];
				uv01 = _blockUVs[(int)(Type + 1), 2];
				uv11 = _blockUVs[(int)(Type + 1), 3];
			}

			// set cracks - this need to be add with the correct order - why this order is different than above, I don't know
			suvs.Add(_crackUVs[(int)HealthType, 3]); // top right corner
			suvs.Add(_crackUVs[(int)HealthType, 2]); // top left corner
			suvs.Add(_crackUVs[(int)HealthType, 0]); // bottom left corner
			suvs.Add(_crackUVs[(int)HealthType, 1]); // bottom right corner

			//all possible vertices
			var p0 = new Vector3(-0.5f, -0.5f, 0.5f);
			var p1 = new Vector3(0.5f, -0.5f, 0.5f);
			var p2 = new Vector3(0.5f, -0.5f, -0.5f);
			var p3 = new Vector3(-0.5f, -0.5f, -0.5f);
			var p4 = new Vector3(-0.5f, 0.5f, 0.5f);
			var p5 = new Vector3(0.5f, 0.5f, 0.5f);
			var p6 = new Vector3(0.5f, 0.5f, -0.5f);
			var p7 = new Vector3(-0.5f, 0.5f, -0.5f);

			switch (side)
			{
				case Cubeside.Bottom:
					vertices = new[] { p0, p1, p2, p3 };
					normals = new[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down };
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Top:
					vertices = new[] { p7, p6, p5, p4 };
					normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Left:
					vertices = new[] { p7, p4, p0, p3 };
					normals = new[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left };
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Right:
					vertices = new[] { p5, p6, p2, p1 };
					normals = new[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right };
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Front:
					vertices = new[] { p4, p5, p1, p0 };
					normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Back:
					vertices = new[] { p6, p7, p3, p2 };
					normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
			}

			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uvs;

			// channel 1 relates to the UV1 we set in the editor
			mesh.SetUVs(1, suvs);
			mesh.triangles = triangles;

			mesh.RecalculateBounds();

			var quad = new GameObject("Quad");
			quad.transform.position = Position;
			quad.transform.parent = _parent.transform;

			var meshFilter = (MeshFilter)quad.AddComponent(typeof(MeshFilter));
			meshFilter.mesh = mesh;
		}

		int ConvertBlockIndexToLocal(int i)
		{
			if (i == -1)
				i = World.ChunkSize - 1;
			else if (i == World.ChunkSize)
				i = 0;
			return i;
		}

		/// <summary>
		/// Returns the neighboring block
		/// </summary>
		Block GetBlock(int x, int y, int z)
		{
			Block[,,] blocks;

			if (x < 0 || x >= World.ChunkSize ||
				y < 0 || y >= World.ChunkSize ||
				z < 0 || z >= World.ChunkSize)
			{
				// block in a neighboring chunk
				var neighborChunkPos = _parent.transform.position +
									   new Vector3((x - (int)Position.x) * World.ChunkSize,
										   (y - (int)Position.y) * World.ChunkSize,
										   (z - (int)Position.z) * World.ChunkSize);
				var chunkName = World.BuildChunkName(neighborChunkPos);

				x = ConvertBlockIndexToLocal(x);
				y = ConvertBlockIndexToLocal(y);
				z = ConvertBlockIndexToLocal(z);

				Chunk chunk;
				if (World.Chunks.TryGetValue(chunkName, out chunk))
					blocks = chunk.Blocks;
				else return null; // block is outside of the world
			} // block is in this chunk
			else
				blocks = Owner.Blocks;

			return blocks[x, y, z];
		}

		bool HasSolidNeighbor(int x, int y, int z)
		{
			try
			{
				Block b = GetBlock(x, y, z);
				if (b != null)
					return b.IsSolid 
						/*|| b.Type != Type*/; // here we test if this block type and neighboring block's type are different
				// and if they are we draw the quad
				// this is not efficient because we need that check because of the water
			}
			catch (System.IndexOutOfRangeException) { }

			return false;
		}

		public void Draw()
		{
			if (Type == BlockType.Air) return;

			if (!HasSolidNeighbor((int)Position.x, (int)Position.y, (int)Position.z + 1))
				CreateQuad(Cubeside.Front);
			if (!HasSolidNeighbor((int)Position.x, (int)Position.y, (int)Position.z - 1))
				CreateQuad(Cubeside.Back);
			if (!HasSolidNeighbor((int)Position.x, (int)Position.y + 1, (int)Position.z))
				CreateQuad(Cubeside.Top);
			if (!HasSolidNeighbor((int)Position.x, (int)Position.y - 1, (int)Position.z))
				CreateQuad(Cubeside.Bottom);
			if (!HasSolidNeighbor((int)Position.x - 1, (int)Position.y, (int)Position.z))
				CreateQuad(Cubeside.Left);
			if (!HasSolidNeighbor((int)Position.x + 1, (int)Position.y, (int)Position.z))
				CreateQuad(Cubeside.Right);
		}
	}
}