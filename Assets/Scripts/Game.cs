using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Game : MonoBehaviour
{
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

    void Start() => Debug.Log("Waiting instructions...");

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
        int chunkX = hitBlock.x < 0 ? 0 : (int)(hitBlock.x / World.ChunkSize);
        int chunkY = hitBlock.y < 0 ? 0 : (int)(hitBlock.y / World.ChunkSize);
        int chunkZ = hitBlock.z < 0 ? 0 : (int)(hitBlock.z / World.ChunkSize);

        int blockX = (int)hitBlock.x - chunkX * World.ChunkSize;
        int blockY = (int)hitBlock.y - chunkY * World.ChunkSize;
        int blockZ = (int)hitBlock.z - chunkZ * World.ChunkSize;

        // inform chunk
        var wasBlockDestroyed = World.Chunks[chunkX, chunkY, chunkZ]
            .BlockHit(blockX, blockY, blockZ);

        if (wasBlockDestroyed)
            CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
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
            fpc.m_MouseLook.m_CharacterTargetRot = Quaternion.Euler(0f, r.y, 0f);
            fpc.m_MouseLook.m_CameraTargetRot = Quaternion.Euler(r.x, 0f, 0f);
        }
        else
        {
            var fpc = Player.GetComponent<FirstPersonController>();
            fpc.m_MouseLook.m_CharacterTargetRot = Quaternion.Euler(0f, 0f, 0f);
            fpc.m_MouseLook.m_CameraTargetRot = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}
