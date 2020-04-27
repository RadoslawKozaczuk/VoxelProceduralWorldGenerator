using UnityEngine;
using Voxels.Common.DataModels;

namespace Voxels.Common
{
    public class GlobalVariables : MonoBehaviour
    {
        public static BlockData[,,] Blocks;
        public static GameSettings Settings;
        public static ChunkData[,,] Chunks;
    }
}
