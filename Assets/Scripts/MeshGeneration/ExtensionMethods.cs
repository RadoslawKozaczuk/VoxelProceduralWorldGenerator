using System.Runtime.CompilerServices;
using UnityEngine;
using Voxels.MeshGeneration.DataModels;

namespace Voxels.MeshGeneration
{
    public static class ExtensionMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 Add(this Vector3 v1, in ReadonlyVector3Int v2) => new Vector3(v1.x + v2.X, v1.y + v2.Y, v1.z + v2.Z);
    }
}
