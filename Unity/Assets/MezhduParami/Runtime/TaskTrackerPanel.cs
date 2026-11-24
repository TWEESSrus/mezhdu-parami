using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MezhduParami
{
    // This view has no knowledge of calendar, locations or quest rules.
    public sealed class TaskTrackerPanel : MonoBehaviour
    {
        public GameObject Panel;
        public TMP_Text Entries;
        public TMP_Text Caption;
        public UnityEngine.UI.Button ToggleButton;
        public bool Visible => Panel != null && Panel.activeSelf;
        private void Awake() { if (ToggleButton != null) ToggleButton.onClick.AddListener(Toggle); }
        private void Update() { if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) Toggle(); }
        public void Toggle() => Panel.SetActive(!Panel.activeSelf);
        public void SetEntries(string text, int completed, int total)
        {
            Entries.text = text;
            Caption.text = $"Задачи {completed}/{total}   [T]";
        }
    }
}
