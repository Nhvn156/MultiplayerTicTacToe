using System;
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
    [SerializeField] private GameObject _connectingPanel;

    private List<HostRoomListItem> _spawnedItems = new List<HostRoomListItem>();
    private Coroutine _refreshCoroutine;
    private Coroutine _joinCoroutine;

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
            item.Setup(kvp.Value, this);
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

    // ---------------- JOIN FLOW ----------------

    public void JoinHost(LanDiscovery.HostInfo hostInfo)
    {
        List<string> orderedIps = new List<string>(hostInfo.Ips);
        orderedIps.Sort((a, b) =>
        {
            bool aLoop = a == "127.0.0.1";
            bool bLoop = b == "127.0.0.1";
            if (aLoop == bLoop) return 0;
            return aLoop ? -1 : 1;
        });

        if (_joinCoroutine != null) StopCoroutine(_joinCoroutine);
        if (_connectingPanel != null) _connectingPanel.SetActive(true);
        _joinCoroutine = StartCoroutine(TryIpsInOrder(orderedIps, 0));
    }

    private IEnumerator TryIpsInOrder(List<string> ips, int index)
    {
        if (index >= ips.Count)
        {
            if (_connectingPanel != null) _connectingPanel.SetActive(false);
            Debug.LogWarning("[Join] All addresses failed.");
            yield break;
        }

        bool success = false;
        yield return AttemptConnect(ips[index], result => success = result);

        if (success)
        {
            _lanDiscovery.StopBrowsing();
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }
            ClearList();

            if (_connectingPanel != null) _connectingPanel.SetActive(false);
            _joinScreenPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[Join] {ips[index]} failed, trying next address...");
            yield return TryIpsInOrder(ips, index + 1);
        }
    }

    private IEnumerator AttemptConnect(string ip, Action<bool> onResult)
    {
        while (NetworkManager.Singleton.ShutdownInProgress)
        {
            yield return null;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[Join] Network still active, waiting...");
            onResult(false);
            yield break;
        }

        bool connected = false;

        void OnConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
                connected = true;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ip;

        bool started = NetworkManager.Singleton.StartClient();
        if (!started)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            onResult(false);
            yield break;
        }

        float timeout = 3f;
        float elapsed = 0f;
        while (!connected && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;

        if (connected)
        {
            onResult(true);
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            while (NetworkManager.Singleton.ShutdownInProgress)
            {
                yield return null;
            }
            onResult(false);
        }
    }
}