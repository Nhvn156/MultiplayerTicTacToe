using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class LanDiscovery : MonoBehaviour
{
    private const int DiscoveryPort = 47777;
    private const string RequestMessage = "TTT_DISCOVER_REQUEST";
    private const string ResponsePrefix = "TTT_DISCOVER_RESPONSE:";

    private UdpClient _serverUdp;
    private UdpClient _clientUdp;
    private string _mySessionId;

    public class HostInfo
    {
        public string SessionId;
        public string RoomName;
        public List<string> Ips = new List<string>();
        public float LastSeenTime;
    }

    public Dictionary<string, HostInfo> DiscoveredHosts { get; private set; } = new Dictionary<string, HostInfo>();

    private readonly Queue<(string sessionId, string roomName, string ip)> _pendingResponses = new Queue<(string, string, string)>();
    private readonly object _queueLock = new object();

    // ---------------- HOST SIDE ----------------

    public void StartAdvertising(string roomName)
    {
        StopAdvertising();
        _mySessionId = Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            _serverUdp = new UdpClient(DiscoveryPort);
            Debug.Log($"[LanDiscovery] Advertising started for room '{roomName}' (session {_mySessionId})");
            _serverUdp.BeginReceive(OnDiscoveryRequestReceived, roomName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LanDiscovery] Failed to start advertising: {e.Message}");
        }
    }

    private void OnDiscoveryRequestReceived(IAsyncResult result)
    {
        if (_serverUdp == null) return;

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] data;
        try
        {
            data = _serverUdp.EndReceive(result, ref remoteEP);
        }
        catch (ObjectDisposedException) { return; }
        catch (Exception e)
        {
            Debug.LogWarning($"[LanDiscovery] Host EndReceive error: {e.Message}");
            return;
        }

        string message = Encoding.UTF8.GetString(data);
        string roomName = (string)result.AsyncState;

        if (message == RequestMessage)
        {
            byte[] response = Encoding.UTF8.GetBytes($"{ResponsePrefix}{_mySessionId}:{roomName}");
            _serverUdp.Send(response, response.Length, remoteEP);
        }

        _serverUdp.BeginReceive(OnDiscoveryRequestReceived, roomName);
    }

    public void StopAdvertising()
    {
        if (_serverUdp != null)
        {
            _serverUdp.Close();
            _serverUdp = null;
        }
    }

    // ---------------- CLIENT SIDE ----------------

    public void StartBrowsing()
    {
        DiscoveredHosts.Clear();
        lock (_queueLock) { _pendingResponses.Clear(); }
        StopBrowsing();

        try
        {
            _clientUdp = new UdpClient();
            _clientUdp.EnableBroadcast = true;
            _clientUdp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            _clientUdp.BeginReceive(OnDiscoveryResponseReceived, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LanDiscovery] Failed to start browsing: {e.Message}");
            return;
        }

        PingForHosts();
    }

    public void PingForHosts()
    {
        if (_clientUdp == null) return;

        byte[] request = Encoding.UTF8.GetBytes(RequestMessage);
        try
        {
            _clientUdp.Send(request, request.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
            _clientUdp.Send(request, request.Length, new IPEndPoint(IPAddress.Loopback, DiscoveryPort));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LanDiscovery] PingForHosts error: {e.Message}");
        }
    }

    private void OnDiscoveryResponseReceived(IAsyncResult result)
    {
        if (_clientUdp == null) return;

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] data;
        try
        {
            data = _clientUdp.EndReceive(result, ref remoteEP);
        }
        catch (ObjectDisposedException) { return; }
        catch (Exception) { return; }

        string message = Encoding.UTF8.GetString(data);

        if (message.StartsWith(ResponsePrefix))
        {
            string rest = message.Substring(ResponsePrefix.Length);
            int sepIndex = rest.IndexOf(':');
            if (sepIndex > 0)
            {
                string sessionId = rest.Substring(0, sepIndex);
                string roomName = rest.Substring(sepIndex + 1);
                string ip = remoteEP.Address.ToString();

                lock (_queueLock)
                {
                    _pendingResponses.Enqueue((sessionId, roomName, ip));
                }
            }
        }

        _clientUdp.BeginReceive(OnDiscoveryResponseReceived, null);
    }

    private void Update()
    {
        if (_pendingResponses.Count == 0) return;

        lock (_queueLock)
        {
            while (_pendingResponses.Count > 0)
            {
                var (sessionId, roomName, ip) = _pendingResponses.Dequeue();

                if (DiscoveredHosts.TryGetValue(sessionId, out HostInfo existing))
                {
                    if (!existing.Ips.Contains(ip))
                        existing.Ips.Add(ip);
                    existing.LastSeenTime = Time.time;
                }
                else
                {
                    var info = new HostInfo { SessionId = sessionId, RoomName = roomName, LastSeenTime = Time.time };
                    info.Ips.Add(ip);
                    DiscoveredHosts[sessionId] = info;
                }
            }
        }
    }

    public void StopBrowsing()
    {
        if (_clientUdp != null)
        {
            _clientUdp.Close();
            _clientUdp = null;
        }
    }

    public void PruneStaleHosts(float maxAgeSeconds = 5f)
    {
        List<string> stale = new List<string>();
        foreach (var kvp in DiscoveredHosts)
        {
            if (Time.time - kvp.Value.LastSeenTime > maxAgeSeconds)
                stale.Add(kvp.Key);
        }
        foreach (var id in stale)
            DiscoveredHosts.Remove(id);
    }

    private void OnDestroy()
    {
        StopAdvertising();
        StopBrowsing();
    }
}