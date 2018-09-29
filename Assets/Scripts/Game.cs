using UnityEngine;

public class Game : MonoBehaviour
{
    public World World { get; set; }
    public GameObject Player;
    public Camera MainCamera;
    public KeyCode NewGameKey = KeyCode.N;
    public KeyCode SaveKey = KeyCode.S;
    public KeyCode LoadKey = KeyCode.L;
    public KeyCode ControlKey = KeyCode.LeftControl;

    public bool ActivatePlayer = true;

    void Start()
    {
        World = GetComponent<World>();
        Debug.Log("Waiting instructions...");
    }

    // Update is called once per frame
    void Update()
    {
        if (/*Input.GetKey(ControlKey) && */Input.GetKeyDown(NewGameKey))
        {
            World.GenerateTerrain();
            World.CalculateMesh();
            CreatePlayer();

            Debug.Log("New Game started");
        }
        else if (/*Input.GetKey(ControlKey) && */Input.GetKeyDown(SaveKey))
        {
            var storage = new PersistentStorage(World.ChunkSize);

            storage.SaveGame(Player.transform, World);
            
            // save game
            Debug.Log("Game Saved");
        }
        else if (/*Input.GetKey(ControlKey) && */Input.GetKeyDown(LoadKey))
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
            
            // load game
            Debug.Log("Game Loaded");
        }
    }

    public void CreatePlayer(Vector3? position = null, Quaternion? rotation = null)
    {
        Player.transform.position = position 
            ?? new Vector3(10, TerrainGenerator.GenerateDirtHeight(10, 10) + 1, 10);

        // for future reference
        // TerrainGenerator.GenerateDirtHeight(playerPos.x, playerPos.z) + 1
        
        if (rotation.HasValue)
            Player.transform.rotation = rotation.Value;
    }
}
