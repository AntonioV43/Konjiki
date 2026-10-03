using UnityEngine;

public class UI_BuffCards : MonoBehaviour
{
    public void Show()
    {
        UIManager.Instance.Open(gameObject);
    }

    public void SelectBuff()
    {
        // Apply buff here

        UIManager.Instance.Back();
    }
}