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

    [SerializeField] World _world;
    public GameObject Player;
    public Camera MainCamera;
    public GameState GameState = GameState.NotInitialized;
    public KeyCode SaveKey = KeyCode.K;
    public KeyCode LoadKey = KeyCode.L;

    public Vector3 PlayerStartPosition;
    
    void Start()
    {
        _crosshair.enabled = false;

        _controlsLabel.text = "Controls:" + Environment.NewLine
            + "Attack - LPM" + Environment.NewLine
            + "Build - RPM" + Environment.NewLine
            + $"Save Game - { SaveKey }" + Environment.NewLine
            + $"Load Game - { LoadKey }";
        Debug.Log("Waiting instructions...");

        Player.SetActive(false);

        GameState = GameState.Starting;
        StartCoroutine(_world.GenerateWorld(firstRun: true));
    }

    void Update()
    {
        var progress = Mathf.Clamp01(_world.AlreadyGenerated / (_world.ChunkObjectsToGenerate + _world.ChunkTerrainToGenerate));
        _progressBar.value = progress;
        _progressText.text = Mathf.RoundToInt(progress * 100) + "%";
        _description.text = $"Created objects { _world.AlreadyGenerated } "
            + $"out of { _world.ChunkObjectsToGenerate + _world.ChunkTerrainToGenerate }";

        HandleInput();

        if (_world.Status == WorldGeneratorStatus.Ready)
        {
            _crosshair.enabled = true;

            if (GameState == GameState.Starting)
            {
                GameState = GameState.StartingReady;
                CreatePlayer();
            }

            Player.SetActive(true);
            RedrawChunksIfNecessary();
        }
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(SaveKey))
        {
            var storage = new PersistentStorage(_world.ChunkSize);

            var t = Player.transform;

            var playerRotation = new Vector3(
                t.GetChild(0).gameObject.transform.eulerAngles.x,
                t.rotation.eulerAngles.y,
                0);
            
            storage.SaveGame(t.position, playerRotation, _world);
            Debug.Log("Game Saved");
        }
        else if (Input.GetKeyDown(LoadKey))
        {
            Player.SetActive(false);
            GameState = GameState.Loading;
            var storage = new PersistentStorage(_world.ChunkSize);

            var save = storage.LoadGame();
            _world.ChunkSize = save.ChunkSize;
            _world.WorldSizeX = save.WorldSizeX;
            _world.WorldSizeY = save.WorldSizeY;
            _world.WorldSizeZ = save.WorldSizeZ;

            CreatePlayer(save.PlayerPosition, save.PlayerRotation);

            StartCoroutine(_world.GenerateWorld(save: save));
        }
    }

    void RedrawChunksIfNecessary()
    {
        for (int x = 0; x < _world.WorldSizeX; x++)
            for (int z = 0; z < _world.WorldSizeZ; z++)
                for (int y = 0; y < _world.WorldSizeY; y++)
                {
                    Chunk c = _world.Chunks[x, y, z];
                    if (c.Status == ChunkStatus.NeedToBeRecreated)
                        RecreateMeshAndCollider(c);
                    else if (c.Status == ChunkStatus.NeedToBeRedrawn) // used only for cracks
                        RecreateTerrainMesh(c);
                }
    }

    void RecreateMeshAndCollider(Chunk c)
    {
        DestroyImmediate(c.Terrain.GetComponent<Collider>());
        
        MeshData t, w;
        _world.MeshGenerator.ExtractMeshData(ref c.Blocks, c.Coord, out t, out w);
        var tm = _world.MeshGenerator.CreateMesh(t);
        var wm = _world.MeshGenerator.CreateMesh(w);

        var terrainFilter = c.Terrain.GetComponent<MeshFilter>();
        terrainFilter.mesh = tm;
        var collider = c.Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        collider.sharedMesh = tm;

        var waterFilter = c.Water.GetComponent<MeshFilter>();
        waterFilter.mesh = wm;

        c.Status = ChunkStatus.Created;
    }

    /// <summary>
    /// Destroys terrain mesh and recreates it.
    /// Used for cracks as they do not change the terrain geometry.
    /// </summary>
    public void RecreateTerrainMesh(Chunk c)
    {
        MeshData t, w;
        _world.MeshGenerator.ExtractMeshData(ref c.Blocks, c.Coord, out t, out w);
        var tm = _world.MeshGenerator.CreateMesh(t);

        var meshFilter = c.Terrain.GetComponent<MeshFilter>();
        meshFilter.mesh = tm;

        c.Status = ChunkStatus.Created;
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
        var playerPos = position ?? PlayerStartPosition;
        Player.transform.position = new Vector3(
                playerPos.x,
                TerrainGenerator.GenerateDirtHeight(playerPos.x, playerPos.z) + 2,
                playerPos.z);

        var fpc = Player.GetComponent<FirstPersonController>();
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
