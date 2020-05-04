using System.Runtime.CompilerServices;
using UnityEngine;
using Voxels.Common.DataModels;
using Voxels.Common.Interfaces;

namespace Voxels.MeshGeneration
{
    /// <summary>
    /// Abstraction layer allows us to encapsulate the assembly's logic.
    /// Thanks to that we can, for example, hide the constructor and control the initialization from here.
    /// Which allows us to set internal parameters to readonly and control the re/initialization by reconstructing the object
    /// rather than by calling an arbitrate initialize method.
    /// Also this is the place when we can set the parameters etc.
    /// </summary>
    [DisallowMultipleComponent]
    public class MeshGenerationAbstractionLayer : MonoBehaviour, INeedInitializeOnWorldSizeChange
    {
        static MeshGenerator _meshGenerator;

        public void InitializeOnWorldSizeChange() => _meshGenerator = new MeshGenerator();

        public static void CalculateFaces() => _meshGenerator.CalculateFaces();

        public static void WorldBoundariesCheck() => _meshGenerator.WorldBoundariesCheck();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalculateMeshes(in ReadonlyVector3Int chunkPos, out Mesh terrain, out Mesh water)
            => _meshGenerator.CalculateMeshes(chunkPos, out terrain, out water);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecalculateFacesAfterBlockDestroy(int blockX, int blockY, int blockZ)
            => _meshGenerator.RecalculateFacesAfterBlockDestroy(blockX, blockY, blockZ);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecalculateFacesAfterBlockBuild(int blockX, int blockY, int blockZ)
            => _meshGenerator.RecalculateFacesAfterBlockBuild(blockX, blockY, blockZ);
    }
}
