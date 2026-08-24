using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HostRoomListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _roomNameText;
    [SerializeField] private TextMeshProUGUI _ipText;
    [SerializeField] private Button _joinButton;

    private string _ip;
    private JoinScreenUI _owner;

    public void Setup(string roomName, string ip, JoinScreenUI owner)
    {
        _roomNameText.text = roomName;
        _ipText.text = ip;
        _ip = ip;
        _owner = owner;

        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(() => _owner.JoinHost(_ip));
    }
}