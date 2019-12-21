using Assets.Scripts.World;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Scripts
{
    public static class ExtensionMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Add(this Vector3 v1, in ReadonlyVector3Int v2) => new Vector3(v1.x + v2.X, v1.y + v2.Y, v1.z + v2.Z);
    }
}
