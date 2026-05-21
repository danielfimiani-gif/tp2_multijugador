using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [Header("Global")]
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private TMP_Text stateLabel;

    [Header("Player Slots (max 4)")]
    [SerializeField] private HudPlayerSlot[] slots = new HudPlayerSlot[4];

    [Header("End Game")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text winnerLabel;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private readonly List<PlayerRef> _sortedPlayers = new();
    private NetworkRunnerController _runner;
    private string _overrideStateText;

    private void Start()
    {
        _runner = NetworkRunnerController.Instance;
        if (_runner != null) _runner.OnError += HandleRunnerError;

        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenu);

        foreach (var slot in slots)
            if (slot != null) slot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_runner != null) _runner.OnError -= HandleRunnerError;
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        UpdateTimer(gm);
        UpdateStateLabel(gm);
        UpdateSlots(gm);
        UpdateEndGame(gm);
    }

    private void HandleRunnerError(string message)
    {
        _overrideStateText = message;
    }

    private void UpdateTimer(GameManager gm)
    {
        if (timerLabel == null) return;

        float seconds = 0f;
        switch (gm.State)
        {
            case MatchState.Countdown:
                seconds = gm.CountdownTimer.RemainingTime(gm.Runner) ?? 0f;
                break;
            case MatchState.InProgress:
                seconds = gm.MatchTimer.RemainingTime(gm.Runner) ?? 0f;
                break;
        }

        var m = Mathf.FloorToInt(seconds / 60f);
        var s = Mathf.FloorToInt(seconds % 60f);
        timerLabel.text = $"{m:0}:{s:00}";
    }

    private void UpdateStateLabel(GameManager gm)
    {
        if (stateLabel == null) return;

        if (!string.IsNullOrEmpty(_overrideStateText))
        {
            stateLabel.text = _overrideStateText;
            return;
        }

        switch (gm.State)
        {
            case MatchState.WaitingForPlayers:
                stateLabel.text = $"Esperando jugadores... ({gm.ActivePlayers.Count}/{gm.MinPlayers})";
                break;
            case MatchState.Countdown:
                stateLabel.text = "Preparate!";
                break;
            case MatchState.InProgress:
                stateLabel.text = "";
                break;
            case MatchState.Ended:
                stateLabel.text = "Fin del match";
                break;
        }
    }

    private void UpdateSlots(GameManager gm)
    {
        var runner = gm.Runner;
        if (runner == null) return;

        _sortedPlayers.Clear();
        foreach (var p in runner.ActivePlayers) _sortedPlayers.Add(p);
        _sortedPlayers.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));

        int idx = 0;
        for (; idx < _sortedPlayers.Count && idx < slots.Length; idx++)
        {
            var slot = slots[idx];
            if (slot == null) continue;

            slot.SetActive(true);
            slot.SetName($"P{idx + 1}");

            var player = _sortedPlayers[idx];
            var obj = runner.GetPlayerObject(player);

            if (obj == null)
            {
                slot.ClearStats();
                continue;
            }

            var combat = obj.GetComponent<PlayerCombat>();
            var stock = obj.GetComponent<PlayerStock>();

            slot.SetDamagePercent(combat != null ? combat.DamagePercent : 0f);
            slot.SetLives(stock != null ? stock.Lives : 0);
            gm.Kos.TryGet(player, out var kos);
            slot.SetKos(kos);
        }

        for (; idx < slots.Length; idx++)
        {
            if (slots[idx] != null) slots[idx].SetActive(false);
        }
    }

    private void UpdateEndGame(GameManager gm)
    {
        if (endGamePanel == null) return;
        var ended = gm.State == MatchState.Ended;
        if (endGamePanel.activeSelf != ended)
            endGamePanel.SetActive(ended);

        if (ended && winnerLabel != null)
        {
            if (!gm.Winner.IsRealPlayer)
            {
                winnerLabel.text = "Empate / sin ganador";
            }
            else
            {
                var displayIdx = _sortedPlayers.IndexOf(gm.Winner);
                winnerLabel.text = displayIdx >= 0
                    ? $"Ganador: P{displayIdx + 1}"
                    : $"Ganador: P{gm.Winner.RawEncoded}";
            }
        }
    }

    private async void OnBackToMenu()
    {
        if (backToMenuButton != null) backToMenuButton.interactable = false;

        if (_runner != null)
            await _runner.LeaveSession();

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
