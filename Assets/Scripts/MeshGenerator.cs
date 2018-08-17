using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public struct MeshData
    {
        public Vector2[] uvs;
        public List<Vector2> suvs;
        public Vector3[] verticies;
        public Vector3[] normals;
        public int[] triangles;
    }

    // assumptions used:
    // coordination start left down corner
    static readonly Vector2[,] _blockUVs = { 
						// left-bottom, right-bottom, left-top, right-top
		/*DIRT*/		{new Vector2(0.125f, 0.9375f), new Vector2(0.1875f, 0.9375f),
                            new Vector2(0.125f, 1.0f), new Vector2(0.1875f, 1.0f)},
		/*STONE*/		{new Vector2(0, 0.875f), new Vector2(0.0625f, 0.875f),
                            new Vector2(0, 0.9375f), new Vector2(0.0625f, 0.9375f)},
		/*DIAMOND*/		{new Vector2 (0.125f, 0.75f), new Vector2(0.1875f, 0.75f),
                            new Vector2(0.125f, 0.8125f), new Vector2(0.1875f, 0.81f)},
		/*BEDROCK*/		{new Vector2(0.3125f, 0.8125f), new Vector2(0.375f, 0.8125f),
                            new Vector2(0.3125f, 0.875f), new Vector2(0.375f, 0.875f)},
		/*REDSTONE*/	{new Vector2(0.1875f, 0.75f), new Vector2(0.25f, 0.75f),
                            new Vector2(0.1875f, 0.8125f), new Vector2(0.25f, 0.8125f)},
		/*SAND*/		{new Vector2(0.125f, 0.875f), new Vector2(0.1875f, 0.875f),
                            new Vector2(0.125f, 0.9375f), new Vector2(0.1875f, 0.9375f)},
		/*LEAVES*/		{new Vector2(0.0625f,0.375f), new Vector2(0.125f,0.375f),
                            new Vector2(0.0625f,0.4375f), new Vector2(0.125f,0.4375f)},
		/*WOOD*/		{new Vector2(0.375f,0.625f), new Vector2(0.4375f,0.625f),
                            new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f)},
		/*WOODBASE*/	{new Vector2(0.375f,0.625f), new Vector2(0.4375f,0.625f),
                            new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f)},	    
			
		/*WATER*/		{new Vector2(0.875f,0.125f), new Vector2(0.9375f,0.125f),
                            new Vector2(0.875f,0.1875f), new Vector2(0.9375f,0.1875f)},

		/*GRASS*/		{new Vector2(0.125f, 0.375f), new Vector2(0.1875f, 0.375f),
                            new Vector2(0.125f, 0.4375f), new Vector2(0.1875f, 0.4375f)},
        /*GRASS SIDE*/	{new Vector2(0.1875f, 0.9375f), new Vector2(0.25f, 0.9375f),
                            new Vector2(0.1875f, 1.0f), new Vector2(0.25f, 1.0f)},

		// BUG: Tile sheet provided is broken and some tiles overlaps each other
	};

    static readonly Vector2[,] _crackUVs = { 
						// left-bottom, right-bottom, left-top, right-top
		/*NOCRACK*/		{new Vector2(0.6875f,0f), new Vector2(0.75f,0f),
                            new Vector2(0.6875f,0.0625f), new Vector2(0.75f,0.0625f)},
		/*CRACK1*/		{new Vector2(0.0625f,0f), new Vector2(0.125f,0f),
                            new Vector2(0.0625f,0.0625f), new Vector2(0.125f,0.0625f)},
		/*CRACK2*/		{new Vector2(0.1875f,0f), new Vector2(0.25f,0f),
                            new Vector2(0.1875f,0.0625f), new Vector2(0.25f,0.0625f)},
		/*CRACK3*/		{new Vector2(0.3125f,0f), new Vector2(0.375f,0f),
                            new Vector2(0.3125f,0.0625f), new Vector2(0.375f,0.0625f)},
		/*CRACK4*/		{new Vector2(0.4375f,0f), new Vector2(0.5f,0f),
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
    
    /// <summary>
    /// Returns the block from the chunk
    /// Returns null in case the chunk that the block supposed to be in does no exists
    /// </summary>
    //public Block GetBlock(int localX, int localY, int localZ)
    //{
    //    // block is in this chunk
    //    if (localX >= 0 && localX < World.ChunkSize &&
    //        localY >= 0 && localY < World.ChunkSize &&
    //        localZ >= 0 && localZ < World.ChunkSize)
    //        return Owner.Blocks[localX, localY, localZ];

    //    // this chunk xyz
    //    int thisChunkX, thisChunkY, thisChunkZ;

    //    // deterining the chuml
    //    if (localX < 0)
    //        thisChunkX = Owner.X - 1; // pod warunkiem że Owner.X istnieje
    //    else if (localX >= World.ChunkSize)
    //        thisChunkX = Owner.X + 1;
    //    else
    //        thisChunkX = Owner.X;

    //    if (localY < 0)
    //        thisChunkY = Owner.Y - 1; // pod warunkiem że Owner.Z istnieje
    //    else if (localY >= World.ChunkSize)
    //        thisChunkY = Owner.Y + 1;
    //    else
    //        thisChunkY = Owner.Y;

    //    if (localZ < 0)
    //        thisChunkZ = Owner.Z - 1; // pod warunkiem że Owner.Z istnieje
    //    else if (localZ >= World.ChunkSize)
    //        thisChunkZ = Owner.Z + 1;
    //    else
    //        thisChunkZ = Owner.Z;

    //    if (localX < 0 || localX >= World.WorldSizeX
    //        || localY < 0 || localY >= World.WorldSizeY
    //        || localZ < 0 || localZ >= World.WorldSizeZ)
    //    {
    //        // coordinates point at out side of the world
    //        return null; // block does not exist
    //    }

    //    Chunk chunk = World.Chunks[thisChunkX, thisChunkY, thisChunkZ];
    //    return chunk.Blocks[
    //        ConvertBlockIndexToLocal(localX),
    //        ConvertBlockIndexToLocal(localY),
    //        ConvertBlockIndexToLocal(localZ)];
    //}

    // for now only in the current chunk
    static bool ShouldCreateQuad(BlockType thisType, BlockType targetType)
    {
        if (thisType == BlockType.Water && targetType == BlockType.Water) return false;

        return !(targetType != BlockType.Water && targetType != BlockType.Air
            && thisType != BlockType.Water && thisType != BlockType.Air);
    }

    public static void CreateMeshes(ref BlockData[,,] blocks, out Mesh terrain, out Mesh water)
    {
        // Determining mesh size
        int size = 0, waterSize = 0;
        CalculateMeshSize(ref blocks, out size, out waterSize);
        
        var terrainData = new MeshData
        {
            uvs = new Vector2[size],
            suvs = new List<Vector2>(size),
            verticies = new Vector3[size],
            normals = new Vector3[size],
            triangles = new int[(int)(1.5f * size)]
        };
        
        var waterData = new MeshData
        {
            uvs = new Vector2[waterSize],
            suvs = new List<Vector2>(waterSize),
            verticies = new Vector3[waterSize],
            normals = new Vector3[waterSize],
            triangles = new int[(int)(1.5f * waterSize)]
        };
        
        var index = 0;
        var triIndex = 0;
        var waterIndex = 0;
        var waterTriIndex = 0;

        for (var z = 0; z < World.ChunkSize; z++)
            for (var y = 0; y < World.ChunkSize; y++)
                for (var x = 0; x < World.ChunkSize; x++)
                {
                    var b = blocks[x, y, z];

                    if (b.Faces == 0 || b.Type == BlockType.Air)
                        continue;

                    if (b.Type == BlockType.Water)
                        CreateStandardQuads(b, ref waterIndex, ref waterTriIndex, ref waterData, new Vector3(x, y, z));
                    else if (b.Type == BlockType.Grass)
                        CreateGrassQuads(b, ref index, ref triIndex, ref terrainData, new Vector3(x, y, z));
                    else
                        CreateStandardQuads(b, ref index, ref triIndex, ref terrainData, new Vector3(x, y, z));
                }

        // Create terrain mesh
        if (size > 0)
        {
            var terrainMesh = new Mesh
            {
                vertices = terrainData.verticies,
                normals = terrainData.normals,
                uv = terrainData.uvs, // Uvs maps the texture over the surface
                triangles = terrainData.triangles
            };
            terrainMesh.SetUVs(1, terrainData.suvs); // secondary uvs
            terrainMesh.RecalculateBounds();

            terrain = terrainMesh;
        }
        else terrain = null;
        
        // Create water mesh
        if (waterSize > 0)
        {
            var waterMesh = new Mesh
            {
                vertices = waterData.verticies,
                normals = waterData.normals,
                uv = waterData.uvs, // Uvs maps the texture over the surface
                triangles = waterData.triangles
            };
            waterMesh.SetUVs(1, waterData.suvs); // secondary uvs
            waterMesh.RecalculateBounds();

            water = waterMesh;
        }
        else water = null;
    }

    public static void CalculateMeshSize(ref BlockData[,,] blocks, out int terrainMeshSize, out int waterMeshSize)
    {
        int terrainSize = 0, waterSize = 0;

        for (var z = 0; z < World.ChunkSize; z++)
            for (var y = 0; y < World.ChunkSize; y++)
                for (var x = 0; x < World.ChunkSize; x++)
                {
                    var type = blocks[x, y, z].Type;
                    Cubeside faces = 0;

                    if (type != BlockType.Air)
                    {
                        // simplification - it does not look into surrounding chunks anymore
                        var size = 0;
                        if (x + 1 >= World.ChunkSize || ShouldCreateQuad(type, blocks[x + 1, y, z].Type)) { faces |= Cubeside.Right; size += 4; }
                        if (y + 1 >= World.ChunkSize || ShouldCreateQuad(type, blocks[x, y + 1, z].Type)) { faces |= Cubeside.Top; size += 4; }
                        if (z + 1 >= World.ChunkSize || ShouldCreateQuad(type, blocks[x, y, z + 1].Type)) { faces |= Cubeside.Front; size += 4; }
                        if (z <= 0 || ShouldCreateQuad(type, blocks[x, y, z - 1].Type)) { faces |= Cubeside.Back; size += 4; }
                        if (y <= 0 || ShouldCreateQuad(type, blocks[x, y - 1, z].Type)) { faces |= Cubeside.Bottom; size += 4; }
                        if (x <= 0 || ShouldCreateQuad(type, blocks[x - 1, y, z].Type)) { faces |= Cubeside.Left; size += 4; }

                        blocks[x, y, z].Faces = faces;

                        if (type == BlockType.Water)
                            waterSize += size;
                        else
                            terrainSize += size;
                    }
                }

        terrainMeshSize = terrainSize;
        waterMeshSize = waterSize;
    }

    static void CreateStandardQuads(BlockData block, ref int index, ref int triIndex, ref MeshData data, Vector3 offset)
    {
        int typeIndex = (int)block.Type;
        
        // all possible UVs
        // top uvs are used only if top side of the block is different
        Vector2 uv00 = _blockUVs[typeIndex, 0],
                uv10 = _blockUVs[typeIndex, 1],
                uv01 = _blockUVs[typeIndex, 2],
                uv11 = _blockUVs[typeIndex, 3];

        if (block.Faces.HasFlag(Cubeside.Top))
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.up,
                uv11, uv01, uv00, uv10,
                _p7 + offset, _p6 + offset, _p5 + offset, _p4 + offset);

        if (block.Faces.HasFlag(Cubeside.Bottom))
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.down,
                uv11, uv01, uv00, uv10,
                _p0 + offset, _p1 + offset, _p2 + offset, _p3 + offset);

        if (block.Faces.HasFlag(Cubeside.Left))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.left,
                uv11, uv01, uv00, uv10,
                _p7 + offset, _p4 + offset, _p0 + offset, _p3 + offset);
        }

        if (block.Faces.HasFlag(Cubeside.Right))
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.right,
                uv11, uv01, uv00, uv10,
                _p5 + offset, _p6 + offset, _p2 + offset, _p1 + offset);

        if (block.Faces.HasFlag(Cubeside.Front))
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.forward,
                uv11, uv01, uv00, uv10,
                _p4 + offset, _p5 + offset, _p1 + offset, _p0 + offset);

        if (block.Faces.HasFlag(Cubeside.Back))
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.back,
                uv11, uv01, uv00, uv10,
                _p6 + offset, _p7 + offset, _p3 + offset, _p2 + offset);
    }

    static void CreateGrassQuads(BlockData block, ref int index, ref int triIndex, ref MeshData data, Vector3 offset)
    {
        int typeIndex = (int)block.Type;
        
        var restIndex = typeIndex - 10;
        Vector2 uv00top = _blockUVs[typeIndex, 0],
                uv10top = _blockUVs[typeIndex, 1],
                uv01top = _blockUVs[typeIndex, 2],
                uv11top = _blockUVs[typeIndex, 3],
                uv00bot = _blockUVs[restIndex, 0],
                uv10bot = _blockUVs[restIndex, 1],
                uv01bot = _blockUVs[restIndex, 2],
                uv11bot = _blockUVs[restIndex, 3],
                uv00side = _blockUVs[typeIndex + 1, 0],
                uv10side = _blockUVs[typeIndex + 1, 1],
                uv01side = _blockUVs[typeIndex + 1, 2],
                uv11side = _blockUVs[typeIndex + 1, 3];

            if (block.Faces.HasFlag(Cubeside.Top))
            {
                AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.up,
                    uv11top, uv01top, uv00top, uv10top,
                    _p7 + offset, _p6 + offset, _p5 + offset, _p4 + offset);
            }

        if (block.Faces.HasFlag(Cubeside.Bottom))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.down,
                uv11bot, uv01bot, uv00bot, uv10bot,
                _p0 + offset, _p1 + offset, _p2 + offset, _p3 + offset);
        }

        if (block.Faces.HasFlag(Cubeside.Left))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.left,
                uv11side, uv01side, uv00side, uv10side,
                _p7 + offset, _p4 + offset, _p0 + offset, _p3 + offset);
        }

        if (block.Faces.HasFlag(Cubeside.Right))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.right,
                uv11side, uv01side, uv00side, uv10side,
                _p5 + offset, _p6 + offset, _p2 + offset, _p1 + offset);
        }

        if (block.Faces.HasFlag(Cubeside.Front))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.forward,
                uv11side, uv01side, uv00side, uv10side,
                _p4 + offset, _p5 + offset, _p1 + offset, _p0 + offset);
        }

        if (block.Faces.HasFlag(Cubeside.Back))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.back,
                uv11side, uv01side, uv00side, uv10side,
                _p6 + offset, _p7 + offset, _p3 + offset, _p2 + offset);
        }
    }

    static void AddQuadComponents(ref int index, ref int triIndex, BlockData block,
        ref MeshData data, 
        Vector3 normal,
        Vector2 uv11, Vector2 uv01, Vector2 uv00, Vector2 uv10,
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p4)
    {
        // add normals
        data.normals[index] = normal;
        data.normals[index + 1] = normal;
        data.normals[index + 2] = normal;
        data.normals[index + 3] = normal;

        // add uvs
        data.uvs[index] = uv11;
        data.uvs[index + 1] = uv01;
        data.uvs[index + 2] = uv00;
        data.uvs[index + 3] = uv10;

        // add verticies
        data.verticies[index] = p0;
        data.verticies[index + 1] = p1;
        data.verticies[index + 2] = p2;
        data.verticies[index + 3] = p4;

        // add suvs
        data.suvs.Add(_crackUVs[(int)block.HealthType, 3]); // top right corner
        data.suvs.Add(_crackUVs[(int)block.HealthType, 2]); // top left corner
        data.suvs.Add(_crackUVs[(int)block.HealthType, 0]); // bottom left corner
        data.suvs.Add(_crackUVs[(int)block.HealthType, 1]); // bottom right corner

        // add triangles
        data.triangles[triIndex++] = index + 3;
        data.triangles[triIndex++] = index + 1;
        data.triangles[triIndex++] = index;
        data.triangles[triIndex++] = index + 3;
        data.triangles[triIndex++] = index + 2;
        data.triangles[triIndex++] = index + 1;

        index += 4;
    }
}
