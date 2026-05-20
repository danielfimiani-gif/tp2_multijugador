using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float airJumpImpulse = -1f;

    [Header("Animation params")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "Grounded";

    [Networked] public int JumpsRemaining { get; private set; }
    [Networked] public sbyte Facing { get; private set; } = 1;
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private NetworkCharacterController _ncc;
    private Animator _animator;
    private PlayerStock _stock;

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _stock = GetComponent<PlayerStock>();
        JumpsRemaining = maxJumps;
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
            _animator.SetFloat(speedParam, Mathf.Abs(_ncc.Velocity.x));
            _animator.SetBool(groundedParam, _ncc.Grounded);
        }
    }
}
