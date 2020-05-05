using System;

namespace Voxels.Common
{
    // whenever you change these values you also have to change MeshGenerator constants
    public enum BlockType : byte
    {
        Air = 0, // air need to be the default value (zero) to allow additional performance optimizations
        Dirt = 1, Stone, Diamond, Bedrock, Redstone, Sand, Leaves, Wood, Woodbase,
        Water = 10,
        Grass = 11 // types that have different textures on sides and bottom (+10 is the shift value)
    }

    [Flags]
    public enum Cubeside : byte { Right = 1, Left = 2, Top = 4, Bottom = 8, Front = 16, Back = 32 }

    public enum ChunkStatus { NotReady, NeedToBeRedrawn, NeedToBeRecreated, Ready }

    public enum TreeProbability { None = 0, Some = 1, Lots = 2 }

    /// <summary>
    /// Specifies which parallelization method is used by the <see cref="TerrainGenerator"/>.
    /// </summary>
    public enum ComputingAccelerationMethod
    {
        /// <summary>
        /// All calculation done on the main thread.
        /// </summary>
        None,
        /// <summary>
        /// TODO: description
        /// </summary>
        PureCSParallelisation,
        /// <summary>
        /// Unity Job System plus Burst Compiler.
        /// </summary>
        UnityJobSystem,
        /// <summary>
        /// Unity Job System and Burst Compiler plus static array allocation trick from the unsafe context.
        /// The trick allow us to stop the calculation as soon as we reach the air.
        /// </summary>
        UnityJobSystemPlusStaticArray,
        /// <summary>
        /// TODO: description
        /// </summary>
        ComputeShader,
        /// <summary>
        /// Utilizes Entity Component System
        /// </summary>
        ECS
    }
}
