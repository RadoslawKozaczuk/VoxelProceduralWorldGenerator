using UnityEngine;

public class SaveGameData
{
    // player data
    public Vector3 PlayerPosition;
    public Vector3 PlayerRotation;

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
    public byte Hp;
    public byte HealthLevel; // corresponds to the visible crack appearence texture
}

public struct ChunkData
{
    public BlockData[,,] Blocks;
}