using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    override protected void Awake()
    {
        base.Awake();
        
    }

    void Start()
    {
        SceneManager.LoadScene("Main", LoadSceneMode.Additive);
    }

    public void PauseGame(bool pause)
    {
        if (pause)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}
