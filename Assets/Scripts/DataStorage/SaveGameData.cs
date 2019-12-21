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