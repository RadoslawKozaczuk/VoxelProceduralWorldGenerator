using UnityEngine;
using Voxels.Common.DataModels;

namespace Voxels.Common
{
    public class GlobalVariables : MonoBehaviour
    {
        // I need to find a way to make it readonly
        public static BlockData[,,] Blocks;

        public static GameSettings Settings;

        public static ChunkData[,,] Chunks;

        public static int TotalBlockNumberX, TotalBlockNumberY, TotalBlockNumberZ;

    }
}
