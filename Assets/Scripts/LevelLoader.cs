using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Assets.Scripts.World;

public class LevelLoader : MonoBehaviour
{
	[SerializeField] GameObject _progressBar;
	[SerializeField] Slider _slider;
	[SerializeField] Text _progressText;
	[SerializeField] Text _description;
	[SerializeField] InputField _worldSizeX;
	[SerializeField] InputField _worldSizeZ;
	[SerializeField] InputField _seedInputField;
	[SerializeField] Text _waterLevelText;
	[SerializeField] Slider _waterSlider;

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

	public void WorldParametersChange()
	{
		_settings.WorldSizeX = int.Parse(_worldSizeX.text);
		_settings.WorldSizeZ = int.Parse(_worldSizeZ.text);
	}

	public void RandomSeed()
	{
		var newSeed = Random.Range(10000, 1000000);
		_seedInputField.text = newSeed.ToString();
		_settings.SeedValue = newSeed;
	}

	public void SetTreeProbability(float value) => _settings.TreeProbability = (TreeProbability)(int)value;

	public void WaterLevelChanged(float value)
	{
		_settings.WaterLevel = (int)value;
		_waterLevelText.text = "Water Level" + System.Environment.NewLine + _settings.WaterLevel.ToString();
	}

	public void WaterToggleChanged(bool value)
	{
		_settings.IsWater = value;

		_waterLevelText.text = _settings.IsWater
			? "Water Level" + System.Environment.NewLine + _waterLevel.ToString()
			: "No Water";

		_waterSlider.enabled = value;
		_waterSlider.interactable = value;
	}

	IEnumerator LoadLevelAsync(int sceneIndex)
	{
        World.Settings = _settings;
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
