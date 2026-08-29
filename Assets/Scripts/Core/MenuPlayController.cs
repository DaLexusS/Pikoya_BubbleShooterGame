using UnityEngine;
using UnityEngine.UI;

public sealed class MenuPlayController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private string levelSceneName = "Level1";

    private void Awake()
    {
        playButton ??= GetComponent<Button>();
        playButton?.onClick.AddListener(Play);
    }

    private void Play()
    {
        if (SceneTransitionManager.IsTransitioning)
        {
            return;
        }

        if (playButton != null)
        {
            playButton.interactable = false;
        }

        SceneTransitionManager.LoadScene(levelSceneName);
    }

    private void OnDestroy()
    {
        playButton?.onClick.RemoveListener(Play);
    }
}
