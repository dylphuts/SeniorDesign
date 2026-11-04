using UnityEngine;

public class Resistor : MonoBehaviour
{
    public Prong legA;
    public Prong legB;

    void Awake()
    {
        if (legA == null || legB == null)
        {
            Prong[] prongs = GetComponentsInChildren<Prong>();
            if (prongs.Length >= 2)
            {
                legA = prongs[0];
                legB = prongs[1];
            }
        }
    }

    public bool TryGetRows(out int rowA, out int rowB)
    {
        rowA = rowB = -1;
        if (legA == null || legB == null) return false;
        if (!legA.IsSnapped || !legB.IsSnapped) return false;

        rowA = legA.Row;
        rowB = legB.Row;
        return true;
    }

    // True if either leg shares a row with the given row
    public bool TouchesRow(int row)
    {
        return TryGetRows(out int a, out int b) && (a == row || b == row);
    }
}