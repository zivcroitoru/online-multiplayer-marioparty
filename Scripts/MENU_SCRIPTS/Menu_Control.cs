using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class GameLogic : MonoBehaviour
{
    public enum GameScreens
    {
        MainMenu, Loading, Options, Student, Multiplayer, Multi_Connect
    };
    public GameObject Menu;
    private Dictionary<string, GameObject> _unityObjects;
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.5f;
    public MenuLogic cuMenuLogic;
    private static GameLogic instance;
    public static GameLogic Instance
    {
        get
        {
            if (instance == null)
                instance = GameObject.Find("GameLogic").GetComponent<GameLogic>();
            return instance;
        }
    }

    private GameScreens _currentScreen;
    void Awake()
    {
        _currentScreen = GameScreens.MainMenu;
        _unityObjects = new Dictionary<string, GameObject>();
        GameObject[] curGameObjects = GameObject.FindGameObjectsWithTag("UnityObject");

        foreach (GameObject g in curGameObjects)
        {
            _unityObjects.Add(g.name, g);
        }

        // Deactivate all screens at the start except the main menu
        SetAllScreensInactive();
        if (_unityObjects.ContainsKey("Screen_MainMenu"))
        {
            _unityObjects["Screen_MainMenu"].SetActive(true);
        }

    }

    public void Btn_SinglePlayerLogic()
    {
        StartCoroutine(ChangeScreenWithFade(GameScreens.Loading, 1f)); // Show loading screen with delay
        StartCoroutine(LoadSinglePlayerSceneAfterDelay(4f));           // Load scene after delay
    }


    // Coroutine to load the SinglePlayer scene after a delay
    private IEnumerator LoadSinglePlayerSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);                        // Wait for the delay
        SceneManager.LoadScene("SinglePlayer");                        // Load the SinglePlayer scene
    }

    public void Btn_Mult_CONNECT()
    {
        StartCoroutine(ChangeScreenWithFade(GameScreens.Multi_Connect));
        
    }
    public void Btn_Play()
    {
        cuMenuLogic.Btn_Play();
    }

    public void Btn_OptionsPlayerLogic()
    {
        StartCoroutine(ChangeScreenWithFade(GameScreens.Options));
    }

    public void Btn_StudentPlayerLogic()
    {
        StartCoroutine(ChangeScreenWithFade(GameScreens.Student));
    }

    public void Btn_MultiplayerLogic()
    {
        StartCoroutine(MultiplayerFlow());
    }

    private IEnumerator MultiplayerFlow()
    {
        // Show the loading screen and wait for 1 second
        yield return StartCoroutine(ChangeScreenWithFade(GameScreens.Loading, 1f));

        // After 1 second, switch to the multiplayer screen
        yield return StartCoroutine(ChangeScreenWithFade(GameScreens.Multiplayer));
    }

    public void Btn_BackLogic()
    {
        StartCoroutine(ChangeScreenWithFade(GameScreens.MainMenu));
    }

    public void OpenLink(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
    }

    public IEnumerator ChangeScreenWithFade(GameScreens newScreen, float delay = 0)
    {
        // Disable the EventSystem to block all input
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.enabled = false;
        }

        // Fade out
        yield return StartCoroutine(Fade(1));

        // Wait for delay (if exists)
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Switch screens
        if (_unityObjects.ContainsKey("Screen_" + _currentScreen))
        {
            _unityObjects["Screen_" + _currentScreen].SetActive(false);
        }

        _currentScreen = newScreen;

        if (_unityObjects.ContainsKey("Screen_" + _currentScreen))
        {
            _unityObjects["Screen_" + _currentScreen].SetActive(true);
        }

        // Fade in
        yield return StartCoroutine(Fade(0));

        // Re-enable the EventSystem
        if (eventSystem != null)
        {
            eventSystem.enabled = true;
        }
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void SetAllScreensInactive()
    {
        foreach (var screen in _unityObjects)
        {
            screen.Value.SetActive(false);
        }
    }
}
