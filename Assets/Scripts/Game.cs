using System;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    [SerializeField] Text _controlsLabel;
    [SerializeField] Slider _progressBar;
    [SerializeField] Text _progressText;
    [SerializeField] Text _description;
    [SerializeField] Image _crosshair;
    [SerializeField] Text _internalStatus;
    [SerializeField] World _world;
    [SerializeField] GameObject _player;
    [SerializeField] Camera _mainCamera;
    [SerializeField] GameState _gameState = GameState.NotInitialized;
    [SerializeField] KeyCode _saveKey = KeyCode.K;
    [SerializeField] KeyCode _loadKey = KeyCode.L;
    [SerializeField] Vector3 _playerStartPosition;

    void Start()
    {
        _crosshair.enabled = false;

        _controlsLabel.text = "Controls:" + Environment.NewLine
            + "Attack - LPM" + Environment.NewLine
            + "Build - RPM" + Environment.NewLine
            + $"Save Game - { _saveKey }" + Environment.NewLine
            + $"Load Game - { _loadKey }";
        Debug.Log("Waiting instructions...");

        _player.SetActive(false);

        _gameState = GameState.Starting;
        StartCoroutine(_world.GenerateWorld(true));
    }

    void Update()
    {
        var progress = Mathf.Clamp01(_world.AlreadyGenerated / (_world.ChunkObjectsToGenerate + _world.ChunkTerrainToGenerate));
        _progressBar.value = progress;
        _progressText.text = Mathf.RoundToInt(progress * 100) + "%";
        _description.text = $"Created objects { _world.AlreadyGenerated } "
            + $"out of { _world.ChunkObjectsToGenerate + _world.ChunkTerrainToGenerate }";
        _internalStatus.text = "Game Status: " + Enum.GetName(_gameState.GetType(), _gameState) + Environment.NewLine
            + "Generator Status: " + Enum.GetName(_world.Status.GetType(), _world.Status);

        HandleInput();

        if (_world.Status == WorldGeneratorStatus.TerrainReady && _gameState == GameState.Starting)
            StartCoroutine(_world.GenerateMeshes());

        if (_world.Status == WorldGeneratorStatus.TerrainReady && _gameState == GameState.ReStarting)
            StartCoroutine(_world.RedrawChunksIfNecessaryAsync());

        if (_world.Status == WorldGeneratorStatus.AllReady)
        {
            _crosshair.enabled = true;

            if (_gameState == GameState.Starting)
            {
                _gameState = GameState.Started;
                CreatePlayer();
            }

            _player.SetActive(true);
            _world.RedrawChunksIfNecessary();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(_saveKey))
        {
            var storage = new PersistentStorage(_world.ChunkSize);

            var t = _player.transform;

            var playerRotation = new Vector3(
                t.GetChild(0).gameObject.transform.eulerAngles.x,
                t.rotation.eulerAngles.y,
                0);

            storage.SaveGame(t.position, playerRotation, _world);
            Debug.Log("Game Saved");
        }
        else if (Input.GetKeyDown(_loadKey))
        {
            _gameState = GameState.ReStarting;
            _player.SetActive(false);
            var storage = new PersistentStorage(_world.ChunkSize);

            var save = storage.LoadGame();
            _world.ChunkSize = save.ChunkSize;
            _world.WorldSizeX = save.WorldSizeX;
            _world.WorldSizeY = save.WorldSizeY;
            _world.WorldSizeZ = save.WorldSizeZ;

            CreatePlayer(save.PlayerPosition, save.PlayerRotation);

            StartCoroutine(_world.LoadWorld(save, false));
        }
    }

    public void ProcessBlockHit(Vector3 hitBlock)
    {
        int chunkX, chunkY, chunkZ, blockX, blockY, blockZ;

        FindChunkAndBlock(hitBlock, out chunkX, out chunkY, out chunkZ, out blockX, out blockY, out blockZ);

        // inform chunk
        var c = _world.Chunks[chunkX, chunkY, chunkZ];

        // was block destroyed
        if (BlockHit(blockX, blockY, blockZ, c))
            CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
    }

    /// <summary>
    /// Returns true if the block has been destroyed.
    /// </summary>
    bool BlockHit(int x, int y, int z, Chunk c)
    {
        var retVal = false;

        byte previousHpLevel = c.Blocks[x, y, z].HealthLevel;
        c.Blocks[x, y, z].Hp--;
        byte currentHpLevel = CalculateHealthLevel(
            c.Blocks[x, y, z].Hp,
            LookupTables.BlockHealthMax[(int)c.Blocks[x, y, z].Type]);

        if (currentHpLevel != previousHpLevel)
        {
            c.Blocks[x, y, z].HealthLevel = currentHpLevel;

            if (c.Blocks[x, y, z].Hp == 0)
            {
                c.Blocks[x, y, z].Type = BlockTypes.Air;
                c.Status = ChunkStatus.NeedToBeRecreated;
                retVal = true;
            }
            else
            {
                c.Status = ChunkStatus.NeedToBeRedrawn;
            }
        }

        return retVal;
    }

    byte CalculateHealthLevel(int hp, int maxHp)
    {
        float proportion = (float)hp / maxHp; // 0.625f

        // TODO: this require information from MeshGenerator which breaks the encapsulation rule
        float step = (float)1 / 11; // _crackUVs.Length; // 0.09f
        float value = proportion / step; // 6.94f
        int level = Mathf.RoundToInt(value); // 7

        return (byte)(11 - level); // array is in reverse order so we subtract our value from 1
    }

    public void ProcessBuildBlock(Vector3 hitBlock, BlockTypes type)
    {
        int chunkX, chunkY, chunkZ, blockX, blockY, blockZ;

        FindChunkAndBlock(hitBlock, out chunkX, out chunkY, out chunkZ, out blockX, out blockY, out blockZ);

        // inform chunk
        var c = _world.Chunks[chunkX, chunkY, chunkZ];

        // was block built
        if (BuildBlock(blockX, blockY, blockZ, type, c))
            CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
    }

    /// <summary>
    /// Returns true if a new block has been built.
    /// </summary>
    bool BuildBlock(int x, int y, int z, BlockTypes type, Chunk c)
    {
        if (c.Blocks[x, y, z].Type != BlockTypes.Air) return false;

        c.Blocks[x, y, z].Type = type;
        c.Blocks[x, y, z].Hp = LookupTables.BlockHealthMax[(int)type];
        c.Blocks[x, y, z].HealthLevel = 0;

        c.Status = ChunkStatus.NeedToBeRecreated;

        return true;
    }

    void FindChunkAndBlock(Vector3 hitBlock,
        out int chunkX, out int chunkY, out int chunkZ,
        out int blockX, out int blockY, out int blockZ)
    {
        chunkX = hitBlock.x < 0 ? 0 : (int)(hitBlock.x / _world.ChunkSize);
        chunkY = hitBlock.y < 0 ? 0 : (int)(hitBlock.y / _world.ChunkSize);
        chunkZ = hitBlock.z < 0 ? 0 : (int)(hitBlock.z / _world.ChunkSize);

        blockX = (int)hitBlock.x - chunkX * _world.ChunkSize;
        blockY = (int)hitBlock.y - chunkY * _world.ChunkSize;
        blockZ = (int)hitBlock.z - chunkZ * _world.ChunkSize;
    }

    /// <summary>
    /// Check if neighboring chunks need to be redrawn and change their status if necessary.
    /// </summary>
    void CheckNeighboringChunks(int blockX, int blockY, int blockZ, int chunkX, int chunkY, int chunkZ)
    {
        // right check
        if (blockX == _world.ChunkSize - 1 && chunkX + 1 < _world.WorldSizeX)
            _world.Chunks[chunkX + 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

        // BUG: there are sometimes faces not beinf rendered on this axis - dunno why
        // left check
        if (blockX == 0 && chunkX - 1 >= 0)
            _world.Chunks[chunkX - 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

        // top check
        if (blockY == _world.ChunkSize - 1 && chunkY + 1 < _world.WorldSizeY)
            _world.Chunks[chunkX, chunkY + 1, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

        // bottom check
        if (blockY == 0 && chunkY - 1 >= 0)
            _world.Chunks[chunkX, chunkY - 1, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

        // front check
        if (blockZ == _world.ChunkSize - 1 && chunkZ + 1 < _world.WorldSizeZ)
            _world.Chunks[chunkX, chunkY, chunkZ + 1].Status = ChunkStatus.NeedToBeRecreated;

        // back check
        if (blockZ == 0 && chunkZ - 1 >= 0)
            _world.Chunks[chunkX, chunkY, chunkZ - 1].Status = ChunkStatus.NeedToBeRecreated;
    }

    void CreatePlayer(Vector3? position = null, Vector3? rotation = null)
    {
        var playerPos = position ?? _playerStartPosition;
        _player.transform.position = new Vector3(
                playerPos.x,
                TerrainGenerator.GenerateDirtHeight(playerPos.x, playerPos.z) + 2,
                playerPos.z);

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
