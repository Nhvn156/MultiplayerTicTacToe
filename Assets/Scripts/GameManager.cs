using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GridManager _gridManager;
    [SerializeField] private CanvasGroup _gridCanvasGroup;

    [SerializeField] private TextMeshProUGUI _player1Text;
    [SerializeField] private TextMeshProUGUI _player2Text;
    [SerializeField] private TextMeshProUGUI _turnText;
    [SerializeField] private TextMeshProUGUI _turnSymbolText;

    [SerializeField] private GameObject _turnUI;
    [SerializeField] private GameObject _resultUI;
    [SerializeField] private TextMeshProUGUI _resultText;

    [SerializeField] private Button _restartButton;

    [SerializeField] private List<GameObject> _winningLines = new List<GameObject>();

    private NetworkList<int> _board;
    private NetworkVariable<Turn> _currentTurn = new NetworkVariable<Turn>(Turn.player1Turn);
    private NetworkVariable<GridSquareState> _player1SquareState = new NetworkVariable<GridSquareState>(GridSquareState.x);
    private NetworkVariable<GridSquareState> _player2SquareState = new NetworkVariable<GridSquareState>(GridSquareState.o);
    private NetworkVariable<GameState> _currentGameState = new NetworkVariable<GameState>(GameState.waitingForPlayers);
    private NetworkVariable<int> _winningLineIndex = new NetworkVariable<int>(-1);

    private bool _waitingInput = false;
    private Dictionary<ulong, Turn> _clientTurnMap = new Dictionary<ulong, Turn>();

    private Turn _myTurn;
    private bool _myTurnAssigned = false;

    private static readonly int[,] WinLines = new int[,]
    {
        {0,1,2}, {3,4,5}, {6,7,8},
        {0,3,6}, {1,4,7}, {2,5,8},
        {0,4,8}, {2,4,6}
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of GameManager detected.");
        }

        _board = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        _board.OnListChanged += OnBoardChanged;
        _currentTurn.OnValueChanged += (oldVal, newVal) => { UpdateTurnText(); UpdateBoardInteractable(); };
        _player1SquareState.OnValueChanged += (oldVal, newVal) => UpdatePlayerTexts();
        _player2SquareState.OnValueChanged += (oldVal, newVal) => UpdatePlayerTexts();
        _currentGameState.OnValueChanged += (oldVal, newVal) => { OnGameStateChanged(newVal); UpdateBoardInteractable(); };
        _winningLineIndex.OnValueChanged += (oldVal, newVal) =>
        {
            if (oldVal >= 0 && oldVal < _winningLines.Count)
                _winningLines[oldVal].SetActive(false);
            if (newVal >= 0 && newVal < _winningLines.Count)
                _winningLines[newVal].SetActive(true);
        };

        if (IsServer)
        {
            for (int i = 0; i < 9; i++)
                _board.Add((int)GridSquareState.empty);

            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        _gridManager.ResetGrid();

        UpdatePlayerTexts();
        UpdateTurnText();
        RepaintBoardFromNetwork();
        UpdateBoardInteractable();

        RequestAssignmentRpc();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (!_clientTurnMap.ContainsKey(clientId)) return;

        _clientTurnMap.Remove(clientId);

        // Clear board and go back to waiting for a full pair of players
        for (int i = 0; i < _board.Count; i++)
        {
            _board[i] = (int)GridSquareState.empty;
        }

        DisableAllWinningLinesServer();
        _currentGameState.Value = GameState.waitingForPlayers;
        _waitingInput = false;

        ReassignRemainingPlayer();
    }

    private void ReassignRemainingPlayer()
    {
        // If exactly one player remains, make them player1Turn and clear the map
        // so the next joiner cleanly becomes player2Turn.
        if (_clientTurnMap.Count == 1)
        {
            var remaining = new List<ulong>(_clientTurnMap.Keys);
            ulong remainingId = remaining[0];

            _clientTurnMap.Clear();
            _clientTurnMap[remainingId] = Turn.player1Turn;

            AssignTurnRpc(Turn.player1Turn, RpcTarget.Single(remainingId, RpcTargetUse.Temp));
        }
        else
        {
            _clientTurnMap.Clear();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestAssignmentRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (_clientTurnMap.TryGetValue(senderId, out Turn existing))
        {
            AssignTurnRpc(existing, RpcTarget.Single(senderId, RpcTargetUse.Temp));
            return;
        }

        Turn assignedTurn;
        if (_clientTurnMap.Count == 0)
            assignedTurn = Turn.player1Turn;
        else if (_clientTurnMap.Count == 1)
            assignedTurn = Turn.player2Turn;
        else
            return;

        _clientTurnMap[senderId] = assignedTurn;
        AssignTurnRpc(assignedTurn, RpcTarget.Single(senderId, RpcTargetUse.Temp));

        if (_clientTurnMap.Count == 2 && _currentGameState.Value == GameState.waitingForPlayers)
        {
            StartNewGame();
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void AssignTurnRpc(Turn assignedTurn, RpcParams rpcParams = default)
    {
        _myTurn = assignedTurn;
        _myTurnAssigned = true;
        UpdateTurnText();
        UpdateBoardInteractable();
    }

    private void StartNewGame()
    {
        if (!IsServer) return;

        DisableAllWinningLinesServer();
        _currentGameState.Value = GameState.onGoing;

        for (int i = 0; i < _board.Count; i++)
        {
            _board[i] = (int)GridSquareState.empty;
        }

        int firstTurn = Random.Range(0, 2);
        _currentTurn.Value = (Turn)firstTurn;

        _player1SquareState.Value = GridSquareState.x;
        _player2SquareState.Value = GridSquareState.o;

        _waitingInput = true;
    }

    private void ProcessTurn(Turn turn, int selectedSquare)
    {
        _waitingInput = false;

        GridSquareState state = GridSquareState.empty;
        if (turn == Turn.player1Turn)
        {
            state = _player1SquareState.Value;
        }
        else if (turn == Turn.player2Turn)
        {
            state = _player2SquareState.Value;
        }

        _board[selectedSquare] = (int)state;

        bool gameEnded = CheckGameEnd();
        if (!gameEnded)
        {
            SwitchTurn();
            _waitingInput = true;
        }
    }

    private bool CheckGameEnd()
    {
        bool gridFull = _gridManager.CheckFullGrid();
        GridSquareState winner = CheckForWin();

        if (winner != GridSquareState.empty)
        {
            _currentGameState.Value = (winner == _player1SquareState.Value) ? GameState.p1Win : GameState.p2Win;
            return true;
        }
        else if (gridFull)
        {
            _currentGameState.Value = GameState.draw;
            return true;
        }

        return false;
    }

    private GridSquareState CheckForWin()
    {
        for (int i = 0; i < WinLines.GetLength(0); i++)
        {
            int a = WinLines[i, 0];
            int b = WinLines[i, 1];
            int c = WinLines[i, 2];

            GridSquareState winner = _gridManager.CheckWin(a, b, c);
            if (winner != GridSquareState.empty)
            {
                _winningLineIndex.Value = i;
                return winner;
            }
        }

        return GridSquareState.empty;
    }

    public void GridSquareClicked(int squareId)
    {
        RequestMoveRpc(squareId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveRpc(int squareId, RpcParams rpcParams = default)
    {
        if (!_waitingInput) return;
        if (squareId < 0 || squareId >= _board.Count) return;
        if (_gridManager.GetSquareState(squareId) != GridSquareState.empty) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        if (!_clientTurnMap.TryGetValue(senderId, out Turn senderTurn)) return;
        if (senderTurn != _currentTurn.Value) return;

        ProcessTurn(senderTurn, squareId);
    }

    public void SwitchTurn()
    {
        if (_currentTurn.Value == Turn.player1Turn)
        {
            _currentTurn.Value = Turn.player2Turn;
        }
        else
        {
            _currentTurn.Value = Turn.player1Turn;
        }
    }

    private void DisableAllWinningLinesServer()
    {
        _winningLineIndex.Value = -1;
        foreach (var line in _winningLines)
        {
            line.SetActive(false);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRestartRpc()
    {
        if (_currentGameState.Value == GameState.onGoing) return;
        StartNewGame();
    }

    private void OnBoardChanged(NetworkListEvent<int> change)
    {
        if (change.Type == NetworkListEvent<int>.EventType.Value)
        {
            _gridManager.SetSquare((GridSquareState)change.Value, change.Index);
        }
    }

    private void RepaintBoardFromNetwork()
    {
        for (int i = 0; i < _board.Count; i++)
        {
            _gridManager.SetSquare((GridSquareState)_board[i], i);
        }
    }

    private void UpdatePlayerTexts()
    {
        _player1Text.text = _player1SquareState.Value.ToString();
        _player2Text.text = _player2SquareState.Value.ToString();
    }

    private void UpdateTurnText()
    {
        if (_currentGameState.Value == GameState.waitingForPlayers)
        {
            _turnText.text = "Waiting for opponent...";
            _turnSymbolText.text = "";
            return;
        }

        if (!_myTurnAssigned)
        {
            _turnText.text = "Spectating";
            _turnSymbolText.text = "";
            return;
        }

        bool isMyTurn = _currentTurn.Value == _myTurn;
        string symbol = _currentTurn.Value == Turn.player1Turn
            ? _player1SquareState.Value.ToString()
            : _player2SquareState.Value.ToString();

        _turnText.text = isMyTurn ? $"Your turn" : $"Opponent's turn";
        _turnSymbolText.text = symbol;
    }

    private void UpdateBoardInteractable()
    {
        if (_gridCanvasGroup == null) return;

        bool isMyTurn = _myTurnAssigned && _currentTurn.Value == _myTurn;
        bool gameOngoing = _currentGameState.Value == GameState.onGoing;

        _gridCanvasGroup.blocksRaycasts = isMyTurn && gameOngoing;
        _gridCanvasGroup.interactable = isMyTurn && gameOngoing;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.waitingForPlayers)
        {
            _turnUI.SetActive(true);
            _turnText.text = "Waiting for opponent...";
            _resultUI.SetActive(false);
            if (_restartButton != null) _restartButton.interactable = false;
            UpdateTurnText(); 
            return;
        }

        if (newState == GameState.onGoing)
        {
            _turnUI.SetActive(true);
            _resultUI.SetActive(false);
            if (_restartButton != null) _restartButton.interactable = false;
            UpdateTurnText(); 
            return;
        }

        string msg = newState switch
        {
            GameState.p1Win => "Player 1 wins!",
            GameState.p2Win => "Player 2 wins!",
            GameState.draw => "It's a draw!",
            _ => ""
        };

        _resultText.text = msg;
        _resultUI.SetActive(true);
        _turnUI.SetActive(false);
        if (_restartButton != null) _restartButton.interactable = true;
    }
}

public enum Turn { player1Turn, player2Turn };
public enum GameState { waitingForPlayers, onGoing, p1Win, p2Win, draw };