using System;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    [SerializeField] Text _controlsLabel;
    public World World;
    public GameObject Player;
    public Camera MainCamera;
    public GameState GameState = GameState.NotStarted;
    public KeyCode NewGameKey = KeyCode.N;
    public KeyCode SaveKey = KeyCode.S;
    public KeyCode LoadKey = KeyCode.L;
    public KeyCode ControlKey = KeyCode.LeftControl;

    public Vector3 PlayerStartPosition;
    public bool ActivatePlayer = true;

    void Start()
    {
        _controlsLabel.text = "Controls:" + Environment.NewLine
            + "Attack - LPM" + Environment.NewLine
            + "Build - RPM" + Environment.NewLine
            + $"Start a new game - { NewGameKey }" + Environment.NewLine
            + $"Save Game - { SaveKey }" + Environment.NewLine
            + $"Load Game - { LoadKey }";
        Debug.Log("Waiting instructions...");
    }

    void Update()
    {
        HandleInput();

        if(GameState == GameState.Started)
            RedrawChunksIfNecessary();
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(NewGameKey))
        {
            World.GenerateTerrain();
            World.CalculateMesh();
            CreatePlayer();

            GameState = GameState.Started;

            Debug.Log("New Game started");
        }
        else if (Input.GetKeyDown(SaveKey))
        {
            var storage = new PersistentStorage(World.ChunkSize);

            var t = Player.transform;

            var playerRotation = new Vector3(
                t.GetChild(0).gameObject.transform.eulerAngles.x,
                t.rotation.eulerAngles.y,
                0);
            
            storage.SaveGame(t.position, playerRotation, World);

            Debug.Log("Game Saved");
        }
        else if (Input.GetKeyDown(LoadKey))
        {
            var storage = new PersistentStorage(World.ChunkSize);

            var save = storage.LoadGame();
            World.ChunkSize = save.ChunkSize;
            World.WorldSizeX = save.WorldSizeX;
            World.WorldSizeY = save.WorldSizeY;
            World.WorldSizeZ = save.WorldSizeZ;

            World.LoadTerrain(save);
            World.CalculateMesh();

            CreatePlayer(save.Position, save.Rotation);
            Player.SetActive(true);

            GameState = GameState.Started;

            Debug.Log("Game Loaded");
        }
    }

    void RedrawChunksIfNecessary()
    {
        for (int x = 0; x < World.WorldSizeX; x++)
            for (int z = 0; z < World.WorldSizeZ; z++)
                for (int y = 0; y < World.WorldSizeY; y++)
                {
                    Chunk c = World.Chunks[x, y, z];
                    if (c.Status == ChunkStatus.NeedToBeRedrawn)
                        c.RecreateMeshAndCollider();
                }
    }

    public void ProcessBlockHit(Vector3 hitBlock)
    {
        int chunkX, chunkY, chunkZ, blockX, blockY, blockZ;

        FindChunkAndBlock(hitBlock, out chunkX, out chunkY, out chunkZ, out blockX, out blockY, out blockZ);

        // inform chunk
        var wasBlockDestroyed = World.Chunks[chunkX, chunkY, chunkZ]
            .BlockHit(blockX, blockY, blockZ);

        if (wasBlockDestroyed)
            CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
    }

    public void ProcessBuildBlock(Vector3 hitBlock, BlockTypes type)
    {
        int chunkX, chunkY, chunkZ, blockX, blockY, blockZ;

        FindChunkAndBlock(hitBlock, out chunkX, out chunkY, out chunkZ, out blockX, out blockY, out blockZ);

        // inform chunk
        var wasBlockBuilt = World.Chunks[chunkX, chunkY, chunkZ]
            .BuildBlock(blockX, blockY, blockZ, type);

        if (wasBlockBuilt)
            CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
    }

    void FindChunkAndBlock(Vector3 hitBlock, 
        out int chunkX, out int chunkY, out int chunkZ, 
        out int blockX, out int blockY, out int blockZ)
    {
        chunkX = hitBlock.x < 0 ? 0 : (int)(hitBlock.x / World.ChunkSize);
        chunkY = hitBlock.y < 0 ? 0 : (int)(hitBlock.y / World.ChunkSize);
        chunkZ = hitBlock.z < 0 ? 0 : (int)(hitBlock.z / World.ChunkSize);

        blockX = (int)hitBlock.x - chunkX * World.ChunkSize;
        blockY = (int)hitBlock.y - chunkY * World.ChunkSize;
        blockZ = (int)hitBlock.z - chunkZ * World.ChunkSize;
    }

    /// <summary>
    /// Check if neighboring chunks need to be redrawn and change their status if necessary.
    /// </summary>
    void CheckNeighboringChunks(int blockX, int blockY, int blockZ, int chunkX, int chunkY, int chunkZ)
    {
        // right check
        if (blockX == World.ChunkSize - 1 && chunkX + 1 < World.WorldSizeX)
            World.Chunks[chunkX + 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // BUG: there are sometimes faces not beinf rendered on this axis - dunno why
        // left check
        if (blockX == 0 && chunkX - 1 >= 0)
            World.Chunks[chunkX - 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // top check
        if (blockY == World.ChunkSize - 1 && chunkY + 1 < World.WorldSizeY)
            World.Chunks[chunkX, chunkY + 1, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // bottom check
        if (blockY == 0 && chunkY - 1 >= 0)
            World.Chunks[chunkX, chunkY - 1, chunkZ].Status = ChunkStatus.NeedToBeRedrawn;

        // front check
        if (blockZ == World.ChunkSize - 1 && chunkZ + 1 < World.WorldSizeZ)
            World.Chunks[chunkX, chunkY, chunkZ + 1].Status = ChunkStatus.NeedToBeRedrawn;

        // back check
        if (blockZ == 0 && chunkZ - 1 >= 0)
            World.Chunks[chunkX, chunkY, chunkZ - 1].Status = ChunkStatus.NeedToBeRedrawn;
    }
    
    void CreatePlayer(Vector3? position = null, Vector3? rotation = null)
    {
        var playerPos = position ?? PlayerStartPosition;
        Player.transform.position = new Vector3(
                playerPos.x,
                TerrainGenerator.GenerateDirtHeight(playerPos.x, playerPos.z) + 1,
                playerPos.z);

        if (rotation.HasValue)
        {
            var r = rotation.Value;
            
            var fpc = Player.GetComponent<FirstPersonController>();
            fpc.MouseLook.CharacterTargetRot = Quaternion.Euler(0f, r.y, 0f);
            fpc.MouseLook.CameraTargetRot = Quaternion.Euler(r.x, 0f, 0f);
        }
        else
        {
            var fpc = Player.GetComponent<FirstPersonController>();
            fpc.MouseLook.CharacterTargetRot = Quaternion.Euler(0f, 0f, 0f);
            fpc.MouseLook.CameraTargetRot = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}
