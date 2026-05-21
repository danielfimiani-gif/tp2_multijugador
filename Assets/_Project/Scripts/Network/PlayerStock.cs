using Fusion;
using UnityEngine;

public class PlayerStock : NetworkBehaviour
{
    [Header("Stock")]
    [SerializeField] private int initialLives = 3;
    [SerializeField] private float respawnDelaySec = 1.5f;
    [SerializeField] private float invincibilityDurationSec = 1.5f;

    [Header("Kill zone")]
    [SerializeField] private float killHeight = -10f;

    private NetworkCharacterController _ncc;
    private PlayerCombat _combat;
    private Renderer[] _renderers;
    private Vector3 _deadPosition;

    [Networked] private TickTimer RespawnTimer { get; set; }
    [Networked] private TickTimer InvincibilityTimer { get; set; }

    [Networked] public int Lives { get; private set; }
    [Networked] public NetworkBool IsAlive { get; private set; }

    public bool IsInvincible => !InvincibilityTimer.ExpiredOrNotRunning(Runner);
    public bool IsEliminated => Lives <= 0 && !IsAlive;

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _combat = GetComponent<PlayerCombat>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _deadPosition = new Vector3(Object.InputAuthority.RawEncoded * 2f, 100f, 0f);

        if (HasStateAuthority)
        {
            Lives = initialLives;
            IsAlive = true;
            InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, invincibilityDurationSec);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!IsAlive)
        {
            if (transform.position != _deadPosition)
                transform.position = _deadPosition;
            if (_ncc != null && _ncc.Velocity != Vector3.zero)
                _ncc.Velocity = Vector3.zero;

            if (Lives > 0 && RespawnTimer.Expired(Runner))
            {
                var gm = GameManager.Instance;
                if (gm == null || gm.State == MatchState.InProgress)
                    Respawn();
            }
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.State != MatchState.InProgress)
            return;

        if (transform.position.y < killHeight)
            HandleKO();
    }

    public override void Render()
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].enabled = IsAlive;
        }
    }

    private void HandleKO()
    {
        var killer = _combat != null ? _combat.LastHitter : PlayerRef.None;
        Lives--;
        IsAlive = false;

        transform.position = _deadPosition;
        if (_ncc != null) _ncc.Velocity = Vector3.zero;

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterKO(killer, Object.InputAuthority);

        RPC_OnKO(Object.InputAuthority, killer);

        if (Lives > 0)
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelaySec);
    }

    private void Respawn()
    {
        var idx = Object.InputAuthority.RawEncoded;
        var pos = GameManager.Instance != null
            ? GameManager.Instance.GetRespawnPoint(idx)
            : new Vector3(0f, 4f, 0f);

        transform.position = pos;
        if (_ncc != null) _ncc.Velocity = Vector3.zero;
        if (_combat != null) _combat.DamagePercent = 0f;

        IsAlive = true;
        InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, invincibilityDurationSec);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnKO(PlayerRef victim, PlayerRef killer)
    {
        Debug.Log($"[KO] killer={killer} victim={victim}");
    }
}
