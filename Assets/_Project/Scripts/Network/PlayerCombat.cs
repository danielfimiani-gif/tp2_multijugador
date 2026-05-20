using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] private float damagePerHit = 12f;
    [SerializeField] private float attackCooldownSec = 0.4f;

    [Header("Hitbox")]
    [SerializeField] private Vector3 hitboxSize = new(1.5f, 1.2f, 1f);
    [SerializeField] private float hitboxDistance = 0.8f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Knockback")]
    [SerializeField] private Vector2 baseKnockback = new(4f, 6f);
    [SerializeField] private float knockbackScalePerPercent = 0.08f;

    [Networked] public float DamagePercent { get; set; }
    [Networked] public PlayerRef LastHitter { get; set; }
    [Networked] private TickTimer AttackCooldown { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private PlayerController _controller;
    private NetworkCharacterController _ncc;
    private PlayerStock _stock;

    private static readonly Collider[] _overlapBuffer = new Collider[16];

    public override void Spawned()
    {
        _controller = GetComponent<PlayerController>();
        _ncc = GetComponent<NetworkCharacterController>();
        _stock = GetComponent<PlayerStock>();

        if (HasStateAuthority)
        {
            DamagePercent = 0f;
            LastHitter = PlayerRef.None;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (_stock != null && !_stock.IsAlive) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.MatchState.InProgress) return;
        if (!GetInput<NetInput>(out var input)) return;

        var pressed = input.Buttons.GetPressed(PreviousButtons);
        PreviousButtons = input.Buttons;

        if (!pressed.IsSet((int)InputButton.Attack)) return;
        if (!AttackCooldown.ExpiredOrNotRunning(Runner)) return;

        PerformAttack();
    }

    private void PerformAttack()
    {
        AttackCooldown = TickTimer.CreateFromSeconds(Runner, attackCooldownSec);

        var facing = _controller != null ? _controller.Facing : (sbyte)1;
        var center = transform.position + new Vector3(facing * hitboxDistance, 0f, 0f);

        var count = Physics.OverlapBoxNonAlloc(
            center,
            hitboxSize * 0.5f,
            _overlapBuffer,
            Quaternion.identity,
            hitMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            var victim = _overlapBuffer[i].GetComponentInParent<PlayerCombat>();
            if (victim == null || victim == this) continue;
            ApplyHit(victim, facing);
        }
    }

    private void ApplyHit(PlayerCombat victim, sbyte attackerFacing)
    {
        if (victim._stock != null && (!victim._stock.IsAlive || victim._stock.IsInvincible))
            return;

        victim.DamagePercent += damagePerHit;
        victim.LastHitter = Object.InputAuthority;

        var scale = 1f + victim.DamagePercent * knockbackScalePerPercent;
        var kb = new Vector3(baseKnockback.x * scale * attackerFacing, baseKnockback.y * scale, 0f);

        var victimNCC = victim._ncc != null ? victim._ncc : victim.GetComponent<NetworkCharacterController>();
        if (victimNCC != null)
            victimNCC.Velocity = kb;

        RPC_OnHit(victim.Object.InputAuthority, Object.InputAuthority, damagePerHit, kb);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnHit(PlayerRef victim, PlayerRef attacker, float damage, Vector3 knockback)
    {
        Debug.Log($"[Hit] attacker={attacker} victim={victim} dmg={damage:F0}% kb={knockback}");
    }

    private void OnDrawGizmosSelected()
    {
        sbyte facing = 1;
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null && Application.isPlaying)
            facing = ctrl.Facing;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(facing * hitboxDistance, 0f, 0f), hitboxSize);
    }
}
