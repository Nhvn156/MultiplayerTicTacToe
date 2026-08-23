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

    private Coroutine _hidePopupCoroutine;

    private void Start()
    {
        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);

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

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        ReturnToMenu();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        bool wasMe = clientId == NetworkManager.Singleton.LocalClientId;

        if (wasMe)
        {
            ReturnToMenu();
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

    // Cancel any previous hide-timer so rapid messages don't fight each other
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
    }
}