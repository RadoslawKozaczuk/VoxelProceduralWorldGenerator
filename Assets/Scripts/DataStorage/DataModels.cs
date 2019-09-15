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
	public ChunkData[,,] Chunks;
	public BlockData[,,] Blocks;
}

public struct BlockData
{
	public Cubeside Faces;
	public BlockType Type;
	public byte Hp;
	public byte HealthLevel; // corresponds to the visible crack appearance texture
}

/// <summary>
/// Stores value type data.
/// </summary>
public struct ChunkData
{
	public readonly Vector3Int Coord;
	public Vector3Int Position;
	public ChunkStatus Status;

	public ChunkData(Vector3Int coord, Vector3Int position, ChunkStatus status)
	{
		Coord = coord;
		Position = position;
		Status = status;
	}

	public ChunkData(Vector3Int coord, Vector3Int position)
	{
		Coord = coord;
		Position = position;
		Status = ChunkStatus.NotReady;
	}
}

/// <summary>
/// Stores game objects.
/// </summary>
public class ChunkObject
{
	public GameObject Terrain;
	public GameObject Water;
}