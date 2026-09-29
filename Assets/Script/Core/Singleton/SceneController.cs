using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    void Awake()
    {
        if(GameManager.Instance == null)
        {
            SceneManager.LoadScene("BOOTSTRAP", LoadSceneMode.Single);
        }
    }

    void Start()
    {
        
    }
}
