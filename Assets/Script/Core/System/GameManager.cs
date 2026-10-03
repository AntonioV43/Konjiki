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
}
