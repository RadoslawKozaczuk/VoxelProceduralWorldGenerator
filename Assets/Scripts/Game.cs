using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Game : MonoBehaviour
{
    public World World { get; set; }
    public GameObject Player;
    public Camera MainCamera;
    public KeyCode NewGameKey = KeyCode.N;
    public KeyCode SaveKey = KeyCode.S;
    public KeyCode LoadKey = KeyCode.L;
    public KeyCode ControlKey = KeyCode.LeftControl;

    public Vector3 PlayerStartPosition;
    public bool ActivatePlayer = true;

    void Start()
    {
        World = GetComponent<World>();
        Debug.Log("Waiting instructions...");
    }

    void Update()
    {
        if (Input.GetKeyDown(NewGameKey))
        {
            World.GenerateTerrain();
            World.CalculateMesh();
            CreatePlayer();

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

            Debug.Log("Game Loaded");
        }
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
