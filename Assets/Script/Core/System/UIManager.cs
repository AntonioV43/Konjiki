using System;
using UnityEngine;
using System.Collections.Generic;

public class UIManager : Singleton<UIManager>
{
    private Stack<GameObject> uiStack = new Stack<GameObject>();

    public void Open(GameObject ui)
    {
        if (uiStack.Count > 0)
        {
            uiStack.Peek().SetActive(false);
        }
        //Add new UI
        uiStack.Push(ui);

        //Show new UI
        ui.SetActive(true);
    }


    public void Back()
    {
        //Check if there is any UI to go back to
        if (uiStack.Count <= 0) return;

        //Hide current UI
        GameObject currentUI = uiStack.Pop();
        currentUI.SetActive(false);

        //Show previous UI
        GameObject previousUI = uiStack.Peek();
        previousUI.SetActive(true);
    }

    public void CloseAll()
    {
        while (uiStack.Count > 0)
        {
            GameObject ui = uiStack.Pop();
            ui.SetActive(false);
        }
    }

    public GameObject GetCurrentUI()
    {
        if(uiStack.Count ==0)return null;
        return uiStack.Peek();
    }

}