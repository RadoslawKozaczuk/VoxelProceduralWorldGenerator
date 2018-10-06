using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public struct MeshData
{
    public Vector2[] Uvs;
    public List<Vector2> Suvs;
    public Vector3[] Verticies;
    public Vector3[] Normals;
    public int[] Triangles;
}

public class MeshGenerator
{
    Stopwatch _stopwatch = new Stopwatch();
    long _accumulatedExtractMeshDataTime, _accumulatedCreateMeshTime;

    #region Readonly lookup tables
    readonly Vector2[,] _blockUVs = { 
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

    // order goes as follows
    // NoCrack, Crack1, Crack2, Crack3, Crack4, Crack5, Crack6, Crack7, Crack8, Crack9, Crack10
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

    readonly int _chunkSize, _worldSizeX, _worldSizeY, _worldSizeZ, _totalBlockNumberX, _totalBlockNumberY, _totalBlockNumberZ;

    public MeshGenerator(int chunkSize, int worldSizeX, int worldSizeY, int worldSizeZ)
    {
        _chunkSize = chunkSize;
        _worldSizeX = worldSizeX;
        _worldSizeY = worldSizeY;
        _worldSizeZ = worldSizeZ;
        _totalBlockNumberX = _worldSizeX * _chunkSize;
        _totalBlockNumberY = _worldSizeY * _chunkSize;
        _totalBlockNumberZ = _worldSizeZ * _chunkSize;

        _crackUVs = FillCrackUvTable();
    }

    /// <summary>
    /// This method creates mesh data necessary to create a mesh.
    /// Data for both terrain and water meshes are created.
    /// </summary>
    public void ExtractMeshData(ref Block[,,] blocks, ref Vector3Int chunkPos, out MeshData terrain, out MeshData water)
    {
        _stopwatch.Restart();

        // Determining mesh size
        int tSize = 0, wSize = 0;
        CalculateMeshSize(ref blocks, ref chunkPos, out tSize, out wSize);

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
        
        for (int x = 0; x < _chunkSize; x++)
            for (int y = 0; y < _chunkSize; y++)
                for (int z = 0; z < _chunkSize; z++)
                {
                    // offset need to be included
                    var b = blocks[x + chunkPos.x, y + chunkPos.y, z + chunkPos.z];
                    
                    if (b.Faces == 0 || b.Type == BlockTypes.Air)
                        continue;

                    if (b.Type == BlockTypes.Water)
                        CreateWaterQuads(ref b, ref waterIndex, ref waterTriIndex, ref waterData, new Vector3(x, y, z));
                    else if (b.Type == BlockTypes.Grass)
                        CreateGrassQuads(ref b, ref index, ref triIndex, ref terrainData, new Vector3(x, y, z));
                    else
                        CreateStandardQuads(ref b, ref index, ref triIndex, ref terrainData, new Vector3(x, y, z));
                }

        terrain = terrainData;
        water = waterData;

        _stopwatch.Stop();
        _accumulatedExtractMeshDataTime += _stopwatch.ElapsedMilliseconds;
    }

    void CalculateMeshSize(ref Block[,,] blocks, ref Vector3Int chunkCoord, out int tSize, out int wSize)
    {
        tSize = 0;
        wSize = 0;

        // offset needs to be calculated
        for (int x = chunkCoord.x; x < chunkCoord.x + _chunkSize; x++)
            for (int y = chunkCoord.y; y < chunkCoord.y + _chunkSize; y++)
                for (int z = chunkCoord.z; z < chunkCoord.z + _chunkSize; z++)
                {
                    if (blocks[x, y, z].Type == BlockTypes.Water)
                    {
                        if ((blocks[x, y, z].Faces & Cubesides.Top) == Cubesides.Top) wSize += 4;
                    }
                    else if (blocks[x, y, z].Type != BlockTypes.Air)
                    {
                        if ((blocks[x, y, z].Faces & Cubesides.Right) == Cubesides.Right) tSize += 4;
                        if ((blocks[x, y, z].Faces & Cubesides.Left) == Cubesides.Left) tSize += 4;
                        if ((blocks[x, y, z].Faces & Cubesides.Top) == Cubesides.Top) tSize += 4;
                        if ((blocks[x, y, z].Faces & Cubesides.Bottom) == Cubesides.Bottom) tSize += 4;
                        if ((blocks[x, y, z].Faces & Cubesides.Front) == Cubesides.Front) tSize += 4;
                        if ((blocks[x, y, z].Faces & Cubesides.Back) == Cubesides.Back) tSize += 4;
                    }
                }
    }

    public Mesh CreateMesh(MeshData meshData)
    {
        _stopwatch.Restart();

        var mesh = new Mesh
        {
            vertices = meshData.Verticies,
            normals = meshData.Normals,
            uv = meshData.Uvs, // Uvs maps the texture over the surface
            triangles = meshData.Triangles
        };
        mesh.SetUVs(1, meshData.Suvs); // secondary uvs
        mesh.RecalculateBounds();

        _stopwatch.Stop();
        _accumulatedCreateMeshTime += _stopwatch.ElapsedMilliseconds;

        return mesh;
    }

    public void LogTimeSpent()
    {
        UnityEngine.Debug.Log($"It took {_accumulatedExtractMeshDataTime} ms to extract all mesh data.");
        UnityEngine.Debug.Log($"It took {_accumulatedCreateMeshTime} ms to generate all meshes.");
    }

    bool QuadVisibilityCheck(BlockTypes target) => target == BlockTypes.Air || target == BlockTypes.Water;

    void PerformTerrainCheck(ref Block[,,] blocks, int blockX, int blockY, int blockZ, ref int meshSize)
    {
        Cubesides faces = 0;

        // right edge
        if (blockX == _worldSizeX - 1)
        { faces |= Cubesides.Right; meshSize += 4; }
        else if (QuadVisibilityCheck(blocks[blockX + 1, blockY, blockZ].Type))
        { faces |= Cubesides.Right; meshSize += 4; }

        // left edge
        if (blockX == 0)
        { faces |= Cubesides.Left; meshSize += 4; }
        else if (QuadVisibilityCheck(blocks[blockX - 1, blockY, blockZ].Type))
        { faces |= Cubesides.Left; meshSize += 4; }

        // top edge
        if (blockY == _worldSizeY - 1)
        { faces |= Cubesides.Top; meshSize += 4; }
        else if (QuadVisibilityCheck(blocks[blockX, blockY + 1, blockZ].Type))
        { faces |= Cubesides.Top; meshSize += 4; }

        // bottom edge
        if (blockY == 0)
        { faces |= Cubesides.Bottom; meshSize += 4; }
        else if (QuadVisibilityCheck(blocks[blockX, blockY - 1, blockZ].Type))
        { faces |= Cubesides.Bottom; meshSize += 4; }

        // front
        if (blockZ == _worldSizeZ - 1)
        { faces |= Cubesides.Front; meshSize += 4; }
        else if (QuadVisibilityCheck(blocks[blockX, blockY, blockZ + 1].Type))
        { faces |= Cubesides.Front; meshSize += 4; }

        // back
        if (blockZ == 0)
        { faces |= Cubesides.Back; meshSize += 4; }
        else if (QuadVisibilityCheck(blocks[blockX, blockY, blockZ - 1].Type))
        { faces |= Cubesides.Back; meshSize += 4; }

        blocks[blockX, blockY, blockZ].Faces = faces;
    }

    void WaterInterChunkCheck(ref Block[,,] blocks, int blockX, int blockY, int blockZ, ref int meshSize)
    {
        if (blockY == _worldSizeY - 1)
        { blocks[blockX, blockY + 1, blockZ].Faces |= Cubesides.Top; meshSize += 4; }
        else if (blocks[blockX, blockY + 1, blockZ].Type == BlockTypes.Air)
        { blocks[blockX, blockY + 1, blockZ].Faces |= Cubesides.Top; meshSize += 4; }
    }

    /// <summary>
    /// Return true if the particular block could be find in the the chunk.
    /// False if otherwise. In case of false a new dummy block will be returned.
    /// </summary>
    bool TryGetBlockFromChunk(int blockX, int blockY, int blockZ, ref Block[,,] blocks, out Block block)
    {
        if (blockX >= _worldSizeX || blockX < 0
            || blockY >= _worldSizeY || blockY < 0
            || blockZ >= _worldSizeZ || blockZ < 0)
        {
            // we are outside of the world!
            block = new Block(); // dummy data
            return false;
        }

        block = blocks[blockX, blockY, blockZ];

        return true;
    }

    /// <summary>
    /// Return true if the particular block could be find in the the chunk.
    /// False if otherwise. In case of false a new dummy block will be returned.
    /// </summary>
    //bool TryGetBlockFromChunk(int chunkX, int chunkY, int chunkZ, int blockX, int blockY, int blockZ,
    //    ref Block[,,] blocks, out Block block)
    //{
    //    if (chunkX >= _worldSizeX || chunkX < 0
    //        || chunkY >= _worldSizeY || chunkY < 0
    //        || chunkZ >= _worldSizeZ || chunkZ < 0)
    //    {
    //        // we are outside of the world!
    //        block = new Block(); // dummy data
    //        return false;
    //    }

    //    block = blocks[blockX, blockY, blockZ];

    //    return true;
    //}

    public void CalculateFaces(ref Block[,,] blocks)
    {
        for (int x = 0; x < _totalBlockNumberX; x++)
            for (int y = 0; y < _totalBlockNumberY; y++)
                for (int z = 0; z < _totalBlockNumberZ; z++)
                {
                    var type = blocks[x, y, z].Type;

                    if(type == BlockTypes.Air)
                    {
                        if(x < _totalBlockNumberX - 1)
                            if (blocks[x + 1, y, z].Type != BlockTypes.Air) blocks[x + 1, y, z].Faces |= Cubesides.Left;

                        if (y < _totalBlockNumberY - 1)
                            if (blocks[x, y + 1, z].Type != BlockTypes.Air) blocks[x, y + 1, z].Faces |= Cubesides.Bottom;

                        if (z < _totalBlockNumberZ - 1)
                            if (blocks[x, y, z + 1].Type != BlockTypes.Air) blocks[x, y, z + 1].Faces |= Cubesides.Back;
                    }
                    else
                    {
                        if (x < _totalBlockNumberX - 1)
                            if (blocks[x + 1, y, z].Type == BlockTypes.Air) blocks[x, y, z].Faces |= Cubesides.Right;

                        if (y < _totalBlockNumberY - 1)
                            if (blocks[x, y + 1, z].Type == BlockTypes.Air) blocks[x, y, z].Faces |= Cubesides.Top;

                        if (z < _totalBlockNumberZ - 1)
                            if (blocks[x, y, z + 1].Type == BlockTypes.Air) blocks[x, y, z].Faces |= Cubesides.Front;
                    }
                }
    }
    
    public void WorldBoundariesCheck(ref Block[,,] blocks)
    {
        // right world boundaries check
        int x = _totalBlockNumberX - 1, y = 0, z = 0;

        for (y = 0; y < _totalBlockNumberY; y++)
            for (z = 0; z < _totalBlockNumberZ; z++)
                if (blocks[x, y, z].Type != BlockTypes.Water && blocks[x, y, z].Type != BlockTypes.Air)
                    blocks[x, y, z].Faces |= Cubesides.Right;

        // left world boundaries check
        x = 0;
        for (y = 0; y < _totalBlockNumberY; y++)
            for (z = 0; z < _totalBlockNumberZ; z++)
                if (blocks[x, y, z].Type != BlockTypes.Water && blocks[x, y, z].Type != BlockTypes.Air)
                    blocks[x, y, z].Faces |= Cubesides.Left;

        // top world boundaries check
        y = _totalBlockNumberY - 1;
        for (x = 0; x < _totalBlockNumberX; x++)
            for (z = 0; z < _totalBlockNumberZ; z++)
                    blocks[x, y, z].Faces |= Cubesides.Top; // there will always be air

        // bottom world boundaries check
        y = 0;
        for (x = 0; x < _totalBlockNumberX; x++)
            for (z = 0; z < _totalBlockNumberZ; z++)
                blocks[x, y, z].Faces |= Cubesides.Bottom; // there will always be bedrock

        // front world boundaries check
        z = _totalBlockNumberZ - 1;
        for (x = 0; x < _totalBlockNumberX; x++)
            for (y = 0; y < _totalBlockNumberY; y++)
                if (blocks[x, y, z].Type != BlockTypes.Water && blocks[x, y, z].Type != BlockTypes.Air)
                    blocks[x, y, z].Faces |= Cubesides.Front;

        // back world boundaries check
        z = 0;
        for (x = 0; x < _totalBlockNumberX; x++)
            for (y = 0; y < _totalBlockNumberY; y++)
                if (blocks[x, y, z].Type != BlockTypes.Water && blocks[x, y, z].Type != BlockTypes.Air)
                    blocks[x, y, z].Faces |= Cubesides.Back;
    }

    void CalculateFacesAndMeshSize(ref Block[,,] blocks, Vector3Int chunkCoord,
        out int terrainMeshSize, out int waterMeshSize)
    {
        terrainMeshSize = 0;
        waterMeshSize = 0;

        // internal blocks
        int x = 1 + chunkCoord.x * _chunkSize,
            y = 1 + chunkCoord.y * _chunkSize,
            z = 1 + chunkCoord.z * _chunkSize,
            endX = x + _chunkSize - 2,
            endY = y + _chunkSize - 2,
            endZ = z + _chunkSize - 2;
        for (; z < endZ; z++)
            for (; y < endY; y++)
                for (; x < endX; x++)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockTypes.Water)
                    {
                        if (blocks[x, y + 1, z].Type == BlockTypes.Air)
                        {
                            blocks[x, y, z].Faces |= Cubesides.Top;
                            waterMeshSize += 4;
                        }
                    }
                    else if (type != BlockTypes.Air)
                    {
                        Cubesides faces = 0;
                        if (QuadVisibilityCheck(blocks[x + 1, y, z].Type)) { faces |= Cubesides.Right; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x - 1, y, z].Type)) { faces |= Cubesides.Left; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y + 1, z].Type)) { faces |= Cubesides.Top; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y - 1, z].Type)) { faces |= Cubesides.Bottom; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y, z + 1].Type)) { faces |= Cubesides.Front; terrainMeshSize += 4; }
                        if (QuadVisibilityCheck(blocks[x, y, z - 1].Type)) { faces |= Cubesides.Back; terrainMeshSize += 4; }
                        blocks[x, y, z].Faces = faces;
                    }
                }

        // right and left entire squares boundaries check
        x = 0 + chunkCoord.x * _chunkSize;
        y = 0 + chunkCoord.y * _chunkSize;
        z = 0 + chunkCoord.z * _chunkSize;
        endX = x + _chunkSize - 1;
        endY = y + _chunkSize - 1;
        endZ = z + _chunkSize - 1;
        for (; z < endZ; z++)
            for (; y < endY; y++)
                for (; x < endX; x += _chunkSize - 1)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockTypes.Water)
                        WaterInterChunkCheck(ref blocks, x, y, z, ref waterMeshSize);
                    else if (type != BlockTypes.Air)
                        PerformTerrainCheck(ref blocks, x, y, z, ref terrainMeshSize);
                }

        // top and bottom rectangles boundaries check
        x = 1 + chunkCoord.x * _chunkSize;
        y = chunkCoord.y * _chunkSize;
        z = chunkCoord.z * _chunkSize;
        endX = x + _chunkSize - 1;
        endY = y + _chunkSize;
        endZ = z + _chunkSize;
        for (; z < endZ; z++)
            for (; y < endY; y += _chunkSize - 1)
                for (; x < endX - 1; x++)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockTypes.Water)
                        WaterInterChunkCheck(ref blocks, x, y, z, ref waterMeshSize);
                    else if (type != BlockTypes.Air)
                        PerformTerrainCheck(ref blocks, x, y, z, ref terrainMeshSize);
                }

        // front and back intarnal squares boundaries check
        x = 1 + chunkCoord.x * _chunkSize;
        y = 1 + chunkCoord.y * _chunkSize;
        z = chunkCoord.z * _chunkSize;
        endX = x + _chunkSize - 1;
        endY = y + _chunkSize - 1;
        endZ = z + _chunkSize;
        for (; z < endZ; z += _chunkSize - 1)
            for (; y < endY; y++)
                for (; x < endX; x++)
                {
                    var type = blocks[x, y, z].Type;

                    if (type == BlockTypes.Water)
                        WaterInterChunkCheck(ref blocks, x, y, z, ref waterMeshSize);
                    else if (type != BlockTypes.Air)
                        PerformTerrainCheck(ref blocks, x, y, z, ref terrainMeshSize);
                }
    }

    void CreateStandardQuads(ref Block block, ref int index, ref int triIndex, ref MeshData data, Vector3 localBlockCoord)
    {
        int typeIndex = (int)block.Type;

        // all possible UVs
        Vector2 uv00 = _blockUVs[typeIndex, 0],
                uv10 = _blockUVs[typeIndex, 1],
                uv01 = _blockUVs[typeIndex, 2],
                uv11 = _blockUVs[typeIndex, 3];

        if (block.Faces.HasFlag(Cubesides.Top))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.up,
                uv11, uv01, uv00, uv10,
                _p7 + localBlockCoord, _p6 + localBlockCoord, _p5 + localBlockCoord, _p4 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Bottom))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.down,
                uv11, uv01, uv00, uv10,
                _p0 + localBlockCoord, _p1 + localBlockCoord, _p2 + localBlockCoord, _p3 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Left))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.left,
                uv11, uv01, uv00, uv10,
                _p7 + localBlockCoord, _p4 + localBlockCoord, _p0 + localBlockCoord, _p3 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Right))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.right,
                uv11, uv01, uv00, uv10,
                _p5 + localBlockCoord, _p6 + localBlockCoord, _p2 + localBlockCoord, _p1 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Front))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.forward,
                uv11, uv01, uv00, uv10,
                _p4 + localBlockCoord, _p5 + localBlockCoord, _p1 + localBlockCoord, _p0 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Back))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.back,
                uv11, uv01, uv00, uv10,
                _p6 + localBlockCoord, _p7 + localBlockCoord, _p3 + localBlockCoord, _p2 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }
    }

    void CreateGrassQuads(ref Block block, ref int index, ref int triIndex, ref MeshData data, Vector3 localBlockCoord)
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

        if (block.Faces.HasFlag(Cubesides.Top))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.up,
                uv11top, uv01top, uv00top, uv10top,
                _p7 + localBlockCoord, _p6 + localBlockCoord, _p5 + localBlockCoord, _p4 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Bottom))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.down,
                uv11bot, uv01bot, uv00bot, uv10bot,
                _p0 + localBlockCoord, _p1 + localBlockCoord, _p2 + localBlockCoord, _p3 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Left))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.left,
                uv00side, uv10side, uv11side, uv01side,
                _p7 + localBlockCoord, _p4 + localBlockCoord, _p0 + localBlockCoord, _p3 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Right))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.right,
                uv00side, uv10side, uv11side, uv01side,
                _p5 + localBlockCoord, _p6 + localBlockCoord, _p2 + localBlockCoord, _p1 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Front))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.forward,
                uv00side, uv10side, uv11side, uv01side,
                _p4 + localBlockCoord, _p5 + localBlockCoord, _p1 + localBlockCoord, _p0 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }

        if (block.Faces.HasFlag(Cubesides.Back))
        {
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.back,
                uv00side, uv10side, uv11side, uv01side,
                _p6 + localBlockCoord, _p7 + localBlockCoord, _p3 + localBlockCoord, _p2 + localBlockCoord);
            AddSuvs(ref block, ref data);
        }
    }

    void CreateWaterQuads(ref Block block, ref int index, ref int triIndex, ref MeshData data, Vector3 localBlockCoord)
    {
        float uvConst = 1.0f / _chunkSize;

        // all possible UVs
        // left-top, right-top, left-bottom, right-bottom
        Vector2 uv00 = new Vector2(uvConst * localBlockCoord.x, 1 - uvConst * localBlockCoord.z),
                uv10 = new Vector2(uvConst * (localBlockCoord.x + 1), 1 - uvConst * localBlockCoord.z),
                uv01 = new Vector2(uvConst * localBlockCoord.x, 1 - uvConst * (localBlockCoord.z + 1)),
                uv11 = new Vector2(uvConst * (localBlockCoord.x + 1), 1 - uvConst * (localBlockCoord.z + 1));

        if (block.Faces.HasFlag(Cubesides.Top))
            AddQuadComponents(ref index, ref triIndex, ref data, Vector3.up,
                uv11, uv01, uv00, uv10,
                _p7 + localBlockCoord, _p6 + localBlockCoord, _p5 + localBlockCoord, _p4 + localBlockCoord);

    }

    void AddQuadComponents(ref int index, ref int triIndex,
        ref MeshData data,
        Vector3 normal,
        Vector2 uv11, Vector2 uv01, Vector2 uv00, Vector2 uv10,
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

    void AddSuvs(ref Block block, ref MeshData data)
    {
        data.Suvs.Add(_crackUVs[block.HealthLevel, 3]); // top right corner
        data.Suvs.Add(_crackUVs[block.HealthLevel, 2]); // top left corner
        data.Suvs.Add(_crackUVs[block.HealthLevel, 0]); // bottom left corner
        data.Suvs.Add(_crackUVs[block.HealthLevel, 1]); // bottom right corner
    }

    Vector2[,] FillCrackUvTable()
    {
        var crackUVs = new Vector2[11, 4];

        // NoCrack
        crackUVs[0, 0] = new Vector2(0.6875f, 0f);
        crackUVs[0, 1] = new Vector2(0.75f, 0f);
        crackUVs[0, 2] = new Vector2(0.6875f, 0.0625f);
        crackUVs[0, 3] = new Vector2(0.75f, 0.0625f);

        float singleUnit = 0.0625f;

        // Crack1, Crack2, Crack3, Crack4, Crack5, Crack6, Crack7, Crack8, Crack9, Crack10
        for (int i = 1; i < 11; i++)
        {
            crackUVs[i, 0] = new Vector2((i - 1) * singleUnit, 0f); // left-bottom
            crackUVs[i, 1] = new Vector2(i * singleUnit, 0f); // right-bottom
            crackUVs[i, 2] = new Vector2((i - 1) * singleUnit, 0.0625f); // left-top
            crackUVs[i, 3] = new Vector2(i * singleUnit, 0.0625f); // right-top
        }

        return crackUVs;
    }
}
