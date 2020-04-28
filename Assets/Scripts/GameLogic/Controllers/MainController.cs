using System.Collections.Generic;
using UnityEngine;
using Voxels.Common.Interfaces;
using Voxels.MeshGeneration;
using Voxels.TerrainGeneration;

namespace Voxels.GameLogic.Controllers
{
    /// <summary>
    /// Responsible for reinitializations.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainController : MonoBehaviour
    {
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [SerializeField] TerrainGenerationAbstractionLayer _terrainGenerationAbstractionLayer;
        [SerializeField] MeshGenerationAbstractionLayer _meshGenerationAbstractionLayer;
#pragma warning restore CS0649

        static readonly List<INeedInitializeOnWorldSizeChange> _list = new List<INeedInitializeOnWorldSizeChange>();

        void Awake()
        {
            _list.Add(_terrainGenerationAbstractionLayer);
            _list.Add(_meshGenerationAbstractionLayer);
        }

        public static void InitializeOnWorldSizeChange() => _list.ForEach(x => x.InitializeOnWorldSizeChange());
    }
}
