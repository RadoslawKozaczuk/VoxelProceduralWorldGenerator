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

    #region Constants
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
    static readonly Vector3 _p0 = new Vector3(-0.5f, -0.5f, 0.5f),
                            _p1 = new Vector3(0.5f, -0.5f, 0.5f),
                            _p2 = new Vector3(0.5f, -0.5f, -0.5f),
                            _p3 = new Vector3(-0.5f, -0.5f, -0.5f),
                            _p4 = new Vector3(-0.5f, 0.5f, 0.5f),
                            _p5 = new Vector3(0.5f, 0.5f, 0.5f),
                            _p6 = new Vector3(0.5f, 0.5f, -0.5f),
                            _p7 = new Vector3(-0.5f, 0.5f, -0.5f);
    #endregion
    
    public static void CreateMeshes(ref BlockData[,,] blocks, Vector3Int coord, out Mesh terrain, out Mesh water)
    {
        // Determining mesh size
        int size = 0, waterSize = 0;
        CalculateFacesAndMeshSize(ref blocks, coord, out size, out waterSize);
        
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
                        CreateWaterQuads(b, ref waterIndex, ref waterTriIndex, ref waterData, new Vector3(x, y, z));
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
    
    static bool QuadVisibilityCheck(BlockType target) => target == BlockType.Air || target == BlockType.Water;
    
    static void PerformInterChunkCheck(ref BlockData[,,] blocks, Vector3Int chunkCoord, Vector3Int blockCoord, ref int meshSize)
    {
        BlockData block;
        Cubeside faces = 0;

        if (blockCoord.x == World.ChunkSize - 1)
        {
            if(World.TryGetBlockFromChunk(chunkCoord.x + 1, chunkCoord.y, chunkCoord.z,
                0, blockCoord.y, blockCoord.z, out block))
            {
                if (QuadVisibilityCheck(block.Type)) { faces |= Cubeside.Right; meshSize += 4; }
            }
            else { faces |= Cubeside.Right; meshSize += 4; }
        }
        else if(QuadVisibilityCheck(blocks[blockCoord.x + 1, blockCoord.y, blockCoord.z].Type))
        { faces |= Cubeside.Right; meshSize += 4; }

        if (blockCoord.x == 0)
        {
            if(World.TryGetBlockFromChunk(chunkCoord.x - 1, chunkCoord.y, chunkCoord.z, 
                World.ChunkSize - 1, blockCoord.y, blockCoord.z, out block))
            {
                if (QuadVisibilityCheck(block.Type)) { faces |= Cubeside.Left; meshSize += 4; }
            }
            else { faces |= Cubeside.Left; meshSize += 4; }
        }
        else if (QuadVisibilityCheck(blocks[blockCoord.x - 1, blockCoord.y, blockCoord.z].Type))
        { faces |= Cubeside.Left; meshSize += 4; }

        if (blockCoord.y == World.ChunkSize - 1)
        {
            if(World.TryGetBlockFromChunk(chunkCoord.x, chunkCoord.y + 1, chunkCoord.z,
                blockCoord.x, 0, blockCoord.z, out block))
            {
                if (QuadVisibilityCheck(block.Type)) { faces |= Cubeside.Top; meshSize += 4; }
            }
            else { faces |= Cubeside.Top; meshSize += 4; }
        }
        else if (QuadVisibilityCheck(blocks[blockCoord.x, blockCoord.y + 1, blockCoord.z].Type))
        { faces |= Cubeside.Top; meshSize += 4; }

        if (blockCoord.y == 0)
        {
            if(World.TryGetBlockFromChunk(chunkCoord.x, chunkCoord.y - 1, chunkCoord.z, 
                blockCoord.x, World.ChunkSize - 1, blockCoord.z, out block))
            {
                if (QuadVisibilityCheck(block.Type)) { faces |= Cubeside.Bottom; meshSize += 4; }
            }
            else { faces |= Cubeside.Bottom; meshSize += 4; }
        }
        else if (QuadVisibilityCheck(blocks[blockCoord.x, blockCoord.y - 1, blockCoord.z].Type))
        { faces |= Cubeside.Bottom; meshSize += 4; }

        if (blockCoord.z == World.ChunkSize - 1)
        {
            if(World.TryGetBlockFromChunk(chunkCoord.x, chunkCoord.y, chunkCoord.z + 1,
                blockCoord.x, blockCoord.y, 0, out block))
            {
                if (QuadVisibilityCheck(block.Type)) { faces |= Cubeside.Front; meshSize += 4; }
            }
            else { faces |= Cubeside.Front; meshSize += 4; }
        }
        else if (QuadVisibilityCheck(blocks[blockCoord.x, blockCoord.y, blockCoord.z + 1].Type))
        { faces |= Cubeside.Front; meshSize += 4; }

        if (blockCoord.z == 0)
        {
            if(World.TryGetBlockFromChunk(chunkCoord.x, chunkCoord.y, chunkCoord.z - 1,
                blockCoord.x, blockCoord.y, World.ChunkSize - 1, out block))
            {
                if (QuadVisibilityCheck(block.Type)) { faces |= Cubeside.Back; meshSize += 4; }
            }
            else { faces |= Cubeside.Back; meshSize += 4; }
        }
        else if (QuadVisibilityCheck(blocks[blockCoord.x, blockCoord.y, blockCoord.z - 1].Type))
        { faces |= Cubeside.Back; meshSize += 4; }

        blocks[blockCoord.x, blockCoord.y, blockCoord.z].Faces = faces;
    }

    static void WaterInterChunkCheck(ref BlockData[,,] blocks, Vector3Int chunkCoord, Vector3Int blockCoord, ref int meshSize)
    {
        if (blockCoord.y == World.ChunkSize - 1)
        {
            BlockData block;
            if (World.TryGetBlockFromChunk(chunkCoord.x, chunkCoord.y + 1, chunkCoord.z, blockCoord.x, 0, blockCoord.z, out block))
            {
                if (block.Type == BlockType.Air)
                {
                    blocks[blockCoord.x, blockCoord.y, blockCoord.z].Faces |= Cubeside.Top;
                    meshSize += 4;
                }
            }
        }
        else if (blocks[blockCoord.x, blockCoord.y + 1, blockCoord.z].Type == BlockType.Air)
        {
            blocks[blockCoord.x, blockCoord.y, blockCoord.z].Faces |= Cubeside.Top;
            meshSize += 4;
        }
    }

    public static void CalculateFacesAndMeshSize(ref BlockData[,,] blocks, Vector3Int chunkCoord, 
        out int terrainMeshSize, out int waterMeshSize)
    {
        var chunkSize = World.ChunkSize;
        terrainMeshSize = 0;
        waterMeshSize = 0;
        
        int x, y, z;
        // internal blocks
        for (z = 1; z < chunkSize - 1; z++)
            for (y = 1; y < chunkSize - 1; y++)
                for (x = 1; x < chunkSize - 1; x++)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockType.Water)
                    {
                        if (blocks[x, y + 1, z].Type == BlockType.Air)
                        {
                            blocks[x, y, z].Faces |= Cubeside.Top;
                            waterMeshSize += 4;
                        }
                    }
                    else if (type != BlockType.Air)
                    {
                        Cubeside faces = 0;
                        if (QuadVisibilityCheck(blocks[x + 1, y, z].Type)) { faces |= Cubeside.Right; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x - 1, y, z].Type)) { faces |= Cubeside.Left; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y + 1, z].Type)) { faces |= Cubeside.Top; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y - 1, z].Type)) { faces |= Cubeside.Bottom; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y, z + 1].Type)) { faces |= Cubeside.Front; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y, z - 1].Type)) { faces |= Cubeside.Back; terrainMeshSize += 4; }
                        blocks[x, y, z].Faces = faces;
                    }
                }

        // right and left entire squares boundaries check
        for (z = 0; z < chunkSize; z++)
            for (y = 0; y < chunkSize; y++)
                for (x = 0; x < chunkSize; x += chunkSize - 1)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockType.Water)
                        WaterInterChunkCheck(ref blocks, chunkCoord, new Vector3Int(x, y, z), ref waterMeshSize);
                    else if (type != BlockType.Air)
                        PerformInterChunkCheck(ref blocks, chunkCoord, new Vector3Int(x, y, z), ref terrainMeshSize);
                }

        // top and bottom rectangles boundaries check
        y = chunkSize - 1;
        for (z = 0; z < chunkSize; z++)
            for (y = 0; y < chunkSize; y += chunkSize - 1)
                for (x = 1; x < chunkSize - 1; x++)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockType.Water)
                        WaterInterChunkCheck(ref blocks, chunkCoord, new Vector3Int(x, y, z), ref waterMeshSize);
                    else if (type != BlockType.Air)
                        PerformInterChunkCheck(ref blocks, chunkCoord, new Vector3Int(x, y, z), ref terrainMeshSize);
                }

        // front and back intarnal squares boundaries check
        for (z = 0; z < chunkSize; z += chunkSize - 1)
            for (y = 1; y < chunkSize - 1; y++)
                for (x = 1; x < chunkSize - 1; x++)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockType.Water)
                        WaterInterChunkCheck(ref blocks, chunkCoord, new Vector3Int(x, y, z), ref waterMeshSize);
                    else if (type != BlockType.Air)
                        PerformInterChunkCheck(ref blocks, chunkCoord, new Vector3Int(x, y, z), ref terrainMeshSize);
                }
    }

    static void CreateStandardQuads(BlockData block, ref int index, ref int triIndex, ref MeshData data, Vector3 blockCoord)
    {
        int typeIndex = (int)block.Type;
        
        // all possible UVs
        Vector2 uv00 = _blockUVs[typeIndex, 0],
                uv10 = _blockUVs[typeIndex, 1],
                uv01 = _blockUVs[typeIndex, 2],
                uv11 = _blockUVs[typeIndex, 3];

        if (block.Faces.HasFlag(Cubeside.Top))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.up,
                uv11, uv01, uv00, uv10,
                _p7 + blockCoord, _p6 + blockCoord, _p5 + blockCoord, _p4 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Bottom))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.down,
                uv11, uv01, uv00, uv10,
                _p0 + blockCoord, _p1 + blockCoord, _p2 + blockCoord, _p3 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Left))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.left,
                uv11, uv01, uv00, uv10,
                _p7 + blockCoord, _p4 + blockCoord, _p0 + blockCoord, _p3 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Right))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.right,
                uv11, uv01, uv00, uv10,
                _p5 + blockCoord, _p6 + blockCoord, _p2 + blockCoord, _p1 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Front))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.forward,
                uv11, uv01, uv00, uv10,
                _p4 + blockCoord, _p5 + blockCoord, _p1 + blockCoord, _p0 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Back))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.back,
                uv11, uv01, uv00, uv10,
                _p6 + blockCoord, _p7 + blockCoord, _p3 + blockCoord, _p2 + blockCoord);
            AddSuvs(block, ref data);
        }
    }

    static void CreateGrassQuads(BlockData block, ref int index, ref int triIndex, ref MeshData data, Vector3 blockCoord)
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
                _p7 + blockCoord, _p6 + blockCoord, _p5 + blockCoord, _p4 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Bottom))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.down,
                uv11bot, uv01bot, uv00bot, uv10bot,
                _p0 + blockCoord, _p1 + blockCoord, _p2 + blockCoord, _p3 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Left))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.left,
                uv11side, uv01side, uv00side, uv10side,
                _p7 + blockCoord, _p4 + blockCoord, _p0 + blockCoord, _p3 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Right))
        { 
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.right,
                uv11side, uv01side, uv00side, uv10side,
                _p5 + blockCoord, _p6 + blockCoord, _p2 + blockCoord, _p1 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Front))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.forward,
                uv11side, uv01side, uv00side, uv10side,
                _p4 + blockCoord, _p5 + blockCoord, _p1 + blockCoord, _p0 + blockCoord);
            AddSuvs(block, ref data);
        }

        if (block.Faces.HasFlag(Cubeside.Back))
        {
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.back,
                uv11side, uv01side, uv00side, uv10side,
                _p6 + blockCoord, _p7 + blockCoord, _p3 + blockCoord, _p2 + blockCoord);
            AddSuvs(block, ref data);
        }
    }

    static void CreateWaterQuads(BlockData block, ref int index, ref int triIndex, ref MeshData data, Vector3 blockCoord)
    {
        float uvConst = 1.0f / World.ChunkSize;

        // all possible UVs
        // left-top, right-top, left-bottom, right-bottom
        Vector2 uv00 = new Vector2(uvConst * blockCoord.x, 1 - uvConst * blockCoord.z),
                uv10 = new Vector2(uvConst * (blockCoord.x + 1), 1 - uvConst * blockCoord.z),
                uv01 = new Vector2(uvConst * blockCoord.x, 1 - uvConst * (blockCoord.z + 1)),
                uv11 = new Vector2(uvConst * (blockCoord.x + 1), 1 - uvConst * (blockCoord.z + 1));

        if (block.Faces.HasFlag(Cubeside.Top))
            AddQuadComponents(ref index, ref triIndex, block, ref data, Vector3.up,
                uv11, uv01, uv00, uv10,
                _p7 + blockCoord, _p6 + blockCoord, _p5 + blockCoord, _p4 + blockCoord);
        
    }

    static void AddQuadComponents(ref int index, ref int triIndex, BlockData block,
        ref MeshData data, 
        Vector3 normal,
        Vector2 uv11, Vector2 uv01, Vector2 uv00, Vector2 uv10,
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // add normals
        data.normals[index] = normal;
        data.normals[index + 1] = normal;
        data.normals[index + 2] = normal;
        data.normals[index + 3] = normal;

        // add uvs
        data.uvs[index] = uv00;
        data.uvs[index + 1] = uv10;
        data.uvs[index + 2] = uv11;
        data.uvs[index + 3] = uv01;

        // add verticies
        data.verticies[index] = p0;
        data.verticies[index + 1] = p1;
        data.verticies[index + 2] = p2;
        data.verticies[index + 3] = p3;
        
        // add triangles
        data.triangles[triIndex++] = index + 3;
        data.triangles[triIndex++] = index + 1;
        data.triangles[triIndex++] = index;
        data.triangles[triIndex++] = index + 3;
        data.triangles[triIndex++] = index + 2;
        data.triangles[triIndex++] = index + 1;

        index += 4;
    }

    static void AddSuvs(BlockData block, ref MeshData data)
    {
        data.suvs.Add(_crackUVs[(int)block.HealthType, 3]); // top right corner
        data.suvs.Add(_crackUVs[(int)block.HealthType, 2]); // top left corner
        data.suvs.Add(_crackUVs[(int)block.HealthType, 0]); // bottom left corner
        data.suvs.Add(_crackUVs[(int)block.HealthType, 1]); // bottom right corner
    }
}
