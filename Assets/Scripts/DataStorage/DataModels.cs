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
    public Chunk[,,] Chunks;
}

public struct Block
{
    public Cubesides Faces;
    public BlockTypes Type;
    public byte Hp;
    public byte HealthLevel; // corresponds to the visible crack appearence texture
}

public class Chunk
{
    public Block[,,] Blocks;
    public Vector3Int Coord;
    public GameObject Terrain;
    public GameObject Water;
    public ChunkStatus Status;
}