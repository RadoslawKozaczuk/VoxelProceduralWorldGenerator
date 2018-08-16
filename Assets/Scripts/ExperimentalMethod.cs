using UnityEngine;
using System;

public class ExperimentalMethod : MonoBehaviour
{
    public Material material;

    [Flags]
    enum Directions : byte { Top = 1, Bottom = 2, Left = 4, Right = 8, Front = 16, Back = 32 }
    enum TerrainTypes : byte {
        Dirt, Stone, Diamond, Bedrock, Redstone, Sand, Leaves, Wood, Woodbase,
        Water,
        Grass,
        Air
    }

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
        //GenerateWorld();    
    }

    // create one block
    public void GenerateWorld()
    {
        // create world
        // create a block
        var block = new BlockStruct() { Type = TerrainTypes.Dirt, Faces = (Directions)22 };
        DrawCube(block);
        // render the shit out of it
    }
    
    void DrawCube(BlockStruct block)
    {
        int typeIndex = (int)block.Type;

        // all possible UVs
        Vector2 uv00 = _blockUVs[typeIndex, 0],
                uv10 = _blockUVs[typeIndex, 1],
                uv01 = _blockUVs[typeIndex, 2],
                uv11 = _blockUVs[typeIndex, 3];
        
        // Normals are vectors projected from the polygon (triangle) at the angle of 90 degrees,
        // they tell the engine which side it should treat as the side on which textures and shaders should be rendered.
        // Verticies also can have their own normals and this is the case here so each vertex has its own normal vector.
        var mesh = new Mesh();

        var faces = block.Faces;

        // no faces means no mesh neededs
        if (faces == 0) return;

        var size = 0;
        if (faces.HasFlag(Directions.Top))      size += 4;
        if (faces.HasFlag(Directions.Bottom))   size += 4;
        if (faces.HasFlag(Directions.Left))     size += 4;
        if (faces.HasFlag(Directions.Right))    size += 4;
        if (faces.HasFlag(Directions.Front))    size += 4;
        if (faces.HasFlag(Directions.Back))     size += 4;
        
        var verticies = new Vector3[size];
        var normals = new Vector3[size];
        var uvs = new Vector2[size];
        var triangles = new int[(int)(1.5f * size)];
        var index = 0;
        var triIndex = 0;

        if (faces.HasFlag(Directions.Top))
        {
            CalculateTriangles(ref triangles, index, ref triIndex);

            verticies[index] = _p7;
            normals[index] = Vector3.up;
            uvs[index] = uv11;
            index++;

            verticies[index] = _p6;
            normals[index] = Vector3.up;
            uvs[index] = uv01;
            index++;

            verticies[index] = _p5;
            normals[index] = Vector3.up;
            uvs[index] = uv00;
            index++;

            verticies[index] = _p4;
            normals[index] = Vector3.up;
            uvs[index] = uv10;
            index++;
        }

        if (faces.HasFlag(Directions.Bottom))
        {
            CalculateTriangles(ref triangles, index, ref triIndex);

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
        
        if (faces.HasFlag(Directions.Left))
        {
            CalculateTriangles(ref triangles, index, ref triIndex);

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

        if (faces.HasFlag(Directions.Right))
        {
            CalculateTriangles(ref triangles, index, ref triIndex);

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

        if (faces.HasFlag(Directions.Front))
        {
            CalculateTriangles(ref triangles, index, ref triIndex);

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

        if (faces.HasFlag(Directions.Back))
        {
            CalculateTriangles(ref triangles, index, ref triIndex);

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

        mesh.RecalculateBounds();

        var cube = new GameObject("Cube");
        cube.transform.position = Vector3.zero;

        var renderer = cube.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = material;

        var meshFilter = (MeshFilter)cube.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;
    }

    //[AgressiveInlining]
    private void CalculateTriangles(ref int[] triangles, int index, ref int triIndex)
    {
        triangles[triIndex++] = index + 3;
        triangles[triIndex++] = index + 1;
        triangles[triIndex++] = index;
        triangles[triIndex++] = index + 3;
        triangles[triIndex++] = index + 2;
        triangles[triIndex++] = index + 1;
    }
}