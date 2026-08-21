using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridSquareManager[] _grid;

    public void ResetGrid()
    {
        foreach (GridSquareManager square in _grid)
        {
            square.SetSquare(GridSquareState.empty);
        }
        for (int i = 0; i < _grid.Length; i++)
        {
            _grid[i].SetSquare(GridSquareState.empty);
            _grid[i].SetSquareId(i); // Set the square ID for each grid square
        }
    }

    public void SetSquare(GridSquareState gridSquareState, int square)
    {
        _grid[square].SetSquare(gridSquareState);
    }

    public GridSquareState GetSquareState(int squareId)
    {
        return _grid[squareId].GetSquareState();
    }

    public bool CheckFullGrid()
    {
        foreach (GridSquareManager square in _grid)
        {
            if (square.GetSquareState() == GridSquareState.empty)
            {
                return false;
            }
        }
        return true; // All squares are filled
    }
    
    public GridSquareState CheckWin(int square1, int square2, int square3)
    {
        GridSquareState state1 = _grid[square1].GetSquareState();
        GridSquareState state2 = _grid[square2].GetSquareState();
        GridSquareState state3 = _grid[square3].GetSquareState();

        if (state1 != GridSquareState.empty && state1 == state2 && state2 == state3)
        {
            return state1; // A player has won
        }
        else
        {
            return GridSquareState.empty; // No winner yet
        }
    }
}
