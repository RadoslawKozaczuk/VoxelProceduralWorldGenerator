using UnityEngine;

namespace Voxels.Common.DataModels
{
    public struct ChunkData
    {
        /// <summary>
        /// This is chunk ID, for example <1, 0 , 1>.
        /// </summary>
        public readonly ReadonlyVector3Int Coord;
        public readonly ReadonlyVector3Int Position;
        public ChunkStatus Status;

        public ChunkData(ReadonlyVector3Int coord, ReadonlyVector3Int position, ChunkStatus status)
        {
            Coord = coord;
            Position = position;
            Status = status;
        }

        public ChunkData(ReadonlyVector3Int coord, ReadonlyVector3Int position)
        {
            Coord = coord;
            Position = position;
            Status = ChunkStatus.NotReady;
        }
    }
}
