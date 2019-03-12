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

	// chunks & blocks
	public Chunk[,,] Chunks;
	public Block[,,] Blocks;
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
	public Vector3Int Coord;
	public Vector3Int Position;
	public GameObject Terrain;
	public GameObject Water;
	public ChunkStatus Status;
}