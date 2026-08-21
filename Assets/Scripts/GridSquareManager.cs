using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class GridSquareManager : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI _oText;
    [SerializeField] private TextMeshProUGUI _xText;
    private GridSquareState _currentState = GridSquareState.empty;
    private int _squareId;

    public void SetSquare(GridSquareState newState)
    {
        if (newState == GridSquareState.empty)
        {
            _oText.enabled = false;
            _xText.enabled = false;
        }
        else if (newState == GridSquareState.o)
        {
            _oText.enabled = true;
            _xText.enabled = false;
        }
        else if (newState == GridSquareState.x)
        {
            _oText.enabled = false;
            _xText.enabled = true;
        }
       
        _currentState = newState;}

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager.Instance.GridSquareClicked(_squareId);
    }

    public GridSquareState GetSquareState()
    {
        return _currentState;
    }

    public void SetSquareId(int id)
    {
        _squareId = id;
    }
}

public enum GridSquareState {empty, o, x};