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
	public readonly Vector3Int Coord; // coordinates are only used for GameObject naming as of now
	public Vector3Int Position;
	public GameObject Terrain;
	public GameObject Water;
	public ChunkStatus Status;

	public Chunk(Vector3Int coord, Vector3Int position, ChunkStatus status)
	{
		Coord = coord;
		Position = position;
		Status = status;
	}

	public Chunk(Vector3Int coord, Vector3Int position)
	{
		Coord = coord;
		Position = position;
		Status = ChunkStatus.Created;
	}
}