using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _openJoinScreenButton;
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private LanDiscovery _lanDiscovery;
    [SerializeField] private JoinScreenUI _joinScreenUI;
    [SerializeField] private GameObject _menuPanel; 

    private void Start()
    {
        _hostButton.onClick.AddListener(OnHostClicked);
        _openJoinScreenButton.onClick.AddListener(OnOpenJoinScreenClicked);
    }

    private void OnHostClicked()
    {
        string roomName = string.IsNullOrWhiteSpace(_roomNameInput.text)
            ? "Untitled Game"
            : _roomNameInput.text;

        bool started = NetworkManager.Singleton.StartHost();
        if (started)
        {
            _lanDiscovery.StartAdvertising(roomName);
            if (_menuPanel != null) _menuPanel.SetActive(false); 
        }
        else
        {
            Debug.LogWarning("Failed to start host — port may already be in use.");
        }
    }

    private void OnOpenJoinScreenClicked()
    {
        if (_menuPanel != null) _menuPanel.SetActive(false); 
        _joinScreenUI.OpenJoinScreen();
    }
}