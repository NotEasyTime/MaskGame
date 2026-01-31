using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public sealed class RebindMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset actions;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject rebindRowPrefab;

    private const string SaveKey = "InputRebinds";
    private const string KeyboardGroup = "Keyboard&Mouse";
    private const string GamepadGroup = "Gamepad";

    private string cachedBindingJson;
    private string activeGroup;

    // ------------------------------------------------------------
    // LIFECYCLE
    // ------------------------------------------------------------
        void Awake()
    {
        activeGroup = KeyboardGroup;
    }
    void OnEnable()
    {
        LoadSavedBindings();
        CacheCurrentBindings();
        RebuildUI();
    }

    void OnDisable()
    {
        ClearUI();
    }

    // ------------------------------------------------------------
    // UI BUILD
    // ------------------------------------------------------------

    void RebuildUI()
    {
        ClearUI();
        BuildUI(activeGroup);
    }

    void BuildUI(string bindingGroup)
    {
        foreach (var map in actions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];

                    if (binding.isComposite || binding.isPartOfComposite)
                        continue;

                    if (string.IsNullOrEmpty(binding.groups))
                        continue;

                    if (string.IsNullOrEmpty(binding.groups) || !binding.groups.Contains(bindingGroup))
                        continue;


                    CreateRow(action, i);
                }
            }
        }
    }


    void ClearUI()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }

    void CreateRow(InputAction action, int bindingIndex)
    {
        var row = Instantiate(rebindRowPrefab, contentRoot);

        var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
        var button = row.GetComponentInChildren<Button>();

        texts[0].text = action.name;
        texts[1].text = action.GetBindingDisplayString(bindingIndex);

        button.onClick.AddListener(() =>
        {
            StartRebind(action, bindingIndex, texts[1]);
        });
    }

    // ------------------------------------------------------------
    // REBINDING WITH SWAP
    // ------------------------------------------------------------

    void StartRebind(InputAction action, int bindingIndex, TextMeshProUGUI label)
    {
        label.text = "...";

        action.Disable();

        string sourceOldPath = action.bindings[bindingIndex].effectivePath;
        string sourceGroup = action.bindings[bindingIndex].groups;

        action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(op =>
            {
                op.Dispose();
                action.Enable();

                string newPath = action.bindings[bindingIndex].effectivePath;

                if (string.IsNullOrEmpty(newPath) || newPath == sourceOldPath)
                {
                    label.text = action.GetBindingDisplayString(bindingIndex);
                    return;
                }

                if (TrySwapDuplicateBinding(
                    action,
                    bindingIndex,
                    sourceOldPath,
                    newPath,
                    sourceGroup))
                {
                    RebuildUI();
                }
                else
                {
                    label.text = action.GetBindingDisplayString(bindingIndex);
                }
            })
            .Start();
    }


    bool TrySwapDuplicateBinding(
        InputAction sourceAction,
        int sourceIndex,
        string sourceOldPath,
        string newPath,
        string bindingGroup
    )
    {
        foreach (var map in actions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action == sourceAction && i == sourceIndex)
                        continue;

                    var binding = action.bindings[i];

                    if (binding.isComposite || binding.isPartOfComposite)
                        continue;

                    if (binding.groups != bindingGroup)
                        continue;

                    if (binding.effectivePath != newPath)
                        continue;

                    // Swap
                    sourceAction.ApplyBindingOverride(sourceIndex, newPath);
                    action.ApplyBindingOverride(i, sourceOldPath);
                    return true;
                }
            }
        }

        return false;
    }


    // ------------------------------------------------------------
    // FILTER BUTTONS
    // ------------------------------------------------------------

    public void ShowKeyboard()
    {
        activeGroup = KeyboardGroup;
        RebuildUI();
    }

    public void ShowGamepad()
    {
        activeGroup = GamepadGroup;
        RebuildUI();
    }

    // ------------------------------------------------------------
    // APPLY / CANCEL
    // ------------------------------------------------------------

    public void Apply()
    {
        PlayerPrefs.SetString(SaveKey, actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        CacheCurrentBindings();
    }

    public void Cancel()
    {
        if (!string.IsNullOrEmpty(cachedBindingJson))
        {
            actions.RemoveAllBindingOverrides();
            actions.LoadBindingOverridesFromJson(cachedBindingJson);
        }

        gameObject.SetActive(false);
    }
        
    // ------------------------------------------------------------
    // SAVE / LOAD
    // ------------------------------------------------------------

    void CacheCurrentBindings()
    {
        cachedBindingJson = actions.SaveBindingOverridesAsJson();
    }

    void LoadSavedBindings()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return;

        actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(SaveKey));
    }

    public void ResetDefaults()
    {
        actions.RemoveAllBindingOverrides();
        RebuildUI();
    }

}
