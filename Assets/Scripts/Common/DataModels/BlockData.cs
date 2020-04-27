namespace Voxels.Common.DataModels
{
    public struct BlockData
    {
        public Cubeside Faces;
        public BlockType Type;
        public byte Hp;
        public byte HealthLevel; // corresponds to the visible crack appearance texture
    }
}
