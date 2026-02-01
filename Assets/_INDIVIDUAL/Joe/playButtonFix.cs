using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ResetPlayButton : MonoBehaviour
{
    void OnEnable()
    {
        var button = GetComponent<Button>();
        button.interactable = true;

        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
