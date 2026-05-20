using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameLabel;
    [SerializeField] private TMP_Text playerCountLabel;
    [SerializeField] private Button selectButton;

    private string _roomName;
    private Action<string> _onSelect;

    public void Bind(SessionInfo session, Action<string> onSelect)
    {
        _roomName = session.Name;
        _onSelect = onSelect;

        if (roomNameLabel) roomNameLabel.text = session.Name;
        if (playerCountLabel) playerCountLabel.text = $"{session.PlayerCount}/{session.MaxPlayers}";

        if (selectButton)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onSelect?.Invoke(_roomName));
        }
    }
}
