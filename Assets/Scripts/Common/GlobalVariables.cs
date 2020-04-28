using UnityEngine;
using Voxels.Common.DataModels;

namespace Voxels.Common
{
    public class GlobalVariables : MonoBehaviour
    {
        public static BlockData[,,] Blocks;
        public static ChunkData[,,] Chunks;
        public static GameSettings Settings;
    }
}
