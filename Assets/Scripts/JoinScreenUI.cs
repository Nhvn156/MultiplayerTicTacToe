using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class JoinScreenUI : MonoBehaviour
{
    [SerializeField] private LanDiscovery _lanDiscovery;
    [SerializeField] private Transform _listContainer;
    [SerializeField] private HostRoomListItem _listItemPrefab;
    [SerializeField] private GameObject _joinScreenPanel;
    [SerializeField] private GameObject _noRoomsText;
    [SerializeField] private GameObject _menuPanel; 

    private List<HostRoomListItem> _spawnedItems = new List<HostRoomListItem>();
    private Coroutine _refreshCoroutine;

    public void OpenJoinScreen()
    {
        _joinScreenPanel.SetActive(true);
        _lanDiscovery.StartBrowsing();

        if (_refreshCoroutine != null) StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = StartCoroutine(RefreshLoop());
    }

    public void CloseJoinScreen()
    {
        _joinScreenPanel.SetActive(false);
        _lanDiscovery.StopBrowsing();

        if (_refreshCoroutine != null)
        {
            StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = null;
        }

        ClearList();

        if (_menuPanel != null) _menuPanel.SetActive(true); 
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            _lanDiscovery.PingForHosts();
            _lanDiscovery.PruneStaleHosts();
            RebuildList();
            yield return new WaitForSeconds(2f);
        }
    }

    private void RebuildList()
    {
        ClearList();

        bool anyFound = false;
        foreach (var kvp in _lanDiscovery.DiscoveredHosts)
        {
            anyFound = true;
            HostRoomListItem item = Instantiate(_listItemPrefab, _listContainer);
            item.Setup(kvp.Value.RoomName, kvp.Value.Ip, this);
            _spawnedItems.Add(item);
        }

        if (_noRoomsText != null)
            _noRoomsText.SetActive(!anyFound);
    }

    private void ClearList()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }

    public void JoinHost(string ip)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ip;

        bool started = NetworkManager.Singleton.StartClient();
        if (started)
        {
            _lanDiscovery.StopBrowsing();
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }
            ClearList();

            _joinScreenPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Failed to connect to host at " + ip);
        }
    }
}