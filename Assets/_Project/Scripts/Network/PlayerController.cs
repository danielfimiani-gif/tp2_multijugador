using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float airJumpImpulse = -1f;

    [Header("Visuals")]
    [Tooltip("Transform child que contiene el modelo visual (mesh/sprite). Si está asignado, se flippea en X según Facing.")]
    [SerializeField] private Transform visualRoot;

    [Header("Animation params")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private string verticalSpeedParam = "VerticalSpeed";
    [SerializeField] private string deadBoolParam = "Dead";
    [SerializeField] private string jumpTriggerParam = "Jump";

    [Networked] public int JumpsRemaining { get; private set; }
    [Networked] public sbyte Facing { get; private set; } = 1;
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private NetworkCharacterController _ncc;
    private Animator _animator;
    private PlayerStock _stock;
    private int _speedHash;
    private int _groundedHash;
    private int _verticalSpeedHash;
    private int _deadHash;
    private int _jumpHash;

    public Animator Animator => _animator;

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _stock = GetComponent<PlayerStock>();
        JumpsRemaining = maxJumps;

        _speedHash = Animator.StringToHash(speedParam);
        _groundedHash = Animator.StringToHash(groundedParam);
        _verticalSpeedHash = Animator.StringToHash(verticalSpeedParam);
        _deadHash = Animator.StringToHash(deadBoolParam);
        _jumpHash = Animator.StringToHash(jumpTriggerParam);

        CameraFollow2D.Register(transform);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        CameraFollow2D.Unregister(transform);
    }

    public override void FixedUpdateNetwork()
    {
        if (_stock != null && !_stock.IsAlive) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.MatchState.InProgress) return;
        if (!GetInput<NetInput>(out var input)) return;

        var dir = new Vector3(input.Horizontal, 0f, 0f);
        _ncc.Move(dir);

        if (input.Horizontal > 0) Facing = 1;
        else if (input.Horizontal < 0) Facing = -1;

        if (_ncc.Grounded)
            JumpsRemaining = maxJumps;

        var pressed = input.Buttons.GetPressed(PreviousButtons);
        PreviousButtons = input.Buttons;

        if (pressed.IsSet((int)InputButton.Jump) && JumpsRemaining > 0)
        {
            if (airJumpImpulse > 0f && !_ncc.Grounded)
                _ncc.Jump(ignoreGrounded: true, overrideImpulse: airJumpImpulse);
            else
                _ncc.Jump(ignoreGrounded: true);
            JumpsRemaining--;

            if (_animator != null) _animator.SetTrigger(_jumpHash);
        }

        var pos = transform.position;
        if (Mathf.Abs(pos.z) > 0.01f)
        {
            pos.z = 0f;
            transform.position = pos;
        }
    }

    public override void Render()
    {
        if (_animator != null && _ncc != null)
        {
            _animator.SetFloat(_speedHash, Mathf.Abs(_ncc.Velocity.x));
            _animator.SetFloat(_verticalSpeedHash, _ncc.Velocity.y);
            _animator.SetBool(_groundedHash, _ncc.Grounded);
            _animator.SetBool(_deadHash, _stock != null && !_stock.IsAlive);
        }

        if (visualRoot != null)
        {
            var targetY = Facing >= 0 ? 0f : 180f;
            var current = visualRoot.localEulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(current.y, targetY)) > 0.5f)
                visualRoot.localRotation = Quaternion.Euler(current.x, targetY, current.z);
        }
    }
}
