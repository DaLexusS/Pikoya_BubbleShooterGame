using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class SceneTransitionManager : MonoBehaviour
{
    private const string LoadingScreenResourceName = "LoadingScreen";
    private const string AudioManagerResourceName = "AudioManager";

    public static bool IsSceneReady { get; private set; }
    public static bool IsTransitioning => instance != null && instance.isTransitioning;
    public static event Action SceneReady;

    private static SceneTransitionManager instance;

    private LoadingScreenView view;
    private bool isTransitioning;
    private bool waitingForInitialScene;
    private float previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(SceneTransitionManager));
        DontDestroyOnLoad(managerObject);
        SceneTransitionManager manager = managerObject.AddComponent<SceneTransitionManager>();

        BootstrapAudio();

        GameObject loadingPrefab = Resources.Load<GameObject>(LoadingScreenResourceName);
        if (loadingPrefab == null)
        {
            Debug.LogError("Resources/LoadingScreen prefab was not found.");
            IsSceneReady = true;
            return;
        }

        GameObject loadingObject = Instantiate(loadingPrefab);
        DontDestroyOnLoad(loadingObject);
        manager.Initialize(loadingObject.GetComponent<LoadingScreenView>());
    }

    private static void BootstrapAudio()
    {
        if (AudioManager.Instance != null)
        {
            return;
        }

        GameObject audioPrefab = Resources.Load<GameObject>(AudioManagerResourceName);
        if (audioPrefab == null)
        {
            Debug.LogError("Resources/AudioManager prefab was not found.");
            return;
        }

        GameObject audioObject = Instantiate(audioPrefab);
        AudioManager audioManager = audioObject.GetComponent<AudioManager>();

        if (audioManager == null)
        {
            Debug.LogError("Resources/AudioManager prefab has no AudioManager component.");
            Destroy(audioObject);
            return;
        }

        audioManager.Init();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Initialize(LoadingScreenView loadingView)
    {
        view = loadingView;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsSceneReady = false;
        isTransitioning = true;
        waitingForInitialScene = true;
        view?.ShowOpaque();
    }

    public static void LoadScene(string sceneName)
    {
        if (instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        instance.BeginTransition(() => SceneManager.LoadSceneAsync(sceneName));
    }

    public static void LoadScene(int buildIndex)
    {
        if (instance == null)
        {
            SceneManager.LoadScene(buildIndex);
            return;
        }

        instance.BeginTransition(() => SceneManager.LoadSceneAsync(buildIndex));
    }

    private void BeginTransition(Func<AsyncOperation> beginLoading)
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionScene(beginLoading));
    }

    private IEnumerator TransitionScene(Func<AsyncOperation> beginLoading)
    {
        isTransitioning = true;
        IsSceneReady = false;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (view != null)
        {
            yield return view.FadeToBlack();
            yield return view.RenderOpaqueFrame();
        }

        AsyncOperation loading = beginLoading();
        if (loading == null)
        {
            CompleteTransition();
            yield break;
        }

        while (!loading.isDone)
        {
            yield return null;
        }

        if (view != null)
        {
            yield return view.HoldOpaqueBeforeReveal();
            yield return view.FadeFromBlack();
        }

        CompleteTransition();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!waitingForInitialScene)
        {
            return;
        }

        waitingForInitialScene = false;
        StartCoroutine(RevealInitialScene());
    }

    private IEnumerator RevealInitialScene()
    {
        yield return null;

        if (view != null)
        {
            yield return view.FadeFromBlack();
        }

        CompleteTransition();
    }

    private void CompleteTransition()
    {
        Time.timeScale = previousTimeScale;
        isTransitioning = false;
        IsSceneReady = true;
        SceneReady?.Invoke();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }
}
