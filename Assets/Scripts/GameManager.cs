using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GridManager _gridManager;
    private Turn _currentTurn;
    private GridSquareState _player1SquareState;
    private GridSquareState _player2SquareState;
    private bool _waitingInput = false;
    private GameState _currentGameState;

    [SerializeField] private TextMeshProUGUI _player1Text;
    [SerializeField] private TextMeshProUGUI _player2Text;
    [SerializeField] private TextMeshProUGUI _turnText;

    [SerializeField] private GameObject _turnUI;
    [SerializeField] private GameObject _resultUI;
    [SerializeField] private TextMeshProUGUI _resultText;

    [SerializeField] private List<GameObject> _winningLines = new List<GameObject>();

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
        StartNewGame();
    }

    private void RestartGame()
    {
        bool gameEnded = CheckGameEnd();

        if (gameEnded)
        {
            StartNewGame();
        }
    }
    private void StartNewGame()
    {   
        DisableAllWinningLines();
        _currentGameState = GameState.onGoing;
        // Reset the grid and randomly select the first turn
        _gridManager.ResetGrid();
        int firstTurn = Random.Range(0, 2);
        _currentTurn = (Turn)firstTurn;

        // Assign the square states based on the first turn
        if (firstTurn == 0)
        {
            _player1SquareState = GridSquareState.x;
            _player2SquareState = GridSquareState.o;
        }
        else
        {
            _player2SquareState = GridSquareState.o;
            _player1SquareState = GridSquareState.x;
        }

        _turnUI.SetActive(true);
        _resultUI.SetActive(false);

        _player1Text.text = _player1SquareState.ToString();
        _player2Text.text = _player2SquareState.ToString();

        _turnText.text = _currentTurn == Turn.player1Turn ? _player1SquareState.ToString() : _player2SquareState.ToString();

        _waitingInput = true;
    }

    private void ProcessTurn(Turn turn, int selectedSquare)
    {
        _waitingInput = false;
        GridSquareState state = GridSquareState.empty;
        if (turn == Turn.player1Turn)
        {
            state = _player1SquareState;
        }
        else if (turn == Turn.player2Turn)
        {
            state = _player2SquareState;
        }
        _gridManager.SetSquare(state, selectedSquare);

        bool gameEnded = CheckGameEnd();
        if (!gameEnded)
        {
            SwitchTurn();
            _waitingInput = true;
            _turnText.text = _currentTurn == Turn.player1Turn ? _player1SquareState.ToString() : _player2SquareState.ToString();
        }
    }

    private bool CheckGameEnd()
    {
        bool gridFull = _gridManager.CheckFullGrid();
        GridSquareState winner = CheckForWin();

        if (winner != GridSquareState.empty)
        {
            _currentGameState = (winner == _player1SquareState) ? GameState.p1Win : GameState.p2Win;
            Debug.Log($"Player {(winner == _player1SquareState ? 1 : 2)} wins!");
            _resultText.text = $"Player {(winner == _player1SquareState ? 1 : 2)} wins!";
            _resultUI.SetActive(true);
            _turnUI.SetActive(false);
            return true;
        }
        else if (gridFull)
        {
            _currentGameState = GameState.draw;
            Debug.Log("It's a draw!");
            _resultText.text = "It's a draw!";
            _resultUI.SetActive(true);
            _turnUI.SetActive(false);
            return true;
        }

        return false;
    }

    private GridSquareState CheckForWin()
    {
        // Check all possible winning combinations
        GridSquareState winner = _gridManager.CheckWin(0, 1, 2);
        if (winner != GridSquareState.empty)
        {
            _winningLines[0].SetActive(true);
            return winner;
        } 

        winner = _gridManager.CheckWin(3, 4, 5);
        if (winner != GridSquareState.empty)
        {
            _winningLines[1].SetActive(true);
            return winner;
        }

        winner = _gridManager.CheckWin(6, 7, 8);
        if (winner != GridSquareState.empty)
        {
            _winningLines[2].SetActive(true);
            return winner;
        }

        winner = _gridManager.CheckWin(0, 3, 6);
        if (winner != GridSquareState.empty)
        {
            _winningLines[3].SetActive(true);
            return winner;
        }

        winner = _gridManager.CheckWin(1, 4, 7);
        if (winner != GridSquareState.empty)
        {
            _winningLines[4].SetActive(true);
            return winner;
        }

        winner = _gridManager.CheckWin(2, 5, 8);
        if (winner != GridSquareState.empty)
        {
            _winningLines[5].SetActive(true);
            return winner;
        }

        winner = _gridManager.CheckWin(0, 4, 8);
        if (winner != GridSquareState.empty)
        {
            _winningLines[6].SetActive(true);
            return winner;
        }

        winner = _gridManager.CheckWin(2, 4, 6);
        if (winner != GridSquareState.empty)
        {
            _winningLines[7].SetActive(true);
            return winner;
        }

        return GridSquareState.empty; // No winner found
    }

    public void GridSquareClicked(int squareId)
    {
        if (_waitingInput == false) return;
        if (_gridManager.GetSquareState(squareId) != GridSquareState.empty) return;

        ProcessTurn(_currentTurn, squareId);
        
    }

    public void SwitchTurn()
    {
        if (_currentTurn == Turn.player1Turn)
        {
            _currentTurn = Turn.player2Turn;
        }
        else
        {
            _currentTurn = Turn.player1Turn;
        }
    }

    private void DisableAllWinningLines()
    {
        foreach (var line in _winningLines)
        {
            line.SetActive(false);
        }
    }
}

public enum Turn { player1Turn, player2Turn};
public enum GameState { onGoing, p1Win, p2Win, draw};
