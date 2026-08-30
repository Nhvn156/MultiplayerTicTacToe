using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HostRoomListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _roomNameText;
    [SerializeField] private TextMeshProUGUI _ipText;
    [SerializeField] private Button _joinButton;

    private LanDiscovery.HostInfo _hostInfo;
    private JoinScreenUI _owner;

    public void Setup(LanDiscovery.HostInfo hostInfo, JoinScreenUI owner)
    {
        _roomNameText.text = hostInfo.RoomName;
        _ipText.text = string.Join(", ", hostInfo.Ips); // hiển thị tất cả IP biết được, để debug dễ hơn
        _hostInfo = hostInfo;
        _owner = owner;

        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(() => _owner.JoinHost(_hostInfo));
    }
}