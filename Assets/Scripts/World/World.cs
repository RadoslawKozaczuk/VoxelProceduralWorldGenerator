using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.World
{
	[CreateAssetMenu]
	public class World : ScriptableObject
	{
		public const int WorldSizeY = 4, ChunkSize = 32;
		const int OverallNumberOfGenerationSteps = 9;

		public static GameSettings Settings;

		public readonly int TotalBlockNumberX, TotalBlockNumberY, TotalBlockNumberZ;

		public Chunk[,,] Chunks;
		public Block[,,] Blocks;
		public Vector3 PlayerLoadedRotation, PlayerLoadedPosition;
		public WorldGeneratorStatus Status { get; private set; }
		public float TerrainProgressSteps { get; private set; }
		public float MeshProgressSteps { get; private set; }
		public float AlreadyGenerated { get; private set; }
		public string ProgressDescription;

		[SerializeField] TerrainGenerator _terrainGenerator;
		[SerializeField] MeshGenerator _meshGenerator;
		[SerializeField] Material _terrainTexture;
		[SerializeField] Material _waterTexture;

		Stopwatch _stopwatch = new Stopwatch();
		Scene _worldScene;
		long _accumulatedTerrainGenerationTime, _accumulatedMeshCreationTime;
		int _progressStep = 1;

		World()
		{
			TotalBlockNumberX = Settings.WorldSizeX * ChunkSize;
			TotalBlockNumberY = WorldSizeY * ChunkSize;
			TotalBlockNumberZ = Settings.WorldSizeZ * ChunkSize;
		}

		void OnEnable()
		{
			_terrainGenerator = new TerrainGenerator(Settings);
			_meshGenerator = new MeshGenerator(Settings);
		}

		/// <summary>
		/// Generates block types with hp and hp level.
		/// Chunks and their objects (if first run = true).
		/// And calculates faces.
		/// </summary>
		public IEnumerator GenerateWorld(bool firstRun)
		{
			_stopwatch.Restart();
			ResetProgressBarVariables();

			Status = WorldGeneratorStatus.GeneratingTerrain;

			yield return null;
			ProgressDescription = "Initialization...";
			Blocks = new Block[Settings.WorldSizeX * ChunkSize, WorldSizeY * ChunkSize, Settings.WorldSizeZ * ChunkSize];
			AlreadyGenerated += _progressStep;

			yield return null;
			ProgressDescription = "Calculating heights...";
			var heights = _terrainGenerator.CalculateHeights();
			AlreadyGenerated += _progressStep;

			yield return null;
			ProgressDescription = "Generating terrain...";
			var types = _terrainGenerator.CalculateBlockTypes(heights);
			AlreadyGenerated += _progressStep;

			yield return null;
			ProgressDescription = "Output deflattenization...";
			DeflattenizeOutput(ref types);
			AlreadyGenerated += _progressStep;

			yield return null;
			if (Settings.IsWater)
			{
				ProgressDescription = "Generating water...";
				_terrainGenerator.AddWater(ref Blocks);
			}
			AlreadyGenerated += _progressStep;

			yield return null;
			ProgressDescription = "Generating trees...";
			_terrainGenerator.AddTrees(ref Blocks);
			AlreadyGenerated += _progressStep;

			yield return null;
			ProgressDescription = "Creating game objects...";
			if (firstRun)
				_worldScene = SceneManager.CreateScene(name);

			Chunks = new Chunk[Settings.WorldSizeX, WorldSizeY, Settings.WorldSizeZ];
			CreateGameObjects(firstRun);
			AlreadyGenerated += _progressStep;

			yield return null;
			ProgressDescription = "Calculating faces...";
			_meshGenerator.CalculateFaces(ref Blocks);
			AlreadyGenerated += _progressStep;

			yield return null;
			ProgressDescription = "World boundaries check...";
			_meshGenerator.WorldBoundariesCheck(ref Blocks);
			AlreadyGenerated += _progressStep;

			Status = WorldGeneratorStatus.TerrainReady;

			_stopwatch.Stop();
			UnityEngine.Debug.Log($"It took {_stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond} ms to generate all terrain.");
		}

		/// <summary>
		/// Returns true if the block has been destroyed.
		/// </summary>
		public bool BlockHit(int blockX, int blockY, int blockZ, Chunk c)
		{
			bool destroyed = false;

			ref Block b = ref Blocks[blockX, blockY, blockZ];

			byte previousHpLevel = b.HealthLevel--;
			byte currentHpLevel = CalculateHealthLevel(b.Hp, LookupTables.BlockHealthMax[(int)b.Type]);

			if (currentHpLevel != previousHpLevel)
			{
				b.HealthLevel = currentHpLevel;

				if (b.Hp == 0)
				{
					b.Type = BlockTypes.Air;
					_meshGenerator.RecalculateFacesAfterBlockDestroy(ref Blocks, blockX, blockY, blockZ);
					c.Status = ChunkStatus.NeedToBeRecreated;
					destroyed = true;
				}
				else
					c.Status = ChunkStatus.NeedToBeRedrawn;
			}

			return destroyed;
		}

		/// <summary>
		/// Returns true if a new block has been built.
		/// </summary>
		public bool BuildBlock(int blockX, int blockY, int blockZ, BlockTypes type, Chunk c)
		{
			ref Block b = ref Blocks[blockX, blockY, blockZ];

			if (b.Type != BlockTypes.Air) return false;

			_meshGenerator.RecalculateFacesAfterBlockBuild(ref Blocks, blockX, blockY, blockZ);

			b.Type = type;
			b.Hp = LookupTables.BlockHealthMax[(int)type];
			b.HealthLevel = 0;

			c.Status = ChunkStatus.NeedToBeRecreated;

			return true;
		}

		public IEnumerator RedrawChunksIfNecessaryAsync()
		{
			_stopwatch.Restart();
			Status = WorldGeneratorStatus.GeneratingMeshes;

			for (int x = 0, y, z; x < Settings.WorldSizeX; x++)
				for (z = 0; z < Settings.WorldSizeZ; z++)
					for (y = 0; y < WorldSizeY; y++)
					{
						Chunk c = Chunks[x, y, z];
						if (c.Status == ChunkStatus.NeedToBeRecreated)
							RecreateMeshAndCollider(c);
						else if (c.Status == ChunkStatus.NeedToBeRedrawn) // used only for cracks
							RecreateTerrainMesh(c);

						AlreadyGenerated++;

						yield return null; // give back control
					}

			Status = WorldGeneratorStatus.AllReady;
			_stopwatch.Stop();
			_accumulatedMeshCreationTime += _stopwatch.ElapsedMilliseconds;
			UnityEngine.Debug.Log($"It took {_accumulatedTerrainGenerationTime} ms to redraw all meshes.");
		}

		public void RedrawChunksIfNecessary()
		{
			for (int x = 0, y, z; x < Settings.WorldSizeX; x++)
				for (z = 0; z < Settings.WorldSizeZ; z++)
					for (y = 0; y < WorldSizeY; y++)
					{
						Chunk c = Chunks[x, y, z];
						if (c.Status == ChunkStatus.NeedToBeRecreated)
							RecreateMeshAndCollider(c);
						else if (c.Status == ChunkStatus.NeedToBeRedrawn) // used only for cracks
							RecreateTerrainMesh(c);
					}
		}

		public IEnumerator LoadWorld(bool firstRun)
		{
			ResetProgressBarVariables();
			_stopwatch.Restart();
			Status = WorldGeneratorStatus.GeneratingTerrain;
			ProgressDescription = "Loading data...";
			yield return null; // give back control

			var storage = new PersistentStorage(ChunkSize);
			var save = storage.LoadGame();

			Settings.WorldSizeX = save.WorldSizeX;
			Settings.WorldSizeZ = save.WorldSizeZ;
			PlayerLoadedPosition = save.PlayerPosition;
			PlayerLoadedRotation = save.PlayerRotation;

			AlreadyGenerated += 4;
			ProgressDescription = "Creating game objects...";
			yield return null; // give back control

			if (firstRun)
				_worldScene = SceneManager.CreateScene(name);

			AlreadyGenerated += 4;

			for (int x = 0, y, z; x < Settings.WorldSizeX; x++)
				for (z = 0; z < Settings.WorldSizeZ; z++)
					for (y = 0; y < WorldSizeY; y++)
					{
						var loaded = save.Chunks[x, y, z];
						var c = new Chunk(loaded.Coord, loaded.Position, ChunkStatus.NeedToBeRedrawn);

						if (firstRun)
						{
							CreateGameObjects(c);

							SceneManager.MoveGameObjectToScene(c.Terrain.gameObject, _worldScene);
							SceneManager.MoveGameObjectToScene(c.Water.gameObject, _worldScene);
						}

						AlreadyGenerated++;

						yield return null; // give back control
					}

			Status = WorldGeneratorStatus.TerrainReady;
			_stopwatch.Stop();
			_accumulatedTerrainGenerationTime += _stopwatch.ElapsedMilliseconds;
			UnityEngine.Debug.Log($"It took { _accumulatedTerrainGenerationTime } ms to load all terrain.");
		}

		public IEnumerator GenerateMeshes()
		{
			ProgressDescription = "Generating meshes";

			_accumulatedMeshCreationTime = 0;
			_stopwatch.Restart();
			Status = WorldGeneratorStatus.GeneratingMeshes;

			for (int x = 0, y, z; x < Settings.WorldSizeX; x++)
				for (z = 0; z < Settings.WorldSizeZ; z++)
					for (y = 0; y < WorldSizeY; y++)
					{
						Chunk c = Chunks[x, y, z];

						// out: This method sets the value of the argument used as this parameter.
						// ref: This method may set the value of the argument used as this parameter.
						// in: This method doesn't modify the value of the argument used as this parameter,
						//		it should only be applied to readonly structs, otherwise it harms the performance
						//		because the compilator has to make defensive copies just to be safe.
						_meshGenerator.ExtractMeshData(ref Blocks, in c.Position, out MeshData terrainData, out MeshData waterData);
						CreateRenderingComponents(c, terrainData, waterData);
						c.Status = ChunkStatus.Created;

						AlreadyGenerated++;

						yield return null; // give back control
					}

			Status = WorldGeneratorStatus.AllReady;
			_stopwatch.Stop();
			_accumulatedMeshCreationTime += _stopwatch.ElapsedMilliseconds;
			ProgressDescription = "Ready";

			UnityEngine.Debug.Log($"It took { _accumulatedMeshCreationTime } ms to create all meshes.");
		}

		void ResetProgressBarVariables()
		{
			// Unity editor remembers the state of the asset classes so these values have to reinitialized
			_accumulatedTerrainGenerationTime = 0;
			_accumulatedMeshCreationTime = 0;
			_progressStep = 1;
			AlreadyGenerated = 0;

			MeshProgressSteps = Settings.WorldSizeX * WorldSizeY * Settings.WorldSizeZ;

			while (OverallNumberOfGenerationSteps * _progressStep * 2f < MeshProgressSteps)
				_progressStep++;

			TerrainProgressSteps = OverallNumberOfGenerationSteps * _progressStep;
		}

		byte CalculateHealthLevel(int hp, int maxHp)
		{
			float proportion = (float)hp / maxHp; // 0.625f

			// TODO: this require information from MeshGenerator which breaks the encapsulation rule
			float step = (float)1 / 11; // _crackUVs.Length; // 0.09f
			float value = proportion / step; // 6.94f
			int level = Mathf.RoundToInt(value); // 7

			return (byte)(11 - level); // array is in reverse order so we subtract our value from 11
		}

		void DeflattenizeOutput(ref BlockTypes[] types)
		{
			ref Block b = ref Blocks[0, 0, 0];
			for (int x = 0, y, z; x < TotalBlockNumberX; x++)
				for (y = 0; y < TotalBlockNumberY; y++)
					for (z = 0; z < TotalBlockNumberZ; z++)
					{
						var type = types[Utils.IndexFlattenizer3D(x, y, z, TotalBlockNumberX, TotalBlockNumberY)];

						b = ref Blocks[x, y, z];
						b.Type = type;
						b.Hp = LookupTables.BlockHealthMax[(int)type];
					}
		}

		void CreateGameObjects(bool firstRun)
		{
			for (int x = 0, y, z; x < Settings.WorldSizeX; x++)
				for (z = 0; z < Settings.WorldSizeZ; z++)
					for (y = 0; y < WorldSizeY; y++)
					{
						var c = new Chunk(new Vector3Int(x, y, z), new Vector3Int(x * ChunkSize, y * ChunkSize, z * ChunkSize));

						if (firstRun)
						{
							CreateGameObjects(c);
							c.Status = ChunkStatus.NeedToBeRedrawn;

							SceneManager.MoveGameObjectToScene(c.Terrain.gameObject, _worldScene);
							SceneManager.MoveGameObjectToScene(c.Water.gameObject, _worldScene);
						}

						Chunks[x, y, z] = c;
					}
		}

		void RecreateMeshAndCollider(Chunk c)
		{
			DestroyImmediate(c.Terrain.GetComponent<Collider>());
			_meshGenerator.ExtractMeshData(ref Blocks, in c.Position, out MeshData t, out MeshData w);
			var tm = _meshGenerator.CreateMesh(t);
			var wm = _meshGenerator.CreateMesh(w);

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
		void RecreateTerrainMesh(Chunk c)
		{
			_meshGenerator.ExtractMeshData(ref Blocks, in c.Position, out MeshData t, out MeshData w);
			var tm = _meshGenerator.CreateMesh(t);

			var meshFilter = c.Terrain.GetComponent<MeshFilter>();
			meshFilter.mesh = tm;

			c.Status = ChunkStatus.Created;
		}

		void CreateGameObjects(Chunk c)
		{
			string name = c.Coord.x.ToString() + c.Coord.y + c.Coord.z;
			c.Terrain = new GameObject(name + "_terrain");
			c.Terrain.transform.position = c.Position;
			c.Water = new GameObject(name + "_water");
			c.Water.transform.position = c.Position;
		}

		void CreateRenderingComponents(Chunk chunk, MeshData terrainData, MeshData waterData)
		{
			var meshT = _meshGenerator.CreateMesh(terrainData);
			var rt = chunk.Terrain.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
			rt.material = _terrainTexture;

			var mft = (MeshFilter)chunk.Terrain.AddComponent(typeof(MeshFilter));
			mft.mesh = meshT;

			var ct = chunk.Terrain.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
			ct.sharedMesh = meshT;

			var meshW = _meshGenerator.CreateMesh(waterData);
			var rw = chunk.Water.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
			rw.material = _waterTexture;

			var mfw = (MeshFilter)chunk.Water.AddComponent(typeof(MeshFilter));
			mfw.mesh = meshW;
		}
	}
}