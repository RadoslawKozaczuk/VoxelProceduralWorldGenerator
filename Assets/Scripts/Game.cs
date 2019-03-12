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
	[SerializeField] Text _internalStatus;
	[SerializeField] World _world;
	[SerializeField] GameObject _player;
	[SerializeField] Camera _mainCamera;
	[SerializeField] GameState _gameState = GameState.NotInitialized;
	[SerializeField] KeyCode _saveKey = KeyCode.K;
	[SerializeField] KeyCode _loadKey = KeyCode.L;
	[SerializeField] Vector3 _playerStartPosition;

	void Start()
	{
		_crosshair.enabled = false;

		_controlsLabel.text = "Controls:" + Environment.NewLine
			+ "Attack - LPM" + Environment.NewLine
			+ "Build - RPM" + Environment.NewLine
			+ $"Save Game - { _saveKey }" + Environment.NewLine
			+ $"Load Game - { _loadKey }";
		Debug.Log("Waiting instructions...");

		_player.SetActive(false);

		_gameState = GameState.Starting;
		StartCoroutine(_world.GenerateWorld(true));
	}

	void Update()
	{
		var progress = Mathf.Clamp01(_world.AlreadyGenerated / (_world.MeshProgressSteps + _world.TerrainProgressSteps));
		_progressBar.value = progress;
		_progressText.text = Mathf.RoundToInt(progress * 100) + "%";
		_description.text = _world.ProgressDescription;
		_internalStatus.text = "Game Status: " + Enum.GetName(_gameState.GetType(), _gameState) + Environment.NewLine
			+ "Generator Status: " + Enum.GetName(_world.Status.GetType(), _world.Status);

		HandleInput();

		if (_world.Status == WorldGeneratorStatus.TerrainReady && _gameState == GameState.Starting)
			StartCoroutine(_world.GenerateMeshes());

		if (_world.Status == WorldGeneratorStatus.TerrainReady && _gameState == GameState.ReStarting)
		{
			CreatePlayer(_world.PlayerLoadedPosition, _world.PlayerLoadedRotation);
			StartCoroutine(_world.RedrawChunksIfNecessaryAsync());
		}

		if (_world.Status == WorldGeneratorStatus.AllReady)
		{
			_crosshair.enabled = true;

			if (_gameState == GameState.Starting)
			{
				_gameState = GameState.Started;
				CreatePlayer();
			}

			_player.SetActive(true);
			_world.RedrawChunksIfNecessary();
		}
	}

	void HandleInput()
	{
		if (Input.GetKeyDown(_saveKey))
		{
			var storage = new PersistentStorage(World.ChunkSize);

			var t = _player.transform;

			var playerRotation = new Vector3(
				t.GetChild(0).gameObject.transform.eulerAngles.x,
				t.rotation.eulerAngles.y,
				0);

			storage.SaveGame(t.position, playerRotation, _world);
			Debug.Log("Game Saved");
		}
		else if (Input.GetKeyDown(_loadKey))
		{
			_gameState = GameState.ReStarting;
			_player.SetActive(false);
			StartCoroutine(_world.LoadWorld(false));
		}
	}

	public void ProcessBlockHit(Vector3 hitBlock)
	{
		FindChunkAndBlock(hitBlock, out int chunkX, out int chunkY, out int chunkZ, out int blockX, out int blockY, out int blockZ);

		// inform chunk
		var c = _world.Chunks[chunkX, chunkY, chunkZ];

		// was block destroyed
		if (_world.BlockHit(blockX, blockY, blockZ, c))
			CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
	}

	public void ProcessBuildBlock(Vector3 hitBlock, BlockTypes type)
	{
		FindChunkAndBlock(hitBlock, out int chunkX, out int chunkY, out int chunkZ, out int blockX, out int blockY, out int blockZ);

		// inform chunk
		var c = _world.Chunks[chunkX, chunkY, chunkZ];

		// was block built
		if (_world.BuildBlock(blockX, blockY, blockZ, type, c))
			CheckNeighboringChunks(blockX, blockY, blockZ, chunkX, chunkY, chunkZ);
	}

	void FindChunkAndBlock(Vector3 hitBlock,
		out int chunkX, out int chunkY, out int chunkZ,
		out int blockX, out int blockY, out int blockZ)
	{
		chunkX = hitBlock.x < 0 ? 0 : (int)(hitBlock.x / World.ChunkSize);
		chunkY = hitBlock.y < 0 ? 0 : (int)(hitBlock.y / World.ChunkSize);
		chunkZ = hitBlock.z < 0 ? 0 : (int)(hitBlock.z / World.ChunkSize);

		blockX = (int)hitBlock.x;
		blockY = (int)hitBlock.y;
		blockZ = (int)hitBlock.z;
	}

	/// <summary>
	/// Check if neighboring chunks need to be redrawn and change their status if necessary.
	/// </summary>
	void CheckNeighboringChunks(int blockX, int blockY, int blockZ, int chunkX, int chunkY, int chunkZ)
	{
		// right check
		if (blockX == World.ChunkSize - 1 && chunkX + 1 < World.Settings.WorldSizeX)
			_world.Chunks[chunkX + 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

		// left check
		if (blockX == 0 && chunkX - 1 >= 0)
			_world.Chunks[chunkX - 1, chunkY, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

		// top check
		if (blockY == World.ChunkSize - 1 && chunkY + 1 < World.WorldSizeY)
			_world.Chunks[chunkX, chunkY + 1, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

		// bottom check
		if (blockY == 0 && chunkY - 1 >= 0)
			_world.Chunks[chunkX, chunkY - 1, chunkZ].Status = ChunkStatus.NeedToBeRecreated;

		// front check
		if (blockZ == World.ChunkSize - 1 && chunkZ + 1 < World.Settings.WorldSizeZ)
			_world.Chunks[chunkX, chunkY, chunkZ + 1].Status = ChunkStatus.NeedToBeRecreated;

		// back check
		if (blockZ == 0 && chunkZ - 1 >= 0)
			_world.Chunks[chunkX, chunkY, chunkZ - 1].Status = ChunkStatus.NeedToBeRecreated;
	}

	void CreatePlayer(Vector3? position = null, Vector3? rotation = null)
	{
		var playerPos = position ?? _playerStartPosition;
		_player.transform.position = new Vector3(
				playerPos.x,
				TerrainGenerator.GenerateDirtHeight(playerPos.x, playerPos.z) + 2,
				playerPos.z);

		var fpc = _player.GetComponent<FirstPersonController>();
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
