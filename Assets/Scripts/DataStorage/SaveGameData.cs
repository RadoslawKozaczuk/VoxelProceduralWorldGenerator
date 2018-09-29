using UnityEngine;
using System;
using System.Collections.Generic;

public enum BlockTypes : byte
{
    Dirt, Stone, Diamond, Bedrock, Redstone, Sand, Leaves, Wood, Woodbase,
    Water,
    Grass, // types that have different textures on sides and bottom
    Air
}

[Flags]
public enum Cubesides : byte { Right = 1, Left = 2, Top = 4, Bottom = 8, Front = 16, Back = 32 }

public enum HealthLevels : byte { NoCrack, Crack1, Crack2, Crack3, Crack4 }

public class SaveGameData
{
    // player data
    public Vector3 Position;
    public Quaternion Rotation;

    // world data
    public byte ChunkSize;
    public byte WorldSizeX;
    public byte WorldSizeY;
    public byte WorldSizeZ;

    // chunks
    public ChunkData[,,] Chunks;
}

public struct BlockData
{
    public Cubesides Faces;
    public BlockTypes Type;
    //public byte CurrentHealth;
    //public HealthLevel HealthType;
}

public struct ChunkData
{
    public BlockData[,,] Blocks;
    //public MeshData TerrainData;
    //public MeshData WaterData;
}

public struct MeshData
{
    // keep the size of each collection
    //public int[] CollectionSizes;

    public Vector2[] Uvs;
    public List<Vector2> Suvs;
    public Vector3[] Verticies;
    public Vector3[] Normals;
    public int[] Triangles;
}