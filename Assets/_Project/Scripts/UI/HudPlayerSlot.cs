using TMPro;
using UnityEngine;

public class HudPlayerSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text damageLabel;
    [SerializeField] private TMP_Text livesLabel;
    [SerializeField] private TMP_Text kosLabel;

    public void SetActive(bool active)
    {
        if (gameObject.activeSelf != active) gameObject.SetActive(active);
    }

    public void SetName(string text)
    {
        if (nameLabel != null) nameLabel.text = text;
    }

    public void SetDamagePercent(float percent)
    {
        if (damageLabel != null) damageLabel.text = $"{Mathf.RoundToInt(percent)}%";
    }

    public void SetLives(int lives)
    {
        if (livesLabel != null) livesLabel.text = $"Vidas: {Mathf.Max(0, lives)}";
    }

    public void SetKos(int kos)
    {
        if (kosLabel != null) kosLabel.text = $"KOs: {kos}";
    }

    public void ClearStats()
    {
        if (damageLabel != null) damageLabel.text = "0%";
        if (livesLabel != null) livesLabel.text = "Vidas: -";
        if (kosLabel != null) kosLabel.text = "KOs: 0";
    }
}
