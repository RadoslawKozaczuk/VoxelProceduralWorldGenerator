﻿using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voxels.Common;
using Voxels.Common.DataModels;
using Voxels.GameLogic.Controllers;
using Voxels.GameLogic.DataModels;
using Voxels.MeshGeneration;
using Voxels.SaveLoad;
using Voxels.TerrainGeneration;

namespace Voxels.GameLogic
{
    [CreateAssetMenu]
    public class World : ScriptableObject
    {
        const int TERRAIN_GENERATION_STEPS = 7;

        public readonly int TotalBlockNumberX, TotalBlockNumberY, TotalBlockNumberZ;

        public Vector3 PlayerLoadedRotation, PlayerLoadedPosition;
        public float TerrainProgressSteps;
        public float MeshProgressSteps;
        public float AlreadyGenerated;
        public string ProgressDescription;

        internal WorldGeneratorStatus Status;
        internal ChunkObject[,,] ChunkObjects;

#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [SerializeField] Material _terrainTexture;
        [SerializeField] Material _waterTexture;
#pragma warning restore CS0649

        readonly Stopwatch _stopwatch = new Stopwatch();

        Scene _worldScene;
        int _progressStep = 1;

        World()
        {
            TotalBlockNumberX = GlobalVariables.Settings.WorldSizeX * Constants.CHUNK_SIZE;
            TotalBlockNumberY = Constants.WORLD_SIZE_Y * Constants.CHUNK_SIZE;
            TotalBlockNumberZ = GlobalVariables.Settings.WorldSizeZ * Constants.CHUNK_SIZE;
        }

        /// <summary>
		/// Generates block types with hp and hp level.
		/// Chunks and their objects (if first run = true).
		/// And calculates faces.
		/// </summary>
		public IEnumerator CreateWorld(bool firstRun, Action callback)
        {
            MainController.InitializeOnWorldSizeChange();

            _stopwatch.Restart();
            ResetProgressBarVariables();

            ProgressDescription = "Initialization...";
            Status = WorldGeneratorStatus.NotReady;
            GlobalVariables.Blocks = new BlockData[
                GlobalVariables.Settings.WorldSizeX * Constants.CHUNK_SIZE,
                Constants.WORLD_SIZE_Y * Constants.CHUNK_SIZE, GlobalVariables.Settings.WorldSizeZ * Constants.CHUNK_SIZE];

            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "Calculating block types...";
            TerrainGenerationAbstractionLayer.CalculateBlockTypes();
            AlreadyGenerated += _progressStep;
            yield return null; // return control to update the UI

            if (GlobalVariables.Settings.IsWater)
            {
                ProgressDescription = "Generating water...";
                TerrainGenerationAbstractionLayer.AddWater();
            }
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "Generating trees...";
            TerrainGenerationAbstractionLayer.AddTrees();
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "Chunk data initialization...";
            if (firstRun)
                _worldScene = SceneManager.CreateScene(name);

            // chunkData need to be initialized earlier in order to allow main loop iterate over chunks before their meshes are ready
            // thanks to this we can display chunk as soon as they become ready 
            GlobalVariables.Chunks = new ChunkData[GlobalVariables.Settings.WorldSizeX, Constants.WORLD_SIZE_Y, GlobalVariables.Settings.WorldSizeZ];
            for (int x = 0; x < GlobalVariables.Settings.WorldSizeX; x++)
                for (int z = 0; z < GlobalVariables.Settings.WorldSizeZ; z++)
                    for (int y = 0; y < Constants.WORLD_SIZE_Y; y++)
                        GlobalVariables.Chunks[x, y, z] = new ChunkData(
                            coord: new ReadonlyVector3Int(x, y, z),
                            position: new ReadonlyVector3Int(x * Constants.CHUNK_SIZE, y * Constants.CHUNK_SIZE, z * Constants.CHUNK_SIZE));

            ChunkObjects = new ChunkObject[GlobalVariables.Settings.WorldSizeX, Constants.WORLD_SIZE_Y, GlobalVariables.Settings.WorldSizeZ];
            yield return null; // return control

            ProgressDescription = "Calculating faces...";
            MeshGenerationAbstractionLayer.CalculateFaces();
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "World boundaries check...";
            MeshGenerationAbstractionLayer.WorldBoundariesCheck();
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "Creating chunks...";
            // create chunk objects one by one 
            for (int x = 0; x < GlobalVariables.Settings.WorldSizeX; x++)
                for (int z = 0; z < GlobalVariables.Settings.WorldSizeZ; z++)
                    for (int y = 0; y < Constants.WORLD_SIZE_Y; y++)
                    {
                        ChunkData chunkData = GlobalVariables.Chunks[x, y, z];
                        MeshGenerationAbstractionLayer.CalculateMeshes(in chunkData.Position, out Mesh terrainMesh, out Mesh waterMesh);

                        if (firstRun)
                        {
                            var chunkObject = new ChunkObject();
                            CreateChunkGameObjects(ref chunkData, ref chunkObject, terrainMesh, waterMesh);
                            SceneManager.MoveGameObjectToScene(chunkObject.Terrain.gameObject, _worldScene);
                            SceneManager.MoveGameObjectToScene(chunkObject.Water.gameObject, _worldScene);
                            ChunkObjects[x, y, z] = chunkObject;
                        }
                        else
                        {
                            SetMeshesAndCollider(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh, waterMesh);
                        }

                        GlobalVariables.Chunks[x, y, z].Status = ChunkStatus.NeedToBeRedrawn;
                        AlreadyGenerated++;

                        yield return null; // return control
                    }

            Status = WorldGeneratorStatus.AllReady;
            AlreadyGenerated += _progressStep;

            _stopwatch.Stop();
            UnityEngine.Debug.Log($"It took {_stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond} ms to generate the world.");

            ProgressDescription = $"Generation Completed {_stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond} ms";
            callback?.Invoke();
        }

        /// <summary>
		/// Generates block types with hp and hp level.
		/// Chunks and their objects (if first run = true).
		/// And calculates faces.
		/// </summary>
		public IEnumerator LoadWorld(bool firstRun, Action callback)
        {
            MainController.InitializeOnWorldSizeChange();

            ResetProgressBarVariables();
            _stopwatch.Restart();

            ProgressDescription = "Loading data...";
            Status = WorldGeneratorStatus.NotReady;
            SaveLoadController.LoadGame(out PlayerLoadedPosition, out PlayerLoadedRotation);
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "Creating game objects...";
            if (firstRun)
                _worldScene = SceneManager.CreateScene(name);

            Status = WorldGeneratorStatus.TerrainReady;
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            if (firstRun)
            {
                ProgressDescription = "Chunk data initialization...";
                // chunkData need to be initialized earlier in order to allow main loop iterate over chunks before their meshes are ready
                // thanks to this we can display chunk as soon as they become ready 
                GlobalVariables.Chunks = new ChunkData[GlobalVariables.Settings.WorldSizeX, Constants.WORLD_SIZE_Y, GlobalVariables.Settings.WorldSizeZ];
                for (int x = 0; x < GlobalVariables.Settings.WorldSizeX; x++)
                    for (int z = 0; z < GlobalVariables.Settings.WorldSizeZ; z++)
                        for (int y = 0; y < Constants.WORLD_SIZE_Y; y++)
                            GlobalVariables.Chunks[x, y, z] = new ChunkData(
                                new ReadonlyVector3Int(x, y, z),
                                new ReadonlyVector3Int(x * Constants.CHUNK_SIZE, y * Constants.CHUNK_SIZE, z * Constants.CHUNK_SIZE));

                ChunkObjects = new ChunkObject[GlobalVariables.Settings.WorldSizeX, Constants.WORLD_SIZE_Y, GlobalVariables.Settings.WorldSizeZ];
                AlreadyGenerated += _progressStep;
                yield return null; // return control
            }

            ProgressDescription = "Calculating faces...";
            MeshGenerationAbstractionLayer.CalculateFaces();
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "World boundaries check...";
            MeshGenerationAbstractionLayer.WorldBoundariesCheck();
            Status = WorldGeneratorStatus.FacesReady;
            AlreadyGenerated += _progressStep;
            yield return null; // return control

            ProgressDescription = "Creating chunks...";
            // create chunk objects one by one 
            for (int x = 0; x < GlobalVariables.Settings.WorldSizeX; x++)
                for (int z = 0; z < GlobalVariables.Settings.WorldSizeZ; z++)
                    for (int y = 0; y < Constants.WORLD_SIZE_Y; y++)
                    {
                        ChunkData chunkData = GlobalVariables.Chunks[x, y, z];
                        MeshGenerationAbstractionLayer.CalculateMeshes(in chunkData.Position, out Mesh terrainMesh, out Mesh waterMesh);

                        if (firstRun)
                        {
                            var chunkObject = new ChunkObject();
                            CreateChunkGameObjects(ref chunkData, ref chunkObject, terrainMesh, waterMesh);
                            SceneManager.MoveGameObjectToScene(chunkObject.Terrain.gameObject, _worldScene);
                            SceneManager.MoveGameObjectToScene(chunkObject.Water.gameObject, _worldScene);
                            ChunkObjects[x, y, z] = chunkObject;
                        }
                        else
                        {
                            SetMeshesAndCollider(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh, waterMesh);
                        }

                        GlobalVariables.Chunks[x, y, z].Status = ChunkStatus.NeedToBeRedrawn;
                        AlreadyGenerated++;

                        yield return null; // return control
                    }

            Status = WorldGeneratorStatus.AllReady;

            _stopwatch.Stop();
            UnityEngine.Debug.Log($"It took {_stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond} ms to load all terrain.");

            AlreadyGenerated = TerrainProgressSteps + MeshProgressSteps; // hardcoded end indicator
            ProgressDescription = "Game Load Completed";

            callback?.Invoke();
        }

        /// <summary>
        /// Returns true if the block has been destroyed.
        /// </summary>
        internal bool BlockHit(int blockX, int blockY, int blockZ, ref ChunkData chunkData)
        {
            ref BlockData b = ref GlobalVariables.Blocks[blockX, blockY, blockZ];

            if (b.Type == BlockType.Air)
            {
                UnityEngine.Debug.LogError("Block of type Air was hit which should have never happened. Probably wrong block coordinates calculation.");
                return false;
            }

            if (--b.Hp == 0)
            {
                b.Type = BlockType.Air;
                MeshGenerationAbstractionLayer.RecalculateFacesAfterBlockDestroy(blockX, blockY, blockZ);
                chunkData.Status = ChunkStatus.NeedToBeRecreated;
                return true;
            }

            byte previousHpLevel = b.HealthLevel;
            b.HealthLevel = CalculateHealthLevel(b.Hp, LookupTables.BlockHealthMax[(int)b.Type]);

            if (b.HealthLevel != previousHpLevel)
                chunkData.Status = ChunkStatus.NeedToBeRedrawn;

            return false;
        }

        /// <summary>
        /// Returns true if a new block has been built.
        /// </summary>
        internal bool BuildBlock(int blockX, int blockY, int blockZ, BlockType type, ref ChunkData chunkData)
        {
            ref BlockData b = ref GlobalVariables.Blocks[blockX, blockY, blockZ];

            if (b.Type != BlockType.Air) return false;

            MeshGenerationAbstractionLayer.RecalculateFacesAfterBlockBuild(blockX, blockY, blockZ);

            b.Type = type;
            b.Hp = LookupTables.BlockHealthMax[(int)type];
            b.HealthLevel = 0;

            chunkData.Status = ChunkStatus.NeedToBeRecreated;

            return true;
        }

        internal void RedrawChunksIfNecessary()
        {
            for (int x = 0; x < GlobalVariables.Settings.WorldSizeX; x++)
                for (int z = 0; z < GlobalVariables.Settings.WorldSizeZ; z++)
                    for (int y = 0; y < Constants.WORLD_SIZE_Y; y++)
                    {
                        ref ChunkData chunkData = ref GlobalVariables.Chunks[x, y, z];

                        if (chunkData.Status == ChunkStatus.NotReady)
                            continue;
                        else if (chunkData.Status == ChunkStatus.NeedToBeRecreated)
                        {
                            MeshGenerationAbstractionLayer.CalculateMeshes(in chunkData.Position, out Mesh terrainMesh, out Mesh waterMesh);
                            SetMeshesAndCollider(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh, waterMesh);
                        }
                        else if (chunkData.Status == ChunkStatus.NeedToBeRedrawn) // used only for cracks
                        {
                            MeshGenerationAbstractionLayer.CalculateMeshes(in chunkData.Position, out Mesh terrainMesh, out _);
                            SetTerrainMesh(ref chunkData, ref ChunkObjects[x, y, z], terrainMesh);
                        }
                    }
        }

        void ResetProgressBarVariables()
        {
            // Unity editor remembers the state of the asset classes so these values have to reinitialized
            _progressStep = 1;
            AlreadyGenerated = 0;

            MeshProgressSteps = GlobalVariables.Settings.WorldSizeX * Constants.WORLD_SIZE_Y * GlobalVariables.Settings.WorldSizeZ;

            while (TERRAIN_GENERATION_STEPS * _progressStep * 2f < MeshProgressSteps)
                _progressStep++;

            TerrainProgressSteps = TERRAIN_GENERATION_STEPS * _progressStep;
        }

        byte CalculateHealthLevel(int hp, int maxHp)
        {
            float proportion = (float)hp / maxHp; // 0.625f

            // TODO: this requires information from MeshGenerator which breaks the encapsulation rule
            float step = (float)1 / 11; // _crackUVs.Length; // 0.09f
            float value = proportion / step; // 6.94f
            int level = Mathf.RoundToInt(value); // 7

            return (byte)(11 - level); // array is in reverse order so we subtract our value from 11
        }

        /// <summary>
        /// Sets both meshes and mesh mesh collider.
        /// </summary>
        void SetMeshesAndCollider(ref ChunkData chunkData, ref ChunkObject chunkObject, Mesh terrainMesh, Mesh waterMesh)
        {
            var terrainFilter = chunkObject.Terrain.GetComponent<MeshFilter>();
            terrainFilter.mesh = terrainMesh;
            chunkObject.Terrain.GetComponent<MeshCollider>().sharedMesh = terrainMesh;

            var waterFilter = chunkObject.Water.GetComponent<MeshFilter>();
            waterFilter.mesh = waterMesh;

            chunkData.Status = ChunkStatus.Ready;
        }

        /// <summary>
        /// Apply terrain mesh to the given chunk.
        /// </summary>
        void SetTerrainMesh(ref ChunkData chunkData, ref ChunkObject chunkObject, Mesh terrainMesh)
        {
            var meshFilter = chunkObject.Terrain.GetComponent<MeshFilter>();
            meshFilter.mesh = terrainMesh;

            chunkData.Status = ChunkStatus.Ready;
        }

        /// <summary>
        /// Creates chunk objects together with all necessary components.
        /// After that's done apply mesh and mesh collider.
        /// </summary>
		void CreateChunkGameObjects(ref ChunkData chunkData, ref ChunkObject chunkObject, Mesh terrainMesh, Mesh waterMesh)
        {
            string name = chunkData.Coord.X.ToString() + chunkData.Coord.Y + chunkData.Coord.Z;
            chunkObject.Terrain = new GameObject(name + "_terrain");
            chunkObject.Terrain.transform.position = new Vector3(chunkData.Position.X, chunkData.Position.Y, chunkData.Position.Z);
            chunkObject.Water = new GameObject(name + "_water");
            chunkObject.Water.transform.position = new Vector3(chunkData.Position.X, chunkData.Position.Y, chunkData.Position.Z);

            MeshRenderer mrT = chunkObject.Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            mrT.material = _terrainTexture;

            var mfT = (MeshFilter)chunkObject.Terrain.AddComponent(typeof(MeshFilter));
            mfT.mesh = terrainMesh;

            var mc = chunkObject.Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            mc.sharedMesh = terrainMesh;

            var mrW = chunkObject.Water.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            mrW.material = _waterTexture;

            var mfW = (MeshFilter)chunkObject.Water.AddComponent(typeof(MeshFilter));
            mfW.mesh = waterMesh;

            chunkObject.Terrain.gameObject.layer = 10;
            chunkObject.Water.gameObject.layer = 4;
        }
    }
}