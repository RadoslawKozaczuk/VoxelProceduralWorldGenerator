using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class World : ScriptableObject
{
	public const int WorldSizeY = 4, ChunkSize = 32;

	public Chunk[,,] Chunks;
	public Block[,,] Blocks;
	public WorldGeneratorStatus Status { get; private set; }

	public static GameSettings Settings;

	public readonly int TotalBlockNumberX, TotalBlockNumberY, TotalBlockNumberZ;

	// progress bar related variables
	public float TerrainProgressSteps { get; private set; }
	public float MeshProgressSteps { get; private set; }
	public float AlreadyGenerated { get; private set; }
	public string ProgressDescription;

	public Vector3 PlayerLoadedRotation, PlayerLoadedPosition;

	[SerializeField] TerrainGenerator _terrainGenerator;
	[SerializeField] MeshGenerator _meshGenerator;
	[SerializeField] Material _terrainTexture;
	[SerializeField] Material _waterTexture;

	Stopwatch _stopwatch = new Stopwatch();
	Scene _worldScene;
	long _accumulatedTerrainGenerationTime, _accumulatedMeshCreationTime;
	int _progressStep = 1;
	const int OverallNumberOfGenerationSteps = 9;

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
		UnityEngine.Debug.Log("WaterLevel " + Settings.WaterLevel);
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
		var retVal = false;

		byte previousHpLevel = Blocks[blockX, blockY, blockZ].HealthLevel;
		Blocks[blockX, blockY, blockZ].Hp--;
		byte currentHpLevel = CalculateHealthLevel(
			Blocks[blockX, blockY, blockZ].Hp,
			LookupTables.BlockHealthMax[(int)Blocks[blockX, blockY, blockZ].Type]);

		if (currentHpLevel != previousHpLevel)
		{
			Blocks[blockX, blockY, blockZ].HealthLevel = currentHpLevel;

			if (Blocks[blockX, blockY, blockZ].Hp == 0)
			{
				Blocks[blockX, blockY, blockZ].Type = BlockTypes.Air;
				_meshGenerator.RecalculateFacesAfterBlockDestroy(ref Blocks, blockX, blockY, blockZ);
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

	/// <summary>
	/// Returns true if a new block has been built.
	/// </summary>
	public bool BuildBlock(int blockX, int blockY, int blockZ, BlockTypes type, Chunk c)
	{
		if (Blocks[blockX, blockY, blockZ].Type != BlockTypes.Air) return false;

		Blocks[blockX, blockY, blockZ].Type = type;
		_meshGenerator.RecalculateFacesAfterBlockBuild(ref Blocks, blockX, blockY, blockZ);

		Blocks[blockX, blockY, blockZ].Hp = LookupTables.BlockHealthMax[(int)type];
		Blocks[blockX, blockY, blockZ].HealthLevel = 0;

		c.Status = ChunkStatus.NeedToBeRecreated;

		return true;
	}

	public IEnumerator RedrawChunksIfNecessaryAsync()
	{
		_stopwatch.Restart();
		Status = WorldGeneratorStatus.GeneratingMeshes;

		for (int x = 0; x < Settings.WorldSizeX; x++)
			for (int z = 0; z < Settings.WorldSizeZ; z++)
				for (int y = 0; y < WorldSizeY; y++)
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
		for (int x = 0; x < Settings.WorldSizeX; x++)
			for (int z = 0; z < Settings.WorldSizeZ; z++)
				for (int y = 0; y < WorldSizeY; y++)
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

		//CreateGameObjects(firstRun);
		AlreadyGenerated += 4;

		for (int x = 0; x < Settings.WorldSizeX; x++)
			for (int z = 0; z < Settings.WorldSizeZ; z++)
				for (int y = 0; y < WorldSizeY; y++)
				{
					var loaded = save.Chunks[x, y, z];
					var c = new Chunk()
					{
						Coord = loaded.Coord,
						Position = loaded.Position,
						Status = ChunkStatus.NeedToBeRedrawn
					};

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
		UnityEngine.Debug.Log($"It took {_accumulatedTerrainGenerationTime} ms to load all terrain.");
	}

	public IEnumerator GenerateMeshes()
	{
		ProgressDescription = "Generating meshes";

		_accumulatedMeshCreationTime = 0;
		_stopwatch.Restart();
		Status = WorldGeneratorStatus.GeneratingMeshes;

		for (int x = 0; x < Settings.WorldSizeX; x++)
			for (int z = 0; z < Settings.WorldSizeZ; z++)
				for (int y = 0; y < WorldSizeY; y++)
				{
					var c = Chunks[x, y, z];
					_meshGenerator.ExtractMeshData(ref Blocks, ref c.Position, out MeshData terrainData, out MeshData waterData);
					CreateRenderingComponents(c, terrainData, waterData);
					c.Status = ChunkStatus.Created;

					AlreadyGenerated++;

					yield return null; // give back control
				}

		Status = WorldGeneratorStatus.AllReady;
		_stopwatch.Stop();
		_accumulatedMeshCreationTime += _stopwatch.ElapsedMilliseconds;
		ProgressDescription = "Ready";

		UnityEngine.Debug.Log("It took "
			+ _accumulatedMeshCreationTime
			+ " ms to create all meshes.");
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
		for (var x = 0; x < TotalBlockNumberX; x++)
		{
			for (var y = 0; y < TotalBlockNumberY; y++)
				for (var z = 0; z < TotalBlockNumberZ; z++)
				{
					var type = types[Utils.IndexFlattenizer3D(x, y, z, TotalBlockNumberX, TotalBlockNumberY)];
					Blocks[x, y, z].Type = type;
					Blocks[x, y, z].Hp = LookupTables.BlockHealthMax[(int)type];
				}
		}
	}

	void CreateGameObjects(bool firstRun)
	{
		for (int x = 0; x < Settings.WorldSizeX; x++)
			for (int z = 0; z < Settings.WorldSizeZ; z++)
				for (int y = 0; y < WorldSizeY; y++)
				{
					var chunkCoord = new Vector3Int(x, y, z);

					var c = new Chunk()
					{
						Position = new Vector3Int(chunkCoord.x * ChunkSize, chunkCoord.y * ChunkSize, chunkCoord.z * ChunkSize),
						Coord = chunkCoord
					};

					if (firstRun)
					{
						CreateGameObjects(c);

						SceneManager.MoveGameObjectToScene(c.Terrain.gameObject, _worldScene);
						SceneManager.MoveGameObjectToScene(c.Water.gameObject, _worldScene);
					}

					Chunks[x, y, z] = c;
				}
	}

	void RecreateMeshAndCollider(Chunk c)
	{
		DestroyImmediate(c.Terrain.GetComponent<Collider>());
		_meshGenerator.ExtractMeshData(ref Blocks, ref c.Position, out MeshData t, out MeshData w);
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
		_meshGenerator.ExtractMeshData(ref Blocks, ref c.Position, out MeshData t, out MeshData w);
		var tm = _meshGenerator.CreateMesh(t);

		var meshFilter = c.Terrain.GetComponent<MeshFilter>();
		meshFilter.mesh = tm;

		c.Status = ChunkStatus.Created;
	}

	void CreateGameObjects(Chunk c)
	{
		string name = "" + c.Coord.x + c.Coord.y + c.Coord.z;
		c.Terrain = new GameObject(name + "_terrain");
		c.Terrain.transform.position = c.Position;
		c.Water = new GameObject(name + "_water");
		c.Water.transform.position = c.Position;
		c.Status = ChunkStatus.NeedToBeRedrawn;
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
