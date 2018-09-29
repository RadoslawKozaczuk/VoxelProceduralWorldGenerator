using UnityEngine;
using System;

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

public struct BlockData
{
    public Cubesides Faces;
    public BlockTypes Type;
    //public byte CurrentHealth;
    //public HealthLevel HealthType;
}

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

public struct ChunkData
{
    public BlockData[,,] Blocks;
}