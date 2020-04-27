namespace Voxels.MeshGenerator.DataModels
{
    internal readonly struct ReadonlyVector3Int
    {
        internal readonly int X;
        internal readonly int Y;
        internal readonly int Z;

        internal ReadonlyVector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
