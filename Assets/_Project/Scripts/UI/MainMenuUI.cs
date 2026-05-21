using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject browsePanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Main Menu Panel")]
    [SerializeField] private Button openCreateButton;
    [SerializeField] private Button openBrowseButton;
    [SerializeField] private Button quitButton;

    [Header("Create Room Panel")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button confirmCreateButton;
    [SerializeField] private Button backFromCreateButton;

    [Header("Browse Panel")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private RoomItemUI roomItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backFromBrowseButton;
    [SerializeField] private TMP_Text browseEmptyLabel;

    [Header("Status")]
    [SerializeField] private TMP_Text errorLabel;
    [SerializeField] private TMP_Text loadingLabel;

    private readonly List<RoomItemUI> _spawnedItems = new();
    private NetworkRunnerController _runner;

    private void Start()
    {
        _runner = NetworkRunnerController.Instance;
        WireButtons();
        SubscribeRunner();
        ShowMain();

        if (!string.IsNullOrEmpty(NetworkRunnerController.PendingMessage))
        {
            SetError(NetworkRunnerController.PendingMessage);
            NetworkRunnerController.PendingMessage = null;
        }

        if (_runner != null) _ = _runner.JoinLobby();
    }

    private void OnDestroy()
    {
        UnsubscribeRunner();
    }

    private void WireButtons()
    {
        if (openCreateButton) openCreateButton.onClick.AddListener(ShowCreate);
        if (openBrowseButton) openBrowseButton.onClick.AddListener(ShowBrowse);
        if (quitButton) quitButton.onClick.AddListener(QuitApp);

        if (confirmCreateButton) confirmCreateButton.onClick.AddListener(OnConfirmCreate);
        if (backFromCreateButton) backFromCreateButton.onClick.AddListener(ShowMain);

        if (refreshButton) refreshButton.onClick.AddListener(RefreshLobby);
        if (backFromBrowseButton) backFromBrowseButton.onClick.AddListener(ShowMain);
    }

    private void SubscribeRunner()
    {
        if (_runner == null) return;
        _runner.SessionListUpdated += HandleSessionListUpdated;
        _runner.OnJoinedLobby += HandleJoinedLobby;
        _runner.OnError += HandleError;
    }

    private void UnsubscribeRunner()
    {
        if (_runner == null) return;
        _runner.SessionListUpdated -= HandleSessionListUpdated;
        _runner.OnJoinedLobby -= HandleJoinedLobby;
        _runner.OnError -= HandleError;
    }

    private void ShowMain()
    {
        SetActive(mainMenuPanel, true);
        SetActive(createRoomPanel, false);
        SetActive(browsePanel, false);
        SetActive(loadingPanel, false);
        SetError("");
    }

    private void ShowCreate()
    {
        SetActive(mainMenuPanel, false);
        SetActive(createRoomPanel, true);
        SetActive(browsePanel, false);
        SetActive(loadingPanel, false);
        if (roomNameInput) roomNameInput.text = "";
        SetError("");
    }

    private void ShowBrowse()
    {
        SetActive(mainMenuPanel, false);
        SetActive(createRoomPanel, false);
        SetActive(browsePanel, true);
        SetActive(loadingPanel, false);
        SetError("");
        RefreshLobby();
    }

    private void ShowLoading(string message)
    {
        SetActive(loadingPanel, true);
        if (loadingLabel) loadingLabel.text = message;
    }

    private void OnConfirmCreate()
    {
        var name = string.IsNullOrWhiteSpace(roomNameInput?.text)
            ? $"Sala-{Random.Range(1000, 9999)}"
            : roomNameInput.text.Trim();
        ShowLoading($"Creando sala '{name}'...");
        _ = NetworkRunnerController.Instance.CreateRoom(name);
    }

    private void RefreshLobby()
    {
        _ = NetworkRunnerController.Instance.JoinLobby();
    }

    private void HandleSessionListUpdated(List<SessionInfo> sessions)
    {
        ClearRoomList();

        var hasAny = false;
        if (sessions != null)
        {
            foreach (var session in sessions)
            {
                if (!session.IsOpen || !session.IsVisible) continue;
                if (roomItemPrefab == null || roomListContent == null) continue;

                var item = Instantiate(roomItemPrefab, roomListContent);
                item.Bind(session, HandleJoinRoomClicked);
                _spawnedItems.Add(item);
                hasAny = true;
            }
        }

        if (browseEmptyLabel) browseEmptyLabel.gameObject.SetActive(!hasAny);
    }

    private void HandleJoinRoomClicked(string roomName)
    {
        ShowLoading($"Uniéndose a '{roomName}'...");
        _ = NetworkRunnerController.Instance.JoinRoom(roomName);
    }

    private void HandleJoinedLobby() { }

    private void HandleError(string message)
    {
        SetActive(loadingPanel, false);
        ShowMain();
        SetError(message);
        Debug.LogError($"[MainMenu] {message}");
    }

    private void ClearRoomList()
    {
        foreach (var item in _spawnedItems)
            if (item) Destroy(item.gameObject);
        _spawnedItems.Clear();
    }

    private void SetError(string message)
    {
        if (errorLabel) errorLabel.text = message ?? "";
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go) go.SetActive(active);
    }

    private static void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
