using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Assets.Scripts
{
	public class World : MonoBehaviour
	{
        public Transform TerrainParent;
        public Transform WaterParent;

        public uint QueueSize;
        public bool UseJobSystem = true;
        public bool ActivatePlayer = true;

		public GameObject Player;
		public Material TextureAtlas; 
		public Material FluidTexture;
		public const int ColumnHeight = 3; // number of chunks in column
		public const int ChunkSize = 16; // number of blocks in x, y and z (32 is safe and recomended)

        public const int WorldSizeX = 5;
        public const int WorldSizeY = 5; // height
        public const int WorldSizeZ = 5;
        
        public static CoroutineQueue Queue; 
		public static uint MaxCoroutines = 1000;
		
		public const int Radius = 3; // radius tell us how many chunks around the layer needs to be generated
		public static List<int> ToRemove = new List<int>();
        
        public static Chunk[,,] Chunks = new Chunk[WorldSizeX, WorldSizeY, WorldSizeZ];
        
		public Camera MainCamera;

		public int Posx;
		public int Posz;

		void Start()
		{
            Vector3 playerPos = Player.transform.position;
			Player.transform.position = new Vector3(
				playerPos.x,
				Utils.GenerateHeight(playerPos.x, playerPos.z) + 1,
				playerPos.z);

			// to be sure player won't fall through the world that hasn't been build yet
			Player.SetActive(false);
            
			Queue = new CoroutineQueue(MaxCoroutines, StartCoroutine);

            for (int x = 0; x < WorldSizeX; x++)
                for (int z = 0; z < WorldSizeZ; z++)
                    for (int y = 0; y < WorldSizeY; y++)
                        BuildChunkAt(x, y, z);

            // BUG: It doesn't really work as intended 
            // For some reason recreated chunks lose their transparency
            //InformSurroundingChunks(x, y, z);
        }

        void Update()
		{
            QueueSize = Queue.NumActive;

            // in final version it should wait for the world genration to end
            if (ActivatePlayer && !Player.activeSelf)
            {
                Player.SetActive(true);
            }

            Queue.Run(DrawChunks());
		}
        
		public static string BuildChunkFileName(Vector3 v)
		{
			return Application.persistentDataPath + "/savedata/Chunk_" 
												  + (int) v.x + "_" + (int) v.y + "_" + (int) v.z + "_" 
												  + ChunkSize + "_" + Radius + ".dat";
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
                            c.CreateMeshAndCollider();
                        else if (c.Status == Chunk.ChunkStatus.NeedToBeRedrawn)
                        {
                            c.DestroyMeshAndCollider();
                            c.CreateMeshAndCollider();
                        }

                        yield return null;
                    }
        }
	}
}