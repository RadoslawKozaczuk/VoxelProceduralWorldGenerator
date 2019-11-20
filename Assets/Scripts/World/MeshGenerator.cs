using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Scripts.World
{
	public class MeshGenerator : MonoBehaviour
	{
		const float WATER_UV_CONST = 1.0f / World.CHUNK_SIZE;

        // these values must match BlockType enumerator
        const int DIRT_TEXTURE_INDEX = 1;
        const int GRASS_TEXTURE_INDEX = 11;
        const int GRASS_SIDE_TEXTURE_INDEX = 12;
        const float UV_UNIT = 0.0625f; // tile sheet is 16 x 16

        #region Readonly lookup tables
        // could be nice to create it in a constructor based on some human readable units instead of float coordinates
        // for example 1 instead of 0.0625f
        readonly Vector2[,] _blockUVs = {

						// left-bottom, right-bottom, left-top, right-top
        /*AIR (dummy)*/ { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero },

		/*DIRT*/		{ new Vector2(2, 15), new Vector2(3, 15), new Vector2(2, 16), new Vector2(3, 16) },
		/*STONE*/		{ new Vector2(0, 14), new Vector2(1, 14), new Vector2(0, 15), new Vector2(1, 15) },
		/*DIAMOND*/		{ new Vector2(2, 12), new Vector2(3, 12), new Vector2(2, 13), new Vector2(3, 13) },
		/*BEDROCK*/		{ new Vector2(5, 13), new Vector2(6, 13), new Vector2(5, 14), new Vector2(6, 14) },
		/*REDSTONE*/	{ new Vector2(3, 12), new Vector2(4, 12), new Vector2(3, 13), new Vector2(4, 13) },
		/*SAND*/		{ new Vector2(2, 14), new Vector2(3, 14), new Vector2(2, 15), new Vector2(3, 15) },
		/*LEAVES*/		{ new Vector2(1, 6),  new Vector2(2, 6),  new Vector2(1, 7),  new Vector2(2, 7) },
		/*WOOD*/		{ new Vector2(6, 10), new Vector2(7, 10), new Vector2(6, 11), new Vector2(7, 11) },
		/*WOODBASE*/	{ new Vector2(6, 10), new Vector2(7, 10), new Vector2(6, 11), new Vector2(7, 11) },
		/*WATER*/		{ new Vector2(14, 2), new Vector2(15, 2), new Vector2(14, 3), new Vector2(15, 3) },
		/*GRASS*/		{ new Vector2(2, 6),  new Vector2(3, 6),  new Vector2(2, 7),  new Vector2(3, 7) },
        /*GRASS SIDE*/	{ new Vector2(3, 15), new Vector2(4, 15), new Vector2(3, 16), new Vector2(4, 16) },

		// BUG: Tile sheet provided is broken and some tiles overlap each other
		};

		readonly Vector2[,] _crackUVs;
		readonly Vector3 _p0 = new Vector3(-0.5f, -0.5f, 0.5f),
						 _p1 = new Vector3(0.5f, -0.5f, 0.5f),
						 _p2 = new Vector3(0.5f, -0.5f, -0.5f),
						 _p3 = new Vector3(-0.5f, -0.5f, -0.5f),
						 _p4 = new Vector3(-0.5f, 0.5f, 0.5f),
						 _p5 = new Vector3(0.5f, 0.5f, 0.5f),
						 _p6 = new Vector3(0.5f, 0.5f, -0.5f),
						 _p7 = new Vector3(-0.5f, 0.5f, -0.5f);
        #endregion

        readonly int _totalBlockNumberY;
        int _worldSizeX, _worldSizeZ, _totalBlockNumberX, _totalBlockNumberZ;

        public MeshGenerator()
        {
            _crackUVs = new Vector2[11, 4];

            // add noCrack
            _crackUVs[0, 0] = new Vector2(0.6875f, 0f);
            _crackUVs[0, 1] = new Vector2(0.75f, 0f);
            _crackUVs[0, 2] = new Vector2(0.6875f, UV_UNIT);
            _crackUVs[0, 3] = new Vector2(0.75f, UV_UNIT);

            // add cracks from crack1 to crack10
            for (int i = 1; i < 11; i++)
            {
                _crackUVs[i, 0] = new Vector2((i - 1) * UV_UNIT, 0f); // left-bottom
                _crackUVs[i, 1] = new Vector2(i * UV_UNIT, 0f); // right-bottom
                _crackUVs[i, 2] = new Vector2((i - 1) * UV_UNIT, UV_UNIT); // left-top
                _crackUVs[i, 3] = new Vector2(i * UV_UNIT, UV_UNIT); // right-top
            }

            _totalBlockNumberY = World.WORLD_SIZE_Y * World.CHUNK_SIZE;

            for (int i = 0; i < _blockUVs.GetLength(0); i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    _blockUVs[i, j].x *= UV_UNIT;
                    _blockUVs[i, j].y *= UV_UNIT;
                }
            }
        }

        public void Initialize(GameSettings options)
		{
			_worldSizeX = options.WorldSizeX;
			_worldSizeZ = options.WorldSizeZ;
			_totalBlockNumberX = _worldSizeX * World.CHUNK_SIZE;
			_totalBlockNumberZ = _worldSizeZ * World.CHUNK_SIZE;
		}

		/// <summary>
		/// This method creates mesh data necessary to create a mesh.
		/// Calculating two meshes at once is faster than creating them one by one.
        /// Although, do not use this method if you do not need one of the meshes.
		/// </summary>
		public void CalculateMeshes(ref BlockData[,,] blocks, Vector3Int chunkPos, out Mesh terrain, out Mesh water)
		{
			CalculateMeshesSize(ref blocks, chunkPos, out int tSize, out int wSize);

			var terrainData = new MeshData
			{
				Uvs = new Vector2[tSize],
				Suvs = new List<Vector2>(tSize),
				Verticies = new Vector3[tSize],
				Normals = new Vector3[tSize],
				Triangles = new int[(int)(1.5f * tSize)]
			};

			var waterData = new MeshData
			{
				Uvs = new Vector2[wSize],
				Suvs = new List<Vector2>(wSize),
				Verticies = new Vector3[wSize],
				Normals = new Vector3[wSize],
				Triangles = new int[(int)(1.5f * wSize)]
			};

			int index = 0, triIndex = 0, waterIndex = 0, waterTriIndex = 0;

			var localBlockCoodinates = new Vector3Int();
			for (int x = 0; x < World.CHUNK_SIZE; x++)
			{
				localBlockCoodinates.x = x;
				for (int y = 0; y < World.CHUNK_SIZE; y++)
				{
					localBlockCoodinates.y = y;
					for (int z = 0; z < World.CHUNK_SIZE; z++)
					{
						localBlockCoodinates.z = z;

						// offset must be included
						ref BlockData b = ref blocks[x + chunkPos.x, y + chunkPos.y, z + chunkPos.z];

						if (b.Faces == 0 || b.Type == BlockType.Air)
							continue;

						if (b.Type == BlockType.Water)
							CreateWaterQuad(ref b, ref waterIndex, ref waterTriIndex, ref waterData, ref localBlockCoodinates);
						else if (b.Type == BlockType.Grass)
							CreateGrassQuads(ref b, ref index, ref triIndex, ref terrainData, ref localBlockCoodinates);
						else
							CreateStandardQuads(ref b, ref index, ref triIndex, ref terrainData, ref localBlockCoodinates);
					}
				}
			}

			terrain = CreateMesh(terrainData);
			water = CreateMesh(waterData);
		}

		public void RecalculateFacesAfterBlockDestroy(ref BlockData[,,] blocks, int blockX, int blockY, int blockZ)
		{
			/* // bitwise operators reminder
			var test = Cubesides.Back; // initialize with one flag
			test |= Cubesides.Front; // add another flag
			var test0 = test &= ~Cubesides.Back; // AND= ~Back means subtract "Back"
			var test1 = test0 &= ~Cubesides.Back; // subsequent subtraction makes no change
			var test2 = test0 |= ~Cubesides.Back; // OR= ~Back means assign everything but not "Back" */

			blocks[blockX, blockY, blockZ].Faces = 0;

			if (blockX > 0)
				if (blocks[blockX - 1, blockY, blockZ].Type != BlockType.Air)
					blocks[blockX - 1, blockY, blockZ].Faces |= Cubeside.Right;

			if (blockX < _totalBlockNumberX - 1)
				if (blocks[blockX + 1, blockY, blockZ].Type != BlockType.Air)
					blocks[blockX + 1, blockY, blockZ].Faces |= Cubeside.Left;

			if (blockY > 0)
				if (blocks[blockX, blockY - 1, blockZ].Type != BlockType.Air)
					blocks[blockX, blockY - 1, blockZ].Faces |= Cubeside.Top;

			if (blockY < _totalBlockNumberY - 1)
				if (blocks[blockX, blockY + 1, blockZ].Type != BlockType.Air)
					blocks[blockX, blockY + 1, blockZ].Faces |= Cubeside.Bottom;

			if (blockZ > 0)
				if (blocks[blockX, blockY, blockZ - 1].Type != BlockType.Air)
					blocks[blockX, blockY, blockZ - 1].Faces |= Cubeside.Front;

			if (blockZ < _totalBlockNumberZ - 1)
				if (blocks[blockX, blockY, blockZ + 1].Type != BlockType.Air)
					blocks[blockX, blockY, blockZ + 1].Faces |= Cubeside.Back;
		}

		public void RecalculateFacesAfterBlockBuild(ref BlockData[,,] blocks, int blockX, int blockY, int blockZ)
		{
			ref BlockData b = ref blocks[blockX, blockY, blockZ];
			BlockType type;

			if (blockX > 0)
			{
				type = blocks[blockX - 1, blockY, blockZ].Type;
				if (type == BlockType.Air || type == BlockType.Water)
					b.Faces |= Cubeside.Left;
				else
					blocks[blockX - 1, blockY, blockZ].Faces &= ~Cubeside.Right;
			}
			else b.Faces |= Cubeside.Left;

			if (blockX < _totalBlockNumberX - 1)
			{
				type = blocks[blockX + 1, blockY, blockZ].Type;
				if (type == BlockType.Air || type == BlockType.Water)
					b.Faces |= Cubeside.Right;
				else
					blocks[blockX + 1, blockY, blockZ].Faces &= ~Cubeside.Left;
			}
			else b.Faces |= Cubeside.Right;

			if (blockY > 0)
			{
				type = blocks[blockX, blockY - 1, blockZ].Type;
				if (type == BlockType.Air || type == BlockType.Water)
					b.Faces |= Cubeside.Bottom;
				else
					blocks[blockX, blockY - 1, blockZ].Faces &= ~Cubeside.Top;
			}
			else b.Faces |= Cubeside.Bottom;

			if (blockY < _totalBlockNumberY - 1)
			{
				type = blocks[blockX, blockY + 1, blockZ].Type;
				if (type == BlockType.Air || type == BlockType.Water)
					b.Faces |= Cubeside.Top;
				else
					blocks[blockX, blockY + 1, blockZ].Faces &= ~Cubeside.Bottom;
			}
			else b.Faces |= Cubeside.Top;

			if (blockZ > 0)
			{
				type = blocks[blockX, blockY, blockZ - 1].Type;
				if (type == BlockType.Air || type == BlockType.Water)
					b.Faces |= Cubeside.Back;
				else
					blocks[blockX, blockY, blockZ - 1].Faces &= ~Cubeside.Front;
			}
			else b.Faces |= Cubeside.Back;

			if (blockZ < _totalBlockNumberZ - 1)
			{
				type = blocks[blockX, blockY, blockZ + 1].Type;
				if (type == BlockType.Air || type == BlockType.Water)
					b.Faces |= Cubeside.Front;
				else
					blocks[blockX, blockY, blockZ + 1].Faces &= ~Cubeside.Back;
			}
			else b.Faces |= Cubeside.Front;
		}

		/// <summary>
		/// Calculates inter block faces visibility.
		/// </summary>
		public void CalculateFaces(ref BlockData[,,] blocks)
		{
			for (int x = 0; x < _totalBlockNumberX; x++)
				for (int y = 0; y < _totalBlockNumberY; y++)
					for (int z = 0; z < _totalBlockNumberZ; z++)
					{
						BlockType type = blocks[x, y, z].Type;

						if (type == BlockType.Air)
						{
							// check block on the right
							if (x < _totalBlockNumberX - 1)
								if (blocks[x + 1, y, z].Type != BlockType.Air)
									blocks[x + 1, y, z].Faces |= Cubeside.Left;

							// check block above
							if (y < _totalBlockNumberY - 1)
								if (blocks[x, y + 1, z].Type != BlockType.Air)
									blocks[x, y + 1, z].Faces |= Cubeside.Bottom;

							// check block in front
							if (z < _totalBlockNumberZ - 1)
								if (blocks[x, y, z + 1].Type != BlockType.Air)
									blocks[x, y, z + 1].Faces |= Cubeside.Back;
						}
						else if (type == BlockType.Water)
						{
							if (y < _totalBlockNumberY - 1)
								if (blocks[x, y + 1, z].Type == BlockType.Air)
									blocks[x, y, z].Faces |= Cubeside.Top;

							// check block on the right
							if (x < _totalBlockNumberX - 1)
								if (blocks[x + 1, y, z].Type != BlockType.Air)
									blocks[x + 1, y, z].Faces |= Cubeside.Left;

							// check block above
							if (y < _totalBlockNumberY - 1)
								if (blocks[x, y + 1, z].Type != BlockType.Air)
									blocks[x, y + 1, z].Faces |= Cubeside.Bottom;

							// check block in front
							if (z < _totalBlockNumberZ - 1)
								if (blocks[x, y, z + 1].Type != BlockType.Air)
									blocks[x, y, z + 1].Faces |= Cubeside.Back;
						}
						else
						{
							if (x < _totalBlockNumberX - 1)
								if (blocks[x + 1, y, z].Type == BlockType.Air || blocks[x + 1, y, z].Type == BlockType.Water)
									blocks[x, y, z].Faces |= Cubeside.Right;

							if (y < _totalBlockNumberY - 1)
								if (blocks[x, y + 1, z].Type == BlockType.Air || blocks[x, y + 1, z].Type == BlockType.Water)
									blocks[x, y, z].Faces |= Cubeside.Top;

							if (z < _totalBlockNumberZ - 1)
								if (blocks[x, y, z + 1].Type == BlockType.Air || blocks[x, y, z + 1].Type == BlockType.Water)
									blocks[x, y, z].Faces |= Cubeside.Front;
						}
					}
		}

		/// <summary>
		/// Calculates faces visibility on the world's edges.
		/// </summary>
		public void WorldBoundariesCheck(ref BlockData[,,] blocks)
		{
			ref BlockData b = ref blocks[0, 0, 0]; // compiler requires initialization

			// right world boundaries check
			int x = _totalBlockNumberX - 1, 
                y, 
                z;
			for (y = 0; y < _totalBlockNumberY; y++)
				for (z = 0; z < _totalBlockNumberZ; z++)
				{
					b = ref blocks[x, y, z];
					if (b.Type != BlockType.Water && b.Type != BlockType.Air)
						b.Faces |= Cubeside.Right;
				}

			// left world boundaries check
			x = 0;
			for (y = 0; y < _totalBlockNumberY; y++)
				for (z = 0; z < _totalBlockNumberZ; z++)
				{
					b = ref blocks[x, y, z];
					if (b.Type != BlockType.Water && b.Type != BlockType.Air)
						b.Faces |= Cubeside.Left;
				}

			// top world boundaries check
			y = _totalBlockNumberY - 1;
			for (x = 0; x < _totalBlockNumberX; x++)
				for (z = 0; z < _totalBlockNumberZ; z++)
					blocks[x, y, z].Faces |= Cubeside.Top; // there will always be air

			// bottom world boundaries check
			y = 0;
			for (x = 0; x < _totalBlockNumberX; x++)
				for (z = 0; z < _totalBlockNumberZ; z++)
					blocks[x, y, z].Faces |= Cubeside.Bottom; // there will always be bedrock

			// front world boundaries check
			z = _totalBlockNumberZ - 1;
			for (x = 0; x < _totalBlockNumberX; x++)
				for (y = 0; y < _totalBlockNumberY; y++)
				{
					b = ref blocks[x, y, z];
					if (b.Type != BlockType.Water && b.Type != BlockType.Air)
						b.Faces |= Cubeside.Front;
				}

			// back world boundaries check
			z = 0;
			for (x = 0; x < _totalBlockNumberX; x++)
				for (y = 0; y < _totalBlockNumberY; y++)
				{
					b = ref blocks[x, y, z];
					if (b.Type != BlockType.Water && b.Type != BlockType.Air)
						b.Faces |= Cubeside.Back;
				}
		}

        Mesh CreateMesh(MeshData meshData)
        {
            var mesh = new Mesh
            {
                vertices = meshData.Verticies,
                normals = meshData.Normals,
                uv = meshData.Uvs, // Uvs maps the texture over the surface
                triangles = meshData.Triangles
            };
            mesh.SetUVs(1, meshData.Suvs); // secondary uvs
            mesh.RecalculateBounds();

            return mesh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CalculateMeshesSize(ref BlockData[,,] blocks, Vector3Int chunkPos, out int tSize, out int wSize)
		{
			tSize = 0;
			wSize = 0;

			ref BlockData b = ref blocks[0, 0, 0]; // assign anything

			// caching - this will make this function at least 2x faster
			int cachedX = chunkPos.x;
			int cachedY = chunkPos.y;
			int cachedZ = chunkPos.z;

			// offset needs to be calculated
			int x, y, z;
			for (x = cachedX; x < cachedX + World.CHUNK_SIZE; x++)
				for (y = cachedY; y < cachedY + World.CHUNK_SIZE; y++)
					for (z = cachedZ; z < cachedZ + World.CHUNK_SIZE; z++)
					{
						b = ref blocks[x, y, z];

						if (b.Type == BlockType.Water)
						{
							if ((b.Faces & Cubeside.Top) == Cubeside.Top)
								wSize += 4;
						}
						else if (b.Type != BlockType.Air)
							tSize += CountFaces(b.Faces) * 4;
					}
		}

        // Around two times faster (which is still insignificant at all in this context - 10ms on a 7*4*7 map...).
        // Although, I still decided to leave it here for learning purpose.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CountFaces(Cubeside faces)
        {
            int bitCount = 0;
            int n = (int)faces;
            while (n != 0)
            {
                bitCount++;
                n &= n - 1;
            }
            return bitCount;
        }

        // Apollo - bitwise enum check is 20x faster
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool HasFlag(Cubeside source, Cubeside target) => (source & target) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CreateStandardQuads(ref BlockData block, ref int index, ref int triIndex, ref MeshData data, ref Vector3Int localBlockCoord)
		{
			int typeIndex = (int)block.Type;

            // all possible UVs
            Vector2 uv00 = _blockUVs[typeIndex, 0],
                    uv10 = _blockUVs[typeIndex, 1],
                    uv01 = _blockUVs[typeIndex, 2],
                    uv11 = _blockUVs[typeIndex, 3];

			if (HasFlag(block.Faces, Cubeside.Top))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.up,
					ref uv11, ref uv01, ref uv00, ref uv10,
					_p7 + localBlockCoord, _p6 + localBlockCoord, _p5 + localBlockCoord, _p4 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Bottom))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.down,
					ref uv11, ref uv01, ref uv00, ref uv10,
					_p0 + localBlockCoord, _p1 + localBlockCoord, _p2 + localBlockCoord, _p3 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Left))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.left,
					ref uv11, ref uv01, ref uv00, ref uv10,
					_p7 + localBlockCoord, _p4 + localBlockCoord, _p0 + localBlockCoord, _p3 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Right))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.right,
					ref uv11, ref uv01, ref uv00, ref uv10,
					_p5 + localBlockCoord, _p6 + localBlockCoord, _p2 + localBlockCoord, _p1 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Front))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.forward,
					ref uv11, ref uv01, ref uv00, ref uv10,
					_p4 + localBlockCoord, _p5 + localBlockCoord, _p1 + localBlockCoord, _p0 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Back))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.back,
					ref uv11, ref uv01, ref uv00, ref uv10,
					_p6 + localBlockCoord, _p7 + localBlockCoord, _p3 + localBlockCoord, _p2 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CreateGrassQuads(ref BlockData block, ref int index, ref int triIndex, ref MeshData data, ref Vector3Int localBlockCoord)
		{
			Vector2 uv00side = _blockUVs[GRASS_SIDE_TEXTURE_INDEX, 0],
					uv10side = _blockUVs[GRASS_SIDE_TEXTURE_INDEX, 1],
					uv01side = _blockUVs[GRASS_SIDE_TEXTURE_INDEX, 2],
					uv11side = _blockUVs[GRASS_SIDE_TEXTURE_INDEX, 3];

			if (HasFlag(block.Faces, Cubeside.Top))
			{
				Vector2 uv00top = _blockUVs[GRASS_TEXTURE_INDEX, 0],
						uv10top = _blockUVs[GRASS_TEXTURE_INDEX, 1],
						uv01top = _blockUVs[GRASS_TEXTURE_INDEX, 2],
						uv11top = _blockUVs[GRASS_TEXTURE_INDEX, 3];

				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.up,
					ref uv11top, ref uv01top, ref uv00top, ref uv10top,
					_p7 + localBlockCoord, _p6 + localBlockCoord, _p5 + localBlockCoord, _p4 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Bottom))
			{
				Vector2 uv00bot = _blockUVs[DIRT_TEXTURE_INDEX, 0],
						uv10bot = _blockUVs[DIRT_TEXTURE_INDEX, 1],
						uv01bot = _blockUVs[DIRT_TEXTURE_INDEX, 2],
						uv11bot = _blockUVs[DIRT_TEXTURE_INDEX, 3];

				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.down,
					ref uv11bot, ref uv01bot, ref uv00bot, ref uv10bot,
					_p0 + localBlockCoord, _p1 + localBlockCoord, _p2 + localBlockCoord, _p3 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Left))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.left,
					ref uv00side, ref uv10side, ref uv11side, ref uv01side,
					_p7 + localBlockCoord, _p4 + localBlockCoord, _p0 + localBlockCoord, _p3 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Right))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.right,
					ref uv00side, ref uv10side, ref uv11side, ref uv01side,
					_p5 + localBlockCoord, _p6 + localBlockCoord, _p2 + localBlockCoord, _p1 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Front))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.forward,
					ref uv00side, ref uv10side, ref uv11side, ref uv01side,
					_p4 + localBlockCoord, _p5 + localBlockCoord, _p1 + localBlockCoord, _p0 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}

			if (HasFlag(block.Faces, Cubeside.Back))
			{
				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.back,
					ref uv00side, ref uv10side, ref uv11side, ref uv01side,
					_p6 + localBlockCoord, _p7 + localBlockCoord, _p3 + localBlockCoord, _p2 + localBlockCoord);
				AddSuvs(ref block, ref data);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void CreateWaterQuad(ref BlockData block, ref int index, ref int triIndex, ref MeshData data, ref Vector3Int localBlockCoord)
		{
			if (HasFlag(block.Faces, Cubeside.Top))
			{
				// all possible UVs
				// left-top, right-top, left-bottom, right-bottom
				Vector2 uv00 = new Vector2(WATER_UV_CONST * localBlockCoord.x, 1 - WATER_UV_CONST * localBlockCoord.z),
					uv10 = new Vector2(WATER_UV_CONST * (localBlockCoord.x + 1), 1 - WATER_UV_CONST * localBlockCoord.z),
					uv01 = new Vector2(WATER_UV_CONST * localBlockCoord.x, 1 - WATER_UV_CONST * (localBlockCoord.z + 1)),
					uv11 = new Vector2(WATER_UV_CONST * (localBlockCoord.x + 1), 1 - WATER_UV_CONST * (localBlockCoord.z + 1));

				AddQuadComponents(ref index, ref triIndex, ref data, Vector3.up,
					ref uv11, ref uv01, ref uv00, ref uv10,
					_p7 + localBlockCoord, _p6 + localBlockCoord, _p5 + localBlockCoord, _p4 + localBlockCoord);
			}
		}

		void AddQuadComponents(ref int index, ref int triIndex,	ref MeshData data, Vector3 normal,
			ref Vector2 uv11, ref Vector2 uv01, ref Vector2 uv00, ref Vector2 uv10,
			Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
            // add normals
            data.Normals[index] = normal;
			data.Normals[index + 1] = normal;
			data.Normals[index + 2] = normal;
			data.Normals[index + 3] = normal;

			// add uvs
			data.Uvs[index] = uv00;
			data.Uvs[index + 1] = uv10;
			data.Uvs[index + 2] = uv11;
			data.Uvs[index + 3] = uv01;

			// add verticies
			data.Verticies[index] = p0;
			data.Verticies[index + 1] = p1;
			data.Verticies[index + 2] = p2;
			data.Verticies[index + 3] = p3;

			// add triangles
			data.Triangles[triIndex++] = index + 3;
			data.Triangles[triIndex++] = index + 1;
			data.Triangles[triIndex++] = index;
			data.Triangles[triIndex++] = index + 3;
			data.Triangles[triIndex++] = index + 2;
			data.Triangles[triIndex++] = index + 1;

			index += 4;
        }

        void AddSuvs(ref BlockData block, ref MeshData data)
		{
			data.Suvs.Add(_crackUVs[block.HealthLevel, 3]); // top right corner
			data.Suvs.Add(_crackUVs[block.HealthLevel, 2]); // top left corner
			data.Suvs.Add(_crackUVs[block.HealthLevel, 0]); // bottom left corner
			data.Suvs.Add(_crackUVs[block.HealthLevel, 1]); // bottom right corner
		}
	}
}
