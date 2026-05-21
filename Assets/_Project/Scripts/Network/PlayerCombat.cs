using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Attack base")]
    [SerializeField] private float attackCooldownSec = 0.25f;
    [SerializeField] private float comboWindowSec = 0.5f;
    [SerializeField] private int maxComboSteps = 3;

    [Header("Hitbox")]
    [SerializeField] private Vector3 hitboxSize = new(1.5f, 1.2f, 1f);
    [SerializeField] private float hitboxDistance = 0.8f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Combo damage (por paso)")]
    [SerializeField] private float[] damagePerStep = { 10f, 12f, 18f };

    [Header("Combo knockback (por paso)")]
    [SerializeField] private Vector2[] baseKnockbackPerStep =
    {
        new(3f, 4f),
        new(4f, 5f),
        new(7f, 9f)
    };

    [SerializeField] private float knockbackScalePerPercent = 0.08f;

    [Header("Animation triggers")]
    [SerializeField] private string[] attackTriggers = { "Attack1", "Attack2", "Attack3" };
    [SerializeField] private string hitTrigger = "Hit";

    [Networked] public float DamagePercent { get; set; }
    [Networked] public PlayerRef LastHitter { get; set; }
    [Networked] private TickTimer AttackCooldown { get; set; }
    [Networked] private TickTimer ComboWindow { get; set; }
    [Networked] private int ComboStep { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private PlayerController _controller;
    private NetworkCharacterController _ncc;
    private PlayerStock _stock;
    private Animator _animator;
    private int[] _attackHashes;
    private int _hitHash;

    private static readonly Collider[] _overlapBuffer = new Collider[16];

    public override void Spawned()
    {
        _controller = GetComponent<PlayerController>();
        _ncc = GetComponent<NetworkCharacterController>();
        _stock = GetComponent<PlayerStock>();
        _animator = GetComponentInChildren<Animator>();

        _attackHashes = new int[attackTriggers.Length];
        for (int i = 0; i < attackTriggers.Length; i++)
            _attackHashes[i] = Animator.StringToHash(attackTriggers[i]);
        _hitHash = Animator.StringToHash(hitTrigger);

        if (HasStateAuthority)
        {
            DamagePercent = 0f;
            LastHitter = PlayerRef.None;
            ComboStep = 0;
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
        var step = ComboWindow.ExpiredOrNotRunning(Runner) ? 1 : Mathf.Min(ComboStep + 1, maxComboSteps);
        ComboStep = step;
        AttackCooldown = TickTimer.CreateFromSeconds(Runner, attackCooldownSec);
        ComboWindow = TickTimer.CreateFromSeconds(Runner, comboWindowSec);

        if (_animator != null && step - 1 < _attackHashes.Length)
            _animator.SetTrigger(_attackHashes[step - 1]);

        var stepIdx = Mathf.Clamp(step - 1, 0, damagePerStep.Length - 1);
        var damage = damagePerStep.Length > 0 ? damagePerStep[stepIdx] : 10f;
        var baseKb = baseKnockbackPerStep.Length > 0 ? baseKnockbackPerStep[Mathf.Min(stepIdx, baseKnockbackPerStep.Length - 1)] : new Vector2(4f, 6f);

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
            ApplyHit(victim, facing, damage, baseKb);
        }
    }

    private void ApplyHit(PlayerCombat victim, sbyte attackerFacing, float damage, Vector2 baseKb)
    {
        if (victim._stock != null && (!victim._stock.IsAlive || victim._stock.IsInvincible))
            return;

        victim.DamagePercent += damage;
        victim.LastHitter = Object.InputAuthority;

        var scale = 1f + victim.DamagePercent * knockbackScalePerPercent;
        var kb = new Vector3(baseKb.x * scale * attackerFacing, baseKb.y * scale, 0f);

        var victimNCC = victim._ncc != null ? victim._ncc : victim.GetComponent<NetworkCharacterController>();
        if (victimNCC != null)
            victimNCC.Velocity = kb;

        if (victim._animator != null) victim._animator.SetTrigger(victim._hitHash);

        RPC_OnHit(victim.Object.InputAuthority, Object.InputAuthority, damage, kb);
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
