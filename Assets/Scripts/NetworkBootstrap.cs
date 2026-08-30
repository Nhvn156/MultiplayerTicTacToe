using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _openJoinScreenButton;
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private LanDiscovery _lanDiscovery;
    [SerializeField] private JoinScreenUI _joinScreenUI;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _menuLayer; 

    private void Start()
    {
        _hostButton.onClick.AddListener(OnHostClicked);
        _openJoinScreenButton.onClick.AddListener(OnOpenJoinScreenClicked);
    }

    private void OnHostClicked()
    {
        if (NetworkManager.Singleton.ShutdownInProgress || NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Network is still active/shutting down — please wait a moment and try again.");
            return;
        }

        StartCoroutine(HostAfterCleanFrame());
    }

    private IEnumerator HostAfterCleanFrame()
    {
        // Wait 1 frame to allow Unity to complete destroying old NetworkObjects
        yield return null;

        string roomName = string.IsNullOrWhiteSpace(_roomNameInput.text)
            ? "Untitled Game"
            : _roomNameInput.text;

        bool started = NetworkManager.Singleton.StartHost();
        if (started)
        {
            _lanDiscovery.StartAdvertising(roomName);
            if (_menuLayer != null) _menuLayer.SetActive(false);
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