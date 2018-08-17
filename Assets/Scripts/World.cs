using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace Assets.Scripts
{
	public class World : MonoBehaviour
	{
        public Transform TerrainParent;
        public Transform WaterParent;

        public uint QueueSize;
        public bool ActivatePlayer = true;

		public GameObject Player;
		public Material TextureAtlas;
		public Material FluidTexture;
		public const int ChunkSize = 32; // number of blocks in x, y and z
        
        public const int WorldSizeX = 7;
        public const int WorldSizeY = 4; // height
        public const int WorldSizeZ = 7;
        
        public static CoroutineQueue Queue; 
		public static uint MaxCoroutines = 1000;
        
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
            
			Queue = new CoroutineQueue(MaxCoroutines, StartCoroutine);

            for (int x = 0; x < WorldSizeX; x++)
                for (int z = 0; z < WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                        BuildChunkAt(x, y, z);
        }

        void Update()
		{
            QueueSize = Queue.NumActive;

            // in final version it should wait for the world genration to end
            if (ActivatePlayer && !Player.activeSelf)
                Player.SetActive(true);

            Queue.Run(DrawChunks());
		}
        
		public static string BuildChunkFileName(Vector3 v)
		{
			return Application.persistentDataPath + "/savedata/Chunk_" 
												  + (int) v.x + "_" + (int) v.y + "_" + (int) v.z + "_" 
												  + ChunkSize + ".dat";
		}

        public static int BuildChunkName(int x, int y, int z) 
            => WorldSizeY * WorldSizeY * y + WorldSizeZ * z + x;

        void BuildChunkAt(int x, int y, int z)
        {
            var chunkPosition = new Vector3(x * ChunkSize, y * ChunkSize, z * ChunkSize);
            var chunkName = BuildChunkName(x, y, z);

            var c = new Chunk(chunkPosition, TextureAtlas, FluidTexture, chunkName, this, x, y, z);
            Chunks[x, y, z] = c;
        }
        
        IEnumerator DrawChunks()
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

                        yield return null;
                    }
            
            sw.Stop();
            UnityEngine.Debug.Log("It took " + sw.ElapsedMilliseconds + "ms to create the whole world");
        }
	}
}