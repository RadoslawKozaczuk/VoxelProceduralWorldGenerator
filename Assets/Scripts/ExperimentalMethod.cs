using UnityEngine;
using System;
using System.Collections.Generic;

public class ExperimentalMethod : MonoBehaviour
{
    [Flags]
    enum Directions : byte { Top = 1, Bottom = 2, Left = 4, Right = 8, Front = 16, Back = 32 }
    enum TerrainTypes : byte
    {
        Dirt, Stone, Diamond, Bedrock, Redstone, Sand, Leaves, Wood, Woodbase,
        Water,
        Grass,
        Air
    }

    public Material material;
    
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
    
    // assumptions used:
    // coordination start left down corner
    readonly Vector2[,] _blockUVs = {
        { // DIRT
            new Vector2(0.125f, 0.9375f), new Vector2(0.1875f, 0.9375f),
            new Vector2(0.125f, 1.0f), new Vector2(0.1875f, 1.0f)
        },
    };

    struct BlockStruct
    {
        public TerrainTypes Type;
        public Directions Faces;
    }

    void Start()
    {
        GenerateWorld();    
    }

    // create one block
    public void GenerateWorld()
    {
        var blocks = new List<BlockStruct>() {
            new BlockStruct { Type = TerrainTypes.Dirt, Faces = (Directions)15 },
            new BlockStruct { Type = TerrainTypes.Dirt, Faces = (Directions)15 }
        };

        var size = 0;

        // the size calculation
        foreach (var b in blocks)
            size += DetermineSize(b.Faces);

        var verticies = new Vector3[size];
        var normals = new Vector3[size];
        var uvs = new Vector2[size];
        var triangles = new int[(int)(1.5f * size)];
        var index = 0;
        var triIndex = 0;

        // here we create mesh components
        for (int i = 0; i < blocks.Count; i++)
            AddMeshComponents(ref index, ref triIndex, 
                ref verticies, ref normals, ref uvs, ref triangles,
                blocks[0], new Vector3(0, 0, i));

        // in this case we modify the same mesh
        var mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.normals = normals;

        // Uvs maps the texture over the surface
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();

        var cube = new GameObject("Cube");
        cube.transform.position = Vector3.zero;

        var renderer = cube.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = material;

        var meshFilter = (MeshFilter)cube.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;
    }

    int DetermineSize(Directions faces)
    {
        if (faces == 0) return 0;

        var size = 0;
        if (faces.HasFlag(Directions.Top)) size += 4;
        if (faces.HasFlag(Directions.Bottom)) size += 4;
        if (faces.HasFlag(Directions.Left)) size += 4;
        if (faces.HasFlag(Directions.Right)) size += 4;
        if (faces.HasFlag(Directions.Front)) size += 4;
        if (faces.HasFlag(Directions.Back)) size += 4;

        return size;
    }

    void AddMeshComponents(ref int index, ref int triIndex, 
        ref Vector3[] verticies, ref Vector3[] normals, ref Vector2[] uvs, ref int[] triangles,
        BlockStruct block, Vector3 offset)
    {
        int typeIndex = (int)block.Type;

        // all possible UVs
        Vector2 uv00 = _blockUVs[typeIndex, 0],
                uv10 = _blockUVs[typeIndex, 1],
                uv01 = _blockUVs[typeIndex, 2],
                uv11 = _blockUVs[typeIndex, 3];

        var faces = block.Faces;

        // no faces means no mesh neededs
        if (faces == 0) return;
        
        if (faces.HasFlag(Directions.Top))
        {
            AddQuadComponents(ref index,
                ref normals, Vector3.up,
                ref uvs, uv11, uv01, uv00, uv10,
                ref verticies, _p7, _p6, _p5, _p4, offset,
                ref triangles, ref triIndex);
        }

        if (faces.HasFlag(Directions.Bottom))
        {
            AddQuadComponents(ref index,
                ref normals, Vector3.down,
                ref uvs, uv11, uv01, uv00, uv10,
                ref verticies, _p0, _p1, _p2, _p3, offset,
                ref triangles, ref triIndex);
        }

        if (faces.HasFlag(Directions.Left))
        {
            AddQuadComponents(ref index,
                ref normals, Vector3.left,
                ref uvs, uv11, uv01, uv00, uv10,
                ref verticies, _p7, _p4, _p0, _p3, offset,
                ref triangles, ref triIndex);
        }

        if (faces.HasFlag(Directions.Right))
        {
            AddQuadComponents(ref index,
                ref normals, Vector3.right,
                ref uvs, uv11, uv01, uv00, uv10,
                ref verticies, _p5, _p6, _p2, _p1, offset,
                ref triangles, ref triIndex);
        }

        if (faces.HasFlag(Directions.Front))
        {
            AddQuadComponents(ref index,
                ref normals, Vector3.forward,
                ref uvs, uv11, uv01, uv00, uv10,
                ref verticies, _p4, _p5, _p1, _p0, offset,
                ref triangles, ref triIndex);
        }

        if (faces.HasFlag(Directions.Back))
        {
            AddQuadComponents(ref index,
                ref normals, Vector3.back,
                ref uvs, uv11, uv01, uv00, uv10,
                ref verticies, _p6, _p7, _p3, _p2, offset,
                ref triangles, ref triIndex);
        }
    }

    void AddQuadComponents(ref int index, ref Vector3[] normals, Vector3 normal,
            ref Vector2[] uvs, Vector2 uv11, Vector2 uv01, Vector2 uv00, Vector2 uv10,
            ref Vector3[] verticies, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p4, Vector3 offset,
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
        verticies[index] = p0 + offset;
        verticies[index + 1] = p1 + offset;
        verticies[index + 2] = p2 + offset;
        verticies[index + 3] = p4 + offset;
        
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