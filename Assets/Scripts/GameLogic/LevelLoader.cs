using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Voxels.Common;
using Voxels.Common.DataModels;
using Voxels.GameLogic;

namespace GameLogic
{
	public class LevelLoader : MonoBehaviour
	{
		[SerializeField] GameObject _progressBar;
		[SerializeField] Slider _slider;
		[SerializeField] Text _progressText;
		[SerializeField] Text _description;
		[SerializeField] TMP_InputField _worldSizeX;
		[SerializeField] TMP_InputField _worldSizeZ;
		[SerializeField] TextMeshProUGUI _worldSizeDescription;
		[SerializeField] TMP_InputField _seedInputField;
		[SerializeField] TextMeshProUGUI _waterLevelText;
		[SerializeField] Slider _waterSlider;
		[SerializeField] RectTransform _footer;

		GameSettings _settings = new GameSettings()
		{
			// default settings
			IsWater = true,
			WaterLevel = 30,
			SeedValue = 32000,
			TreeProbability = TreeProbability.Some,
			WorldSizeX = 3,
			WorldSizeZ = 3
		};

		int _waterLevel = 30;

		void Start()
		{
			_settings.TreeProbability = TreeProbability.Some;
			_settings.ComputingAcceleration = ComputingAcceleration.PureCSParallelisation;
			SetWorldSizeDescription();
		}

		public void NewGame()
		{
			Game.StartFromLoadGame = false;
			StartCoroutine(LoadLevelAsync(1));
		}

		public void LoadGame()
		{
			Game.StartFromLoadGame = true;
			StartCoroutine(LoadLevelAsync(1));
		}

		public void QuitGame() => Application.Quit();

		public void WorldParametersChanged()
		{
			int xInput = int.Parse(_worldSizeX.text);
			int zInput = int.Parse(_worldSizeZ.text);

			if (xInput <= 0)
			{
				xInput = 1;
				_worldSizeX.text = xInput.ToString();
			}

			if (zInput <= 0)
			{
				zInput = 1;
				_worldSizeZ.text = zInput.ToString();
			}

			_settings.WorldSizeX = xInput;
			_settings.WorldSizeZ = zInput;

			SetWorldSizeDescription();
		}

		public void RandomSeed()
		{
			var newSeed = Random.Range(1000, 1000000);
			_seedInputField.text = newSeed.ToString();
			_settings.SeedValue = newSeed;
		}

		// this crap gets called OnEnable for some reason so I had reassign it again in Start method
		public void SetTreeProbability(float value) => _settings.TreeProbability = (TreeProbability)(int)value;

		public void WaterLevelChanged(float value)
		{
			_settings.WaterLevel = (int)value;
			_waterLevelText.text = "Water Level " + _settings.WaterLevel.ToString();
		}

		public void WaterToggleChanged(bool value)
		{
			_settings.IsWater = value;

			_waterLevelText.text = _settings.IsWater
				? "Water Level " + _waterLevel.ToString()
				: "No Water";

			_waterSlider.enabled = value;
			_waterSlider.handleRect.gameObject.SetActive(value);
			_waterSlider.interactable = value;
		}

		void SetWorldSizeDescription()
		{
			int totalNumberOfCubes = _settings.WorldSizeX * Constants.WORLD_SIZE_Y * _settings.WorldSizeZ
				* Constants.CHUNK_SIZE * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE;
			int numberOfCubesInChunk = Constants.CHUNK_SIZE * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE;

			var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
			nfi.NumberGroupSeparator = " ";

			_worldSizeDescription.text = $@"World's size is measured in chunks. 
Height is always equal to 4.
Each chunk is made of {Constants.CHUNK_SIZE}^3 = { numberOfCubesInChunk.ToString("#,0", nfi) } cubes.
Total number of cubes is: <b>{ totalNumberOfCubes.ToString("#,0", nfi) }</b>";
		}

		IEnumerator LoadLevelAsync(int sceneIndex)
		{
			World.Settings = _settings;
			_footer.gameObject.SetActive(true);
			_progressBar.SetActive(true);
			_description.text = "Level loading...";

			AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

			while (!operation.isDone)
			{
				// Unity load scenes in two stages:
				// 1. Loading - loading all the stuff
				// 2. Activation - deleting all previously used stuff and replacing it with the newly loaded stuff
				// the progress value increments from 0 to 0.9 during the first stage and 0.9 to 1 during the second stage
				// isDone is set to true as soon as the loading stage is done
				// that is why we need to multiply the value by .9f before we clamp it
				float progress = Mathf.Clamp01(operation.progress / .9f); // clamps value between min (0) and max (1) and returns value.
				_slider.value = progress;
				_progressText.text = Mathf.RoundToInt(progress * 100) + "%";

				yield return null;
			}
		}
	}
}
