using System.Diagnostics;
using UnityEngine;

namespace Assets.Scripts
{
	public class World : MonoBehaviour
	{
        public Transform TerrainParent;
        public Transform WaterParent;
        
        public bool ActivatePlayer = true;

		public GameObject Player;
		public Material TextureAtlas;
		public Material FluidTexture;
        public const int ChunkSize = 32; //32; // number of blocks in x, y and z

        [SerializeField] public const int WorldSizeX = 7; // 7;
        [SerializeField] public const int WorldSizeY = 4; // 4; // height
        [SerializeField] public const int WorldSizeZ = 7; // 7;
        
        public static Chunk[,,] Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];
        
		public Camera MainCamera;

		public int Posx;
		public int Posz;
        
        public static Stopwatch sw = new Stopwatch();
        public static int numChunks = 0;
        public static long milisec = 0;

        void Start()
		{
            sw.Start();
            Vector3 playerPos = Player.transform.position;
			Player.transform.position = new Vector3(
				playerPos.x,
				TerrainGenerator.GenerateHeight(playerPos.x, playerPos.z) + 1,
				playerPos.z);

			// to be sure player won't fall through the world that hasn't been build yet
			Player.SetActive(false);

            BuildChunks();
            DrawChunks();
        }

        void Update()
		{
            // in final version it should wait for the world genration to end
            if (ActivatePlayer && !Player.activeSelf)
                Player.SetActive(true);
		}
        
        public static bool TryGetBlockFromChunk(int chunkX, int chunkY, int chunkZ, int blockX, int blockY, int blockZ, out BlockData block)
        {
            if (chunkX >= WorldSizeX || chunkX < 0 
                || chunkY >= WorldSizeY || chunkY < 0 
                || chunkZ >= WorldSizeZ || chunkZ < 0)
            {
                // we are outside of the world!
                block = new BlockData(); // dummy data
                return false;
            }

            block = Chunks[chunkX, chunkY, chunkZ].Blocks[blockX, blockY, blockZ];
            return true;
        }

		public static string BuildChunkFileName(Vector3 v) => 
            Application.persistentDataPath + "/savedata/Chunk_" 
            + (int) v.x + "_" + (int) v.y + "_" + (int) v.z + "_" + ChunkSize + ".dat";

        public static int BuildChunkName(int x, int y, int z) => WorldSizeY * WorldSizeY * y + WorldSizeZ * z + x;

        void BuildChunks()
        {
            for (int x = 0; x < WorldSizeX; x++)
                for (int z = 0; z < WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                    {
                        var chunkPosition = new Vector3(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                        var chunkName = BuildChunkName(x, y, z);

                        var c = new Chunk(chunkPosition, TextureAtlas, FluidTexture, chunkName, this, new Vector3Int(x, y, z));
                        c.Blocks = TerrainGenerator.BuildChunk(chunkPosition);
                        Chunks[x, y, z] = c;
                    }
        }
        
        void DrawChunks()
        {
            for (int x = 0; x < WorldSizeX; x++)
                for (int z = 0; z < WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                    {
                        Chunk c = Chunks[x, y, z];
                        if (c.Status == Chunk.ChunkStatus.NotInitialized)
                            c.CreateChunkObject();
                        else if (c.Status == Chunk.ChunkStatus.NeedToBeRedrawn)
                        {
                            c.DestroyMeshAndCollider();
                            c.CreateChunkObject();
                        }
                    }
            
            sw.Stop();
            UnityEngine.Debug.Log("It took " + sw.ElapsedMilliseconds + "ms to create the whole world");
        }
	}
}