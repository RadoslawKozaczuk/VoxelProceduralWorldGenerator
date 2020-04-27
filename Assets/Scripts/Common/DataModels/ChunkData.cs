using UnityEngine;

namespace Voxels.Common.DataModels
{
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
}
