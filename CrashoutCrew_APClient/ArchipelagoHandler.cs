using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CrashoutCrew_APClient;

public class ArchipelagoHandler : MonoBehaviour
{
    public static ArchipelagoHandler Instance = null!;
    
    private ArchipelagoSession? _session;

    public SlotData? slotData;
    
    public string? Server { get; set; }
    public string? Slot { get; set; }
    public string? Password { get; set; }
    public string? Seed { get; set; }
    public bool IsConnected => _session?.Socket.Connected ?? false;
    public static Action? OnDisconnect;
    public event Action<string, string>? OnConnected;
    public event Action<string>? OnConnectionFailed;

    private ConcurrentQueue<long> _locationsToCheck = new();

    public volatile bool connectionFinished = true;
    public volatile bool connectionSucceeded;
    private readonly bool _queueBreak = false;
    private bool _shouldDisconnect;
    
    private string? _lastDeath;
    private DateTime _lastDeathTime =  DateTime.Now;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (!_shouldDisconnect) 
            return;
        _shouldDisconnect = false;
        Disconnect();
    }

    public bool CreateSession(string server, string slot, string password)
    {
        Server = server;
        Slot = slot;
        Password = password;
        _locationsToCheck = new ConcurrentQueue<long>();
        _shouldDisconnect = false;
        connectionSucceeded = false;
        connectionFinished = false;
        try { _session = ArchipelagoSessionFactory.CreateSession(Server); }
        catch (Exception) { return false; }
        _session.MessageLog.OnMessageReceived += OnMessageReceived;
        _session.Socket.ErrorReceived += OnError;
        _session.Socket.SocketClosed += OnSocketClosed;
        _session.Items.ItemReceived += ItemReceived;
        _session.Socket.PacketReceived += OnPacketReceived;
        return true;
    }

    private IEnumerator ConnectRoutine()
    {
        APConsole.Instance.Log($"Logging in to {Server} as {Slot}...");
        var connectTask = _session!.ConnectAsync();
        
        yield return new WaitUntil(() => connectTask.IsCompleted);
        
        if (connectTask.IsFaulted || connectTask.IsCanceled)
        {
            var exception = connectTask.Exception?.InnerException?.Message ?? "Took too long...";
            Debug.Log($"Connection Error: {exception}");
            connectionSucceeded = false;
            connectionFinished = true;
            OnConnectionFailed?.Invoke(exception);
            yield break;
        }
        
        Seed = connectTask.Result.SeedName;

        var loginTask = _session.LoginAsync(
            "Crashout Crew",
            Slot,
            ItemsHandlingFlags.AllItems,
            new Version(0, 6, 7),
            [],
            password: Password
            );
        
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.IsFaulted || loginTask.IsCanceled)
        {
            var exception = loginTask.Exception?.InnerException?.Message ?? "Took too long...";
            APConsole.Instance.Log($"Login Error: {exception}");
            connectionSucceeded = false;
            connectionFinished = true;
            yield break;
        }

        if (loginTask.Result.Successful)
        {
            APConsole.Instance.Log($"Successfully connected to {Server}!");
            var successResult = (LoginSuccessful)loginTask.Result;
            slotData = new SlotData(successResult.SlotData);

            Instance.StartCoroutine(RunCheckQueue());
            connectionSucceeded = true;
            connectionFinished = true;
            OnConnected?.Invoke(Seed, Slot!);
            yield break;
        }
        
        connectionSucceeded = false;
        connectionFinished = true;

        if (loginTask.Result != null)
        {
            var failure = (LoginFailure)loginTask.Result;
            var errorMessage = $"Failed to connect to {Server} with {Slot}.";
            errorMessage = failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
            errorMessage = failure.ErrorCodes.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
            OnConnectionFailed?.Invoke(errorMessage);
            APConsole.Instance.Log(errorMessage);
        }
        APConsole.Instance.Log("Attempting to reconnect...");
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Connect()
    {
        StartCoroutine(ConnectRoutine());
    }

    public void Disconnect()
    {
        if (_session == null)
            return;
        _session.MessageLog.OnMessageReceived -= OnMessageReceived;
        _session.Socket.ErrorReceived -= OnError;
        _session.Socket.SocketClosed -= OnSocketClosed;
        _session.Items.ItemReceived -= ItemReceived;
        _session.Socket.PacketReceived -= OnPacketReceived;
        ItemHandler.Instance.Reset();
        PluginMain.SaveDataHandler?.Reset();
        try
        {
            _session.Socket.DisconnectAsync();
        } catch (Exception e) { Debug.LogWarning($"Error during async disconnect: {e.Message}"); }
        _session = null;
        StopAllCoroutines();
        OnDisconnect?.Invoke();
        APConsole.Instance.Log("Disconnected from Archipelago!");
    }

    private void OnError(Exception exception, string message)
    {
        Debug.LogWarning(exception);
        APConsole.Instance.Log($"Socket Error: {message} - {exception.Message}");
        if (exception is SocketException or System.Net.WebSockets.WebSocketException)
            _shouldDisconnect = true;
    }

    private void OnSocketClosed(string reason)
        => _shouldDisconnect = true;

    private void ItemReceived(ReceivedItemsHelper helper)
    {
        try
        {
            while (helper.Any())
            {
                var itemIndex = helper.Index;
                var item = helper.DequeueItem();
                ItemHandler.Instance.EnqueueItem(itemIndex, item);
            }
        }
        catch (Exception exception)
        {
            APConsole.Instance.Log($"[Item Received ERROR] {exception}");
            throw;
        }
    }

    public void Release()
    {
        _session?.SetGoalAchieved();
        _session?.SetClientState(ArchipelagoClientState.ClientGoal);
    }

    public void CheckLocation(long id)
    {
        if (!IsLocationChecked(id))
            _locationsToCheck.Enqueue(id);
    }

    public void CheckLocations(long[] ids)
    {
        ids.ToList().ForEach(id => _locationsToCheck.Enqueue(id));
    }

    private IEnumerator RunCheckQueue()
    {
        while (true)
        {
            if (_locationsToCheck.TryDequeue(out var locationId))
            {
                _session?.Locations.CompleteLocationChecks(locationId);
            }
            
            if (_queueBreak)
                yield break;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private bool IsLocationChecked(long id)
    {
        return _session?.Locations.AllLocationsChecked.Contains(id) ?? false;
    }

    public int CountLocationsCheckedInRange(long start, long end)
    {
        return _session?.Locations.AllLocationsChecked.Count(loc => loc >= start && loc < end) ?? 0;
    }

    public void UpdateTags(List<string> tags)
    {
        var packet = new ConnectUpdatePacket
        {
            Tags = tags.ToArray(),
            ItemsHandling = ItemsHandlingFlags.AllItems
        };
        _session?.Socket.SendPacket(packet);
    }

    private void OnMessageReceived(LogMessage message)
    {
        if (PluginMain.FilterLog && message is PlayerSpecificLogMessage { IsRelatedToActivePlayer: false }) 
            return;
        
        string messageStr;
        if (message.Parts.Length == 1)
            messageStr = message.Parts[0].Text;
        else
        {
            var builder = new StringBuilder();
            foreach (var part in message.Parts)
                builder.Append($"{part.Text}");
            messageStr = builder.ToString();
        }
        APConsole.Instance.Log(messageStr);
    }

    private void OnPacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet)
        {
            case BouncePacket bouncePacket:
                BouncePacketReceived(bouncePacket);
                break;
        }
    }

    public ScoutedItemInfo? TryScoutLocation(long locationId, bool createHint = false)
    {
        return _session?.Locations.ScoutLocationsAsync(createHint, locationId)?.Result?.Values.First();
    }

    public void SendDeath()
    {
        APConsole.Instance.Log($"SendDeath called!");
        if (slotData?.DeathLink ?? true)
            return;
        var packet = new BouncePacket();
        var now = DateTime.Now;
        
        if (now - _lastDeathTime < TimeSpan.FromSeconds(2))
            return;

        packet.Tags = ["DeathLink"];
        packet.Data = new Dictionary<string, JToken>
        {
            { "time", now.ToUnixTimeStamp() },
            { "source", Slot },
            { "cause", "Death" },
        };
        
        _lastDeathTime = now;
        _session?.Socket.SendPacket(packet);
    }

    private void BouncePacketReceived(BouncePacket packet)
    {
        if (_lastDeath == null)
            _lastDeathTime = DateTime.Now;
        if (slotData?.DeathLink ?? false)
            ProcessBouncePacket(packet, "DeathLink", ref _lastDeath!, (source, data) =>
                HandleDeathLink(source, data["cause"]?.ToString() ?? "Unknown"));
    }

    private static void ProcessBouncePacket(BouncePacket packet, string tag, ref string lastTime,
        Action<string, Dictionary<string, JToken>> handler)
    {
        if (!packet.Tags.Contains(tag)) return;
        if (!packet.Data.TryGetValue("time", out var timeObj))
            return;
        if (lastTime != timeObj.ToString())
            return;
        lastTime = timeObj.ToString();
        if (!packet.Data.TryGetValue("source", out var sourceObj))
            return;
        var source = sourceObj?.ToString() ?? "Unknown";
        handler(source, packet.Data);
    }
    
    private void HandleDeathLink(string source, string cause)
    {
        if (!slotData?.DeathLink ?? true)
            return;
        if (source == Slot)
            return;
        Kill();
    }

    public void Kill()
    {
        APConsole.Instance.Log("Player Killed!!!");
    }

    public void Reset()
    {
        connectionFinished = true;
        connectionSucceeded = false;
    }
}
