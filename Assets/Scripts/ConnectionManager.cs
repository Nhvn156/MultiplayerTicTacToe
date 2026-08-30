using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private Button _quitButton;
    [SerializeField] private GameObject _disconnectedPanel;
    [SerializeField] private TextMeshProUGUI _disconnectedText;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private GameObject _menuLayer;
    [SerializeField] private LanDiscovery _lanDiscovery;
    [SerializeField] private Button _quitAppButton;

    private Coroutine _hidePopupCoroutine;

    private void Start()
    {
        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);

        if (_quitAppButton != null)
            _quitAppButton.onClick.AddListener(OnQuitAppClicked);

        if (_disconnectedPanel != null)
            _disconnectedPanel.SetActive(false);

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnQuitClicked()
    {
        Disconnect();
    }

    private void Disconnect()
    {
        if (NetworkManager.Singleton == null) return;

        if (_lanDiscovery != null)
            _lanDiscovery.StopAdvertising();

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            StartCoroutine(WaitForShutdownThenReturnToMenu());
        }
        else
        {
            ReturnToMenu();
        }
    }

    private IEnumerator WaitForShutdownThenReturnToMenu()
    {
        while (NetworkManager.Singleton.ShutdownInProgress)
        {
            yield return null;
        }
        ReturnToMenu();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        bool wasMe = clientId == NetworkManager.Singleton.LocalClientId;

        if (wasMe)
        {
            if (_lanDiscovery != null)
                _lanDiscovery.StopAdvertising();

            StartCoroutine(WaitForShutdownThenReturnToMenu());
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            ShowDisconnectMessage("Opponent disconnected.");
        }
        else
        {
            ShowDisconnectMessage("Host disconnected.");
        }
    }

    private void ShowDisconnectMessage(string message)
{
    if (_disconnectedText != null) _disconnectedText.text = message;
    if (_disconnectedPanel != null) _disconnectedPanel.SetActive(true);

    if (_hidePopupCoroutine != null)
        StopCoroutine(_hidePopupCoroutine);

    _hidePopupCoroutine = StartCoroutine(HidePopupAfterDelay(3f));
}

    private IEnumerator HidePopupAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_disconnectedPanel != null) _disconnectedPanel.SetActive(false);
        _hidePopupCoroutine = null;
    }

    private void ReturnToMenu()
    {
        Debug.Log("Returned to menu after disconnect.");
        if (_menuLayer != null) _menuLayer.SetActive(true);
        if (_menuPanel != null) _menuPanel.SetActive(true);
    }

    private void OnQuitAppClicked()
    {
        if (_lanDiscovery != null)
            _lanDiscovery.StopAdvertising();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}