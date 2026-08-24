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

    public class HostInfo
    {
        public string RoomName;
        public string Ip;
        public float LastSeenTime;
    }

    public Dictionary<string, HostInfo> DiscoveredHosts { get; private set; } = new Dictionary<string, HostInfo>();

    public void StartAdvertising(string roomName)
    {
        StopAdvertising(); // safety: avoid double-binding if called twice
        _serverUdp = new UdpClient(DiscoveryPort);
        _serverUdp.BeginReceive(OnDiscoveryRequestReceived, roomName);
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
        catch (ObjectDisposedException)
        {
            return; // socket was closed mid-receive, stop quietly
        }

        string message = Encoding.UTF8.GetString(data);
        string roomName = (string)result.AsyncState;

        if (message == RequestMessage)
        {
            byte[] response = Encoding.UTF8.GetBytes(ResponsePrefix + roomName);
            _serverUdp.Send(response, response.Length, remoteEP);
        }

        _serverUdp.BeginReceive(OnDiscoveryRequestReceived, roomName);
    }

    public void StopAdvertising()
    {
        _serverUdp?.Close();
        _serverUdp = null;
    }

    public void StartBrowsing()
    {
        DiscoveredHosts.Clear();
        StopBrowsing(); // safety
        _clientUdp = new UdpClient();
        _clientUdp.EnableBroadcast = true;

        byte[] request = Encoding.UTF8.GetBytes(RequestMessage);
        _clientUdp.Send(request, request.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

        _clientUdp.BeginReceive(OnDiscoveryResponseReceived, null);
    }

    public void PingForHosts()
    {
        if (_clientUdp == null) return;
        byte[] request = Encoding.UTF8.GetBytes(RequestMessage);
        _clientUdp.Send(request, request.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
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
        catch (ObjectDisposedException)
        {
            return;
        }

        string message = Encoding.UTF8.GetString(data);

        if (message.StartsWith(ResponsePrefix))
        {
            string roomName = message.Substring(ResponsePrefix.Length);
            string ip = remoteEP.Address.ToString();
            DiscoveredHosts[ip] = new HostInfo { RoomName = roomName, Ip = ip, LastSeenTime = Time.time };
        }

        _clientUdp.BeginReceive(OnDiscoveryResponseReceived, null);
    }

    public void StopBrowsing()
    {
        _clientUdp?.Close();
        _clientUdp = null;
    }

    public void PruneStaleHosts(float maxAgeSeconds = 5f)
    {
        List<string> stale = new List<string>();
        foreach (var kvp in DiscoveredHosts)
        {
            if (Time.time - kvp.Value.LastSeenTime > maxAgeSeconds)
                stale.Add(kvp.Key);
        }
        foreach (var ip in stale)
            DiscoveredHosts.Remove(ip);
    }

    private void OnDestroy()
    {
        StopAdvertising();
        StopBrowsing();
    }
}