namespace Voxels.MeshGenerator.DataModels
{
    public readonly struct ReadonlyVector3Int
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public ReadonlyVector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
