using System;
using UnityEngine;
using UnityEngine.UI;
using Voxels.Common;
using Voxels.Common.DataModels;
using Voxels.GameLogic.PlayerController;
using Voxels.TerrainGeneration;
using Voxels.SaveLoad;
using Voxels.UI;

namespace Voxels.GameLogic
{
    public class Game : MonoBehaviour
    {
        // start variables
        public static bool StartFromLoadGame; // true - game started from load game, false - game started from new game

#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [SerializeField] Text _controlsLabel;
        [SerializeField] Slider _progressBar;
        [SerializeField] Text _progressText;
        [SerializeField] Text _description;
        [SerializeField] Image _crosshair;
        [SerializeField] Text _internalStatus;
        [SerializeField] World _world;
        [SerializeField] GameObject _player;
        [SerializeField] Camera _mainCamera;
        [SerializeField] TextReveal _topMessage;
        [SerializeField] KeyCode _saveKey = KeyCode.K;
        [SerializeField] KeyCode _loadKey = KeyCode.L;
        [SerializeField] Vector3 _playerStartPosition;
#pragma warning restore CS0649

        void Start()
        {
            _crosshair.enabled = false;

            _controlsLabel.text = "Controls:" + Environment.NewLine
                + "Attack - LPM" + Environment.NewLine
                + "Build - RPM" + Environment.NewLine
                + $"Save Game - { _saveKey }" + Environment.NewLine
                + $"Load Game - { _loadKey }";

            _player.SetActive(false);

            _topMessage.HideMessage();

            if (StartFromLoadGame)
            {
                _topMessage.HideMessage();
                StartCoroutine(_world.LoadWorld(true, () =>
                {
                    CreatePlayer(_world.PlayerLoadedPosition, _world.PlayerLoadedRotation);
                    _player.SetActive(true);
                    _crosshair.enabled = true;
                    _topMessage.ShowNewMessage("Game Loaded Successfully");
                    _mainCamera.gameObject.SetActive(false);
                }));
            }
            else
            {
                _topMessage.HideMessage();
                StartCoroutine(_world.CreateWorld(true, () =>
                {
                    CreatePlayer();
                    _player.SetActive(true);
                    _crosshair.enabled = true;
                    _topMessage.ShowNewMessage("New World Created Successfully");
                    _mainCamera.gameObject.SetActive(false);
                }));
            }
        }

        void Update()
        {
            // this should be done only when it is necessary not on every frame
            // and its should be moved to a separate folder
            var progress = Mathf.Clamp01(_world.AlreadyGenerated / (_world.MeshProgressSteps + _world.TerrainProgressSteps));
            _progressBar.value = progress;
            _progressText.text = Mathf.RoundToInt(progress * 100) + "%";
            _description.text = _world.ProgressDescription;
            _internalStatus.text = "Generator Status: " + Enum.GetName(_world.Status.GetType(), _world.Status);

            if (_world.Status == WorldGeneratorStatus.FacesReady || _world.Status == WorldGeneratorStatus.AllReady)
            {
                _world.RedrawChunksIfNecessary();
                HandleInput();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();
        }

        public void ProcessBlockHit(Vector3 hitBlock)
        {
            FindChunkAndBlock(hitBlock, out int chunkX, out int chunkY, out int chunkZ, out int blockX, out int blockY, out int blockZ);

            // inform chunk
            ref ChunkData chunk = ref GlobalVariables.Chunks[chunkX, chunkY, chunkZ];

            // a block has been destroyed
            if (_world.BlockHit(blockX, blockY, blockZ, ref chunk))
                CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
        }

        public void ProcessBuildBlock(Vector3 hitBlock, BlockType type)
        {
            FindChunkAndBlock(hitBlock, out int chunkX, out int chunkY, out int chunkZ, out int blockX, out int blockY, out int blockZ);

            // inform chunk
            ref ChunkData chunk = ref GlobalVariables.Chunks[chunkX, chunkY, chunkZ];

            // a block has been built
            if (_world.BuildBlock(blockX, blockY, blockZ, type, ref chunk))
                CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
        }

        void HandleInput()
        {
            if (Input.GetKeyDown(_saveKey))
            {
                _topMessage.HideMessage();
                Transform t = _player.transform;

                var playerRotation = new Vector3(
                    t.GetChild(0).gameObject.transform.eulerAngles.x,
                    t.rotation.eulerAngles.y,
                    0);

                SaveLoadController.SaveGame(t.position, playerRotation);
                _topMessage.ShowNewMessage("Game Saved Successfully");
                Debug.Log("Game Saved");
            }
            else if (Input.GetKeyDown(_loadKey))
            {
                _topMessage.HideMessage();
                _player.SetActive(false);
                _crosshair.enabled = false;
                _mainCamera.gameObject.SetActive(true);

                StartCoroutine(_world.LoadWorld(false, () =>
                {
                    _mainCamera.gameObject.SetActive(false);
                    CreatePlayer(_world.PlayerLoadedPosition, _world.PlayerLoadedRotation);
                    _player.SetActive(true);
                    _crosshair.enabled = true;
                    _topMessage.ShowNewMessage("Game Loaded Successfully");
                }));
            }
        }

        void FindChunkAndBlock(Vector3 hitBlock,
            out int chunkX, out int chunkY, out int chunkZ,
            out int blockX, out int blockY, out int blockZ)
        {
            chunkX = hitBlock.x < 0 ? 0 : (int)(hitBlock.x / Constants.CHUNK_SIZE);
            chunkY = hitBlock.y < 0 ? 0 : (int)(hitBlock.y / Constants.CHUNK_SIZE);
            chunkZ = hitBlock.z < 0 ? 0 : (int)(hitBlock.z / Constants.CHUNK_SIZE);

            blockX = (int)hitBlock.x;
            blockY = (int)hitBlock.y;
            blockZ = (int)hitBlock.z;
        }

        /// <summary>
        /// Check if neighboring chunks need to be redrawn and change their status if necessary.
        /// </summary>
        void CheckNeighboringChunks(int blockX, int blockY, int blockZ, int chunkX, int chunkY, int chunkZ)
        {
            // right check
            if (blockX == Constants.CHUNK_SIZE - 1 && chunkX + 1 < GlobalVariables.Settings.WorldSizeX)
                GlobalVariables.Chunks[chunkX + 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

            // left check
            if (blockX == 0 && chunkX - 1 >= 0)
                GlobalVariables.Chunks[chunkX - 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

            // top check
            if (blockY == Constants.CHUNK_SIZE - 1 && chunkY + 1 < Constants.WORLD_SIZE_Y)
                GlobalVariables.Chunks[chunkX, chunkY + 1, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

            // bottom check
            if (blockY == 0 && chunkY - 1 >= 0)
                GlobalVariables.Chunks[chunkX, chunkY - 1, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

            // front check
            if (blockZ == Constants.CHUNK_SIZE - 1 && chunkZ + 1 < GlobalVariables.Settings.WorldSizeZ)
                GlobalVariables.Chunks[chunkX, chunkY, chunkZ + 1].Status = ChunkStatus.NeedToBeRecreated;

            // back check
            if (blockZ == 0 && chunkZ - 1 >= 0)
                GlobalVariables.Chunks[chunkX, chunkY, chunkZ - 1].Status = ChunkStatus.NeedToBeRecreated;
        }

        void CreatePlayer(Vector3? position = null, Vector3? rotation = null)
        {
            Vector3 playerPos = position ?? _playerStartPosition;
            ReadonlyVector3Int heights = TerrainGenerationAbstractionLayer.CalculateHeights(
                GlobalVariables.Settings.SeedValue, (int)playerPos.x, (int)playerPos.z);

            int maxY = Math.Max(heights.Y, heights.Z); // y = stone, z = dirt

            _player.transform.position = new Vector3(playerPos.x, maxY + 2, playerPos.z);

            var fpc = _player.GetComponent<FirstPersonController>();
            if (rotation.HasValue)
            {
                var r = rotation.Value;
                fpc.MouseLook.CharacterTargetRot = Quaternion.Euler(0f, r.y, 0f);
                fpc.MouseLook.CameraTargetRot = Quaternion.Euler(r.x, 0f, 0f);
            }
            else
            {
                fpc.MouseLook.CharacterTargetRot = Quaternion.Euler(0f, 0f, 0f);
                fpc.MouseLook.CameraTargetRot = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }
}