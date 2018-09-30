using UnityEngine;

public class LookupTables : MonoBehaviour
{
    // this corresponds to the BlockType enum
    public static readonly byte[] BlockHealthMax = {
            3, 4, 4, byte.MaxValue, 4, 3, 3, 3, 3,
            8, // water
			3, // grass
			byte.MaxValue  // air
		}; // byte.MaxValue means the block cannot be destroyed
}
