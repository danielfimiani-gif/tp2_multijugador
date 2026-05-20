using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkRunner))]
[RequireComponent(typeof(NetworkSceneManagerDefault))]
public class NetworkRunnerController : MonoBehaviourSingleton<NetworkRunnerController>, INetworkRunnerCallbacks
{
    [SerializeField] private string lobbyName = "DefaultLobby";
    [SerializeField] private SceneRef gameSceneRef;
    [SerializeField] private int maxPlayerPerRoom = 4;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string attackActionName = "Attack";

    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _sceneManager;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _attackAction;
    private bool _jumpQueued;
    private bool _attackQueued;

    private bool _isLeavingVoluntarily;
    private bool _handledDisconnect;

    public NetworkRunner Runner => _runner;
    public int MaxPlayersPerRoom => maxPlayerPerRoom;

    public event Action<List<SessionInfo>> SessionListUpdated;
    public event Action OnJoinedLobby;
    public event Action<string> OnError;
    public event Action<PlayerRef> PlayerJoined;
    public event Action<PlayerRef> PlayerLeft;

    protected override void OnAwaken()
    {
        _runner = GetComponent<NetworkRunner>();
        _sceneManager = GetComponent<NetworkSceneManagerDefault>();

        if (_runner == null) Debug.LogError("[NetworkRunnerController] Missing NetworkRunner component");
        if (_sceneManager == null) Debug.LogError("[NetworkRunnerController] Missing NetworkSceneManagerDefault component");

        if (_runner != null) _runner.AddCallbacks(this);

        InitInputActions();
    }

    private void OnDestroy()
    {
        if (_jumpAction != null) _jumpAction.performed -= OnJumpPerformed;
        if (_attackAction != null) _attackAction.performed -= OnAttackPerformed;

        _moveAction?.Disable();
        _jumpAction?.Disable();
        _attackAction?.Disable();
    }

    private void InitInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("[NetworkRunnerController] Input Actions asset not assigned");
            return;
        }

        var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogError($"[NetworkRunnerController] Action map '{actionMapName}' not found in {inputActions.name}");
            return;
        }

        _moveAction = map.FindAction(moveActionName, throwIfNotFound: false);
        _jumpAction = map.FindAction(jumpActionName, throwIfNotFound: false);
        _attackAction = map.FindAction(attackActionName, throwIfNotFound: false);

        if (_moveAction == null) Debug.LogError($"[NetworkRunnerController] Action '{moveActionName}' not found");
        if (_jumpAction == null) Debug.LogError($"[NetworkRunnerController] Action '{jumpActionName}' not found");
        if (_attackAction == null) Debug.LogError($"[NetworkRunnerController] Action '{attackActionName}' not found");

        if (_jumpAction != null) _jumpAction.performed += OnJumpPerformed;
        if (_attackAction != null) _attackAction.performed += OnAttackPerformed;

        map.Enable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext _) => _jumpQueued = true;
    private void OnAttackPerformed(InputAction.CallbackContext _) => _attackQueued = true;

    public async Task JoinLobby()
    {
        if (_runner == null) return;
        var result = await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);
        if (result.Ok) OnJoinedLobby?.Invoke();
        else OnError?.Invoke(result.ErrorMessage);
    }

    public async Task CreateRoom(string roomName)
    {
        if (_runner == null || _sceneManager == null) return;
        _handledDisconnect = false;
        _isLeavingVoluntarily = false;

        var args = new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = maxPlayerPerRoom,
            CustomLobbyName = lobbyName,
            Scene = gameSceneRef,
            SceneManager = _sceneManager
        };

        var result = await _runner.StartGame(args);
        if (!result.Ok) OnError?.Invoke(result.ErrorMessage);
    }

    public async Task JoinRoom(string roomName)
    {
        if (_runner == null || _sceneManager == null) return;
        _handledDisconnect = false;
        _isLeavingVoluntarily = false;

        var args = new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            CustomLobbyName = lobbyName,
            SceneManager = _sceneManager
        };

        var result = await _runner.StartGame(args);
        if (!result.Ok) OnError?.Invoke(result.ErrorMessage);
    }

    public async Task LeaveSession()
    {
        if (_runner == null) return;
        _isLeavingVoluntarily = true;
        await _runner.Shutdown();
    }

    private void HandleUnexpectedDisconnect(string reason)
    {
        if (_isLeavingVoluntarily) return;
        if (_handledDisconnect) return;
        _handledDisconnect = true;

        Debug.Log($"[NetworkRunnerController] Unexpected disconnect ({reason}). Returning to main menu.");
        OnError?.Invoke("Conexión perdida con el host. Volviendo al menú...");

        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
            SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        SessionListUpdated?.Invoke(sessionList);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        PlayerJoined?.Invoke(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        PlayerLeft?.Invoke(player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

        var data = new NetInput();
        if (move.x > 0.3f) data.Horizontal = 1;
        else if (move.x < -0.3f) data.Horizontal = -1;

        if (_jumpQueued) data.Buttons.Set((int)InputButton.Jump, true);
        if (_attackQueued) data.Buttons.Set((int)InputButton.Attack, true);

        input.Set(data);

        _jumpQueued = false;
        _attackQueued = false;
    }

#pragma warning disable UNT0006
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        HandleUnexpectedDisconnect($"shutdown: {shutdownReason}");
        _isLeavingVoluntarily = false;
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        HandleUnexpectedDisconnect($"disconnect: {reason}");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
#pragma warning restore UNT0006 // Incorrect message signature
}
