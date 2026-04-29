using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System;

public class LobbyNetworkManager : NetworkBehaviour
{
    public static LobbyNetworkManager Instance { get; private set; }

    // 1. CẬP NHẬT: Khai báo RoomSettings với cấu trúc mới (phân chia Map và Match Rules)
    public NetworkVariable<LobbySettings> RoomSettings = new NetworkVariable<LobbySettings>(
        new LobbySettings 
        { 
            matchRules = new MatchRules { 
                isShipMode = true, 
                hasTurnTimer = false, 
                turnTimeSeconds = 30, 
                isCustomMatch = false,
                fleetComposition = new int[] { 1, 1, 2, 1 }, // Tàu 5, 4, 3, 2
                maxPlanes = 5,
                maxFlightPath = 5,
                isFreeFlightPath = false
            },
            mapSettings = new MapSettings { 
                isCustomMap = false, 
                mapWidth = 10, 
                mapHeight = 10 
            }
        }, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // Các biến đồng bộ khác
    public NetworkVariable<bool> isClientReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> hostName = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> clientName = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> hostFactionIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> clientFactionIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hostName.Value = DataManager.Instance.CurrentSettings.playerName;
            NetworkManager.Singleton.OnClientDisconnectCallback += CleanUpWhenClientLeaves;
        }
        else if (IsClient)
        {
            SetClientNameServerRpc(DataManager.Instance.CurrentSettings.playerName);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= CleanUpWhenClientLeaves;
        }
    }

    private void CleanUpWhenClientLeaves(ulong clientId)
    {
        if (clientId != NetworkManager.ServerClientId)
        {
            clientName.Value = ""; 
            isClientReady.Value = false; 
            clientFactionIndex.Value = 0; 
        }
    }

    public void HostUpdateSettings(LobbySettings newSettings)
    {
        if (IsServer) RoomSettings.Value = newSettings;
    }

    // ==========================================
    // 2. CẬP NHẬT: Cú pháp Rpc Mới nhất của Netcode (Fix lỗi Vàng)
    // ==========================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ToggleReadyServerRpc()
    {
        isClientReady.Value = !isClientReady.Value; 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetClientNameServerRpc(FixedString32Bytes name)
    {
        clientName.Value = name; 
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ChangeClientFactionServerRpc(int newIndex)
    {
        clientFactionIndex.Value = newIndex;
    }
}