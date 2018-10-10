using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelLoader : MonoBehaviour
{
    [SerializeField] GameObject _progressBar;
    [SerializeField] Slider _slider;
    [SerializeField] Text _progressText;
    [SerializeField] Text _description;
    [SerializeField] InputField _worldSizeX;
    [SerializeField] InputField _worldSizeZ;

    public void LoadLevel(int sceneIndex)
    {
        _progressBar.SetActive(true);
        _description.text = "Level loading...";
        StartCoroutine(LoadLevelAsync(1));
    }

    public void QuitGame() => Application.Quit();

    IEnumerator LoadLevelAsync(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        Scenes.parameters = parameters;

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
