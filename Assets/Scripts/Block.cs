using UnityEngine;

namespace Assets.Scripts
{
	public class Block
	{
		public enum BlockType { Grass, Dirt, Stone, Diamond, Bedrock, Redstone, Air };
		enum Cubeside { Bottom, Top, Left, Right, Front, Back };

		public bool IsSolid;

		readonly BlockType _blockType;
		readonly Chunk _owner;
		readonly GameObject _parent;
		Vector3 _position;

		readonly Vector2[,] _blockUVs = { 
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
									new Vector2( 0.3125f, 0.875f ),new Vector2( 0.375f, 0.875f )},
			/*REDSTONE*/		{new Vector2( 0.1875f, 0.75f ), new Vector2( 0.25f, 0.75f),
									new Vector2( 0.1875f, 0.8125f ),new Vector2( 0.25f, 0.8125f )},
		};

		public Block(BlockType block, Vector3 pos, GameObject p, Chunk o)
		{
			_blockType = block;
			_owner = o;
			_parent = p;
			_position = pos;
			IsSolid = _blockType != BlockType.Air;
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

			//all possible UVs
			Vector2 uv00;
			Vector2 uv10;
			Vector2 uv01;
			Vector2 uv11;

			if (_blockType == BlockType.Grass && side == Cubeside.Top)
			{
				uv00 = _blockUVs[0, 0];
				uv10 = _blockUVs[0, 1];
				uv01 = _blockUVs[0, 2];
				uv11 = _blockUVs[0, 3];
			}
			else if (_blockType == BlockType.Grass && side == Cubeside.Bottom)
			{
				uv00 = _blockUVs[(int)(BlockType.Dirt + 1), 0];
				uv10 = _blockUVs[(int)(BlockType.Dirt + 1), 1];
				uv01 = _blockUVs[(int)(BlockType.Dirt + 1), 2];
				uv11 = _blockUVs[(int)(BlockType.Dirt + 1), 3];
			}
			else
			{
				uv00 = _blockUVs[(int)(_blockType + 1), 0];
				uv10 = _blockUVs[(int)(_blockType + 1), 1];
				uv01 = _blockUVs[(int)(_blockType + 1), 2];
				uv11 = _blockUVs[(int)(_blockType + 1), 3];
			}

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
					normals = new[] {Vector3.down, Vector3.down, Vector3.down, Vector3.down};
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Top:
					vertices = new[] { p7, p6, p5, p4 };
					normals = new[] {Vector3.up, Vector3.up, Vector3.up, Vector3.up};
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Left:
					vertices = new[] { p7, p4, p0, p3 };
					normals = new[] {Vector3.left, Vector3.left, Vector3.left, Vector3.left};
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Right:
					vertices = new[] { p5, p6, p2, p1 };
					normals = new[] {Vector3.right, Vector3.right, Vector3.right, Vector3.right};
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Front:
					vertices = new[] { p4, p5, p1, p0 };
					normals = new[] {Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward};
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
				case Cubeside.Back:
					vertices = new[] { p6, p7, p3, p2 };
					normals = new[] {Vector3.back, Vector3.back, Vector3.back, Vector3.back};
					uvs = new[] { uv11, uv01, uv00, uv10 };
					triangles = new[] { 3, 1, 0, 3, 2, 1 };
					break;
			}

			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uvs;
			mesh.triangles = triangles;

			mesh.RecalculateBounds();

			var quad = new GameObject("Quad");
			quad.transform.position = _position;
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

		bool HasSolidNeighbor(int x, int y, int z)
		{
			Block[,,] chunks;

			if (x < 0 || x >= World.ChunkSize ||
			    y < 0 || y >= World.ChunkSize ||
			    z < 0 || z >= World.ChunkSize)
			{
				// block in a neighboring ChunkGameObject

				var neighborChunkPos = _parent.transform.position +
				                       new Vector3((x - (int) _position.x) * World.ChunkSize,
					                       (y - (int) _position.y) * World.ChunkSize,
					                       (z - (int) _position.z) * World.ChunkSize);
				var nName = World.BuildChunkName(neighborChunkPos);

				x = ConvertBlockIndexToLocal(x);
				y = ConvertBlockIndexToLocal(y);
				z = ConvertBlockIndexToLocal(z);

				Chunk nChunk;
				if (World.Chunks.TryGetValue(nName, out nChunk))
				{
					chunks = nChunk.Blocks;
				}
				else
				{
					return false; // block is outside of the world
				}
			} // block in this ChunkGameObject
			else
			{
				chunks = _owner.Blocks;
			}

			try
			{
				return chunks[x, y, z].IsSolid;
			}
			catch (System.IndexOutOfRangeException) { }

			return false;
		}

		public void Draw()
		{
			if (_blockType == BlockType.Air) return;

			if (!HasSolidNeighbor((int)_position.x, (int)_position.y, (int)_position.z + 1))
				CreateQuad(Cubeside.Front);
			if (!HasSolidNeighbor((int)_position.x, (int)_position.y, (int)_position.z - 1))
				CreateQuad(Cubeside.Back);
			if (!HasSolidNeighbor((int)_position.x, (int)_position.y + 1, (int)_position.z))
				CreateQuad(Cubeside.Top);
			if (!HasSolidNeighbor((int)_position.x, (int)_position.y - 1, (int)_position.z))
				CreateQuad(Cubeside.Bottom);
			if (!HasSolidNeighbor((int)_position.x - 1, (int)_position.y, (int)_position.z))
				CreateQuad(Cubeside.Left);
			if (!HasSolidNeighbor((int)_position.x + 1, (int)_position.y, (int)_position.z))
				CreateQuad(Cubeside.Right);
		}
	}
}