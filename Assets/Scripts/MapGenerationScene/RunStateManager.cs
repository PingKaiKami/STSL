using System.Collections.Generic;
using UnityEngine;

// Attach this to an empty GameObject named RunStateManager in MapScene.
// MapUIRenderer can also create it automatically if it is missing.
public class RunStateManager : MonoBehaviour
{
    public static RunStateManager Instance { get; private set; }

    [Header("Runtime Player State")]
    [SerializeField] private PlayerRunState playerState = new PlayerRunState();

    [Header("Runtime Map State")]
    [System.NonSerialized]
    public MapData currentMap;

    [System.NonSerialized]
    public MapNode currentSelectedNode;

    [System.NonSerialized]
    public MapNode pendingSelectedNode;

    [Header("Runtime Progress Ids")]
    [SerializeField] private List<string> clearedNodeIds = new List<string>();
    [SerializeField] private List<string> availableNextNodeIds = new List<string>();

    public IReadOnlyList<string> ClearedNodeIds
    {
        get { return clearedNodeIds; }
    }

    public IReadOnlyList<string> AvailableNextNodeIds
    {
        get { return availableNextNodeIds; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePlayerStateInitialized();
    }

    public static RunStateManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RunStateManager existing = FindObjectOfType<RunStateManager>();

        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return existing;
        }

        GameObject managerObject = new GameObject("RunStateManager");
        return managerObject.AddComponent<RunStateManager>();
    }

    public bool HasActiveRun()
    {
        return currentMap != null;
    }

    public void StartNewRun(MapGenerationConfig config)
    {
        playerState.InitializeNewRun();
        currentMap = SlayLikeMapGenerator.Generate(config);
        currentSelectedNode = null;
        pendingSelectedNode = null;
        clearedNodeIds.Clear();
        availableNextNodeIds.Clear();

        for (int i = 0; i < currentMap.Nodes.Count; i++)
        {
            MapNode node = currentMap.Nodes[i];

            if (node.Layer == 0 && !availableNextNodeIds.Contains(node.Id))
            {
                availableNextNodeIds.Add(node.Id);
            }
        }
    }

    public void ClearRun()
    {
        playerState = new PlayerRunState();
        currentMap = null;
        currentSelectedNode = null;
        pendingSelectedNode = null;
        clearedNodeIds.Clear();
        availableNextNodeIds.Clear();
    }

    public bool TrySelectNode(MapNode node)
    {
        if (node == null)
        {
            return false;
        }

        if (!IsNodeAvailable(node.Id))
        {
            return false;
        }

        pendingSelectedNode = node;
        currentSelectedNode = node;
        return true;
    }

    public void CompletePendingRoom()
    {
        if (pendingSelectedNode == null)
        {
            return;
        }

        string completedId = pendingSelectedNode.Id;

        if (!clearedNodeIds.Contains(completedId))
        {
            clearedNodeIds.Add(completedId);
        }

        availableNextNodeIds.Clear();

        for (int i = 0; i < pendingSelectedNode.Children.Count; i++)
        {
            MapNode child = pendingSelectedNode.Children[i];

            if (!clearedNodeIds.Contains(child.Id) && !availableNextNodeIds.Contains(child.Id))
            {
                availableNextNodeIds.Add(child.Id);
            }
        }

        currentSelectedNode = pendingSelectedNode;
        pendingSelectedNode = null;
    }

    public bool IsNodeCleared(string nodeId)
    {
        return clearedNodeIds.Contains(nodeId);
    }

    public bool IsNodeAvailable(string nodeId)
    {
        return availableNextNodeIds.Contains(nodeId) && !clearedNodeIds.Contains(nodeId);
    }

    public bool IsPendingBossRoom()
    {
        return IsPendingRoomType(RoomType.Boss);
    }

    public bool IsPendingRoomType(RoomType roomType)
    {
        return pendingSelectedNode != null && pendingSelectedNode.Type == roomType;
    }

    public PlayerRunState GetPlayerState()
    {
        EnsurePlayerStateInitialized();
        return playerState;
    }

    public PlayerBattleStartData GetPlayerBattleStartData()
    {
        return GetPlayerState().CreateBattleStartData();
    }

    public List<PlayerCardRuntimeData> GetPlayerCards()
    {
        return GetPlayerState().GetCardsSnapshot();
    }

    public void SetPlayerState(PlayerRunState newPlayerState)
    {
        playerState = newPlayerState;
        EnsurePlayerStateInitialized();
        playerState.Normalize();
    }

    public void SetPlayerBattleResult(int remainingHP)
    {
        GetPlayerState().SetCurrentHP(remainingHP);
    }

    public void SetPlayerBattleResult(int remainingHP, int goldReward)
    {
        PlayerRunState state = GetPlayerState();
        state.SetCurrentHP(remainingHP);
        state.AddGold(goldReward);
    }

    public void SetPlayerBattleResult(int remainingHP, IEnumerable<PlayerCardRuntimeData> updatedCards)
    {
        PlayerRunState state = GetPlayerState();
        state.SetCurrentHP(remainingHP);
        state.SetCards(updatedCards);
    }

    public void SetPlayerBattleResult(int remainingHP, int goldReward, IEnumerable<PlayerCardRuntimeData> updatedCards)
    {
        PlayerRunState state = GetPlayerState();
        state.SetCurrentHP(remainingHP);
        state.AddGold(goldReward);
        state.SetCards(updatedCards);
    }

    public void SetPlayerCurrentHP(int currentHP)
    {
        GetPlayerState().SetCurrentHP(currentHP);
    }

    public void SetPlayerMaxHP(int maxHP)
    {
        GetPlayerState().SetMaxHP(maxHP);
    }

    public int GetPlayerGold()
    {
        return GetPlayerState().gold;
    }

    public void SetPlayerGold(int gold)
    {
        GetPlayerState().SetGold(gold);
    }

    public void AddPlayerGold(int amount)
    {
        GetPlayerState().AddGold(amount);
    }

    public bool TrySpendPlayerGold(int amount)
    {
        return GetPlayerState().TrySpendGold(amount);
    }

    public void SetPlayerCards(IEnumerable<PlayerCardRuntimeData> cards)
    {
        GetPlayerState().SetCards(cards);
    }

    public void AddPlayerCard(PlayerCardRuntimeData card)
    {
        GetPlayerState().AddCard(card);
    }

    public bool RemovePlayerCardById(string cardId)
    {
        return GetPlayerState().RemoveCardById(cardId);
    }

    public bool RemovePlayerCardAt(int index)
    {
        return GetPlayerState().RemoveCardAt(index);
    }

    public List<PlayerEquipmentRuntimeData> GetPlayerEquipment()
    {
        return GetPlayerState().GetEquipmentSnapshot();
    }

    public void AddPlayerEquipment(PlayerEquipmentRuntimeData equipment)
    {
        GetPlayerState().AddEquipment(equipment);
    }

    private void EnsurePlayerStateInitialized()
    {
        if (playerState == null)
        {
            playerState = new PlayerRunState();
        }

        if (!playerState.IsInitialized())
        {
            playerState.InitializeNewRun();
            return;
        }

        playerState.Normalize();
    }
}
