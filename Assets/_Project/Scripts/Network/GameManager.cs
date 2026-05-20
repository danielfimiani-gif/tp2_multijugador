using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public enum MatchState : byte
    {
        WaitingForPlayers = 0,
        Countdown = 1,
        InProgress = 2,
        Ended = 3
    }

    public static GameManager Instance { get; private set; }

    [Header("Match Settings")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private float matchDurationSeconds = 180f;
    [SerializeField] private float countdownSeconds = 3f;

    [Header("Player Spawn")]
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Networked] public MatchState State { get; set; }
    [Networked] public TickTimer MatchTimer { get; set; }
    [Networked] public TickTimer CountdownTimer { get; set; }
    [Networked] public PlayerRef Winner { get; private set; }
    [Networked, Capacity(8)] public NetworkDictionary<PlayerRef, int> Kos => default;

    public int MinPlayers => minPlayers;
    public float MatchDurationSeconds => matchDurationSeconds;

    public Vector3 GetRespawnPoint(int playerIndex)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Mathf.Abs(playerIndex) % spawnPoints.Length].position;
        return new Vector3(0f, 2f, 0f);
    }

    private readonly List<PlayerRef> _activePlayers = new();
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    public IReadOnlyList<PlayerRef> ActivePlayers => _activePlayers;
    public IReadOnlyDictionary<PlayerRef, NetworkObject> SpawnedPlayers => _spawnedPlayers;

    public override void Spawned()
    {
        Instance = this;

        if (NetworkRunnerController.Instance != null)
        {
            NetworkRunnerController.Instance.PlayerJoined += HandlePlayerJoined;
            NetworkRunnerController.Instance.PlayerLeft += HandlePlayerLeft;
        }

        if (HasStateAuthority)
        {
            State = MatchState.WaitingForPlayers;
            foreach (var player in Runner.ActivePlayers)
                HandlePlayerJoined(player);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (NetworkRunnerController.Instance != null)
        {
            NetworkRunnerController.Instance.PlayerJoined -= HandlePlayerJoined;
            NetworkRunnerController.Instance.PlayerLeft -= HandlePlayerLeft;
        }
        if (Instance == this) Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (State)
        {
            case MatchState.Countdown:
                if (CountdownTimer.Expired(Runner))
                {
                    State = MatchState.InProgress;
                    MatchTimer = TickTimer.CreateFromSeconds(Runner, matchDurationSeconds);
                    Debug.Log("[GameManager] Match started");
                }
                break;

            case MatchState.InProgress:
                CheckEndConditions();
                break;
        }
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        if (_activePlayers.Contains(player)) return;

        _activePlayers.Add(player);
        TrySpawnPlayerCharacter(player);
        TryStartMatch();
    }

    private void HandlePlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        if (_spawnedPlayers.TryGetValue(player, out var obj))
        {
            if (obj) Runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
        _activePlayers.Remove(player);
    }

    private void TrySpawnPlayerCharacter(PlayerRef player)
    {
        if (_spawnedPlayers.ContainsKey(player)) return;
        if (!playerPrefab.IsValid)
        {
            Debug.LogError("[GameManager] playerPrefab not assigned");
            return;
        }

        var idx = _activePlayers.IndexOf(player);
        Vector3 pos = spawnPoints != null && spawnPoints.Length > 0
            ? spawnPoints[idx % spawnPoints.Length].position
            : new Vector3(idx * 2f, 1f, 0f);

        var obj = Runner.Spawn(playerPrefab, pos, Quaternion.identity, player);
        _spawnedPlayers[player] = obj;
        Runner.SetPlayerObject(player, obj);
    }

    private void TryStartMatch()
    {
        if (State != MatchState.WaitingForPlayers) return;
        if (_activePlayers.Count < minPlayers) return;

        State = MatchState.Countdown;
        CountdownTimer = TickTimer.CreateFromSeconds(Runner, countdownSeconds);
        Debug.Log($"[GameManager] Countdown started ({countdownSeconds}s)");
    }

    private void CheckEndConditions()
    {
        if (MatchTimer.Expired(Runner))
        {
            EndMatch(SelectWinnerByLives());
            return;
        }

        int aliveCount = 0;
        PlayerRef lastAlive = PlayerRef.None;
        foreach (var kvp in _spawnedPlayers)
        {
            var obj = kvp.Value;
            if (obj == null) continue;
            var stock = obj.GetComponent<PlayerStock>();
            if (stock != null && !stock.IsEliminated)
            {
                aliveCount++;
                lastAlive = kvp.Key;
            }
        }

        if (aliveCount <= 1)
            EndMatch(lastAlive);
    }

    private PlayerRef SelectWinnerByLives()
    {
        PlayerRef best = PlayerRef.None;
        int bestLives = -1;
        float bestPercent = float.MaxValue;

        foreach (var kvp in _spawnedPlayers)
        {
            var obj = kvp.Value;
            if (obj == null) continue;
            var stock = obj.GetComponent<PlayerStock>();
            var combat = obj.GetComponent<PlayerCombat>();
            if (stock == null) continue;

            int lives = Mathf.Max(0, stock.Lives);
            float pct = combat != null ? combat.DamagePercent : 0f;

            if (lives > bestLives || (lives == bestLives && pct < bestPercent))
            {
                best = kvp.Key;
                bestLives = lives;
                bestPercent = pct;
            }
        }
        return best;
    }

    private void EndMatch(PlayerRef winner)
    {
        State = MatchState.Ended;
        Winner = winner;
        Debug.Log($"[GameManager] Match ended. Winner: {winner}");
        RPC_OnMatchEnd(winner);
    }

    public void RegisterKO(PlayerRef killer, PlayerRef victim)
    {
        if (!HasStateAuthority) return;
        if (killer == PlayerRef.None) return;

        Kos.TryGet(killer, out var current);
        Kos.Set(killer, current + 1);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnMatchEnd(PlayerRef winner)
    {
        Debug.Log($"[Match End] Winner: {winner}");
    }
}
