public readonly struct LookupTables
{
	// this corresponds to the BlockType enum
	public static readonly byte[] BlockHealthMax = {
    	byte.MaxValue, // air
        3, 4, 4, byte.MaxValue, 4, 3, 3, 3, 3,
		8, // water
    	3  // grass
    }; // byte.MaxValue means the block cannot be destroyed
}
