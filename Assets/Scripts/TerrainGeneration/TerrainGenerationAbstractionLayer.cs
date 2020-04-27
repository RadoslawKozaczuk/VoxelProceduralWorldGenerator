using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Voxels.Common;
using Voxels.Common.Interfaces;

namespace Voxels.TerrainGeneration
{
    /// <summary>
    /// Abstraction layer allows us to encapsulate the assembly's logic.
    /// Thanks to that we can, for example, hide the constructor and control the initialization from here.
    /// Which allows us to set internal parameters to readonly and control the re/initialization by reconstructing the object
    /// rather than by calling an arbitrate initialize method.
    /// Also this is the place when we can set the parameters etc.
    /// </summary>
    [DisallowMultipleComponent]
    public class TerrainGenerationAbstractionLayer : MonoBehaviour, INeedInitializeOnWorldSizeChange
    {
        static TerrainGenerator _terrainGenerator;

        public void InitializeOnWorldSizeChange() => _terrainGenerator = new TerrainGenerator();

        public static int3[] CalculateHeightsJobSystem() => _terrainGenerator.CalculateHeightsJobSystem();

        public static BlockType[] CalculateBlockTypes(int3[] heights) => _terrainGenerator.CalculateBlockTypes(heights);

        public static void CalculateBlockTypesParallel() => _terrainGenerator.CalculateBlockTypesParallel();

        public static void AddWater() => _terrainGenerator.AddWater();

        public static void AddTreesParallel() => _terrainGenerator.AddTreesParallel();
    }
}
