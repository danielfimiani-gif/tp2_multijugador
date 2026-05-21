using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPreview : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Image previewImage;
    [SerializeField] private Sprite[] characterSprites;

    private void Start()
    {
        if (dropdown == null)
        {
            Debug.LogWarning("[CharacterPreview] Dropdown no asignado en el Inspector.");
            return;
        }

        var maxIdx = Mathf.Max(0, dropdown.options.Count - 1);
        var saved = Mathf.Clamp(PlayerCharacterSelection.SelectedIndex, 0, maxIdx);
        dropdown.SetValueWithoutNotify(saved);

        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        UpdatePreview(saved);
    }

    private void OnDestroy()
    {
        if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int idx)
    {
        PlayerCharacterSelection.SelectedIndex = idx;
        UpdatePreview(idx);
    }

    private void UpdatePreview(int idx)
    {
        if (previewImage == null || characterSprites == null || characterSprites.Length == 0) return;

        idx = Mathf.Clamp(idx, 0, characterSprites.Length - 1);
        previewImage.sprite = characterSprites[idx];
        previewImage.enabled = previewImage.sprite != null;
    }
}
