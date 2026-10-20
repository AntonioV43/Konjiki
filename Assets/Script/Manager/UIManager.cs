using System.Collections.Generic;
using UnityEngine;

public enum UIType
{
    PlayerHUD,
    PauseMenu,
    BuffCards,
    Inventory,
    Shop
}

public class UIManager : Singleton<UIManager>
{
    [System.Serializable]
    private class UIEntry
    {
        public UIType type;
        public MonoBehaviour controller;
    }

    [SerializeField] private List<UIEntry> uiEntries = new();

    private Dictionary<UIType, IUIController> uiDictionary;

    protected override void Awake()
    {
        base.Awake();

        uiDictionary = new Dictionary<UIType, IUIController>();

        foreach (UIEntry entry in uiEntries)
        {
            if (entry.controller == null)
                continue;

            if (entry.controller is IUIController controller)
            {
                uiDictionary[entry.type] = controller;
            }
            else
            {
                Debug.LogError(
                    $"{entry.controller.name} harus implement IUIController."
                );
            }
        }
    }

    public void Open(UIType type)
    {
        if (!uiDictionary.TryGetValue(type, out IUIController ui))
        {
            Debug.LogWarning($"UI {type} tidak ditemukan.");
            return;
        }

        ui.Show();
    }

    public void Close(UIType type)
    {
        if (!uiDictionary.TryGetValue(type, out IUIController ui))
            return;

        ui.Hide();
    }

    public void Toggle(UIType type)
    {
        if (!uiDictionary.TryGetValue(type, out IUIController ui))
            return;

        ui.Show();
    }

    public IUIController Get(UIType type)
    {
        uiDictionary.TryGetValue(type, out IUIController ui);
        return ui;
    }
}