using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private PlayerShooter playerShooter;

    private bool isInitialized;

    private void Awake()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        if (isInitialized)
        {
            return;
        }

        mapLoader.Initialize();
        playerShooter.Initialize();
        isInitialized = true;
    }
}
