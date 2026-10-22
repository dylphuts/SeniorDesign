using UnityEngine;

public class Wire : MonoBehaviour
{
    public Prong prongA;
    public Prong prongB;

    void Awake()
    {
        if (prongA == null || prongB == null)
        {
            Prong[] prongs = GetComponentsInChildren<Prong>();
            if (prongs.Length >= 2)
            {
                prongA = prongs[0];
                prongB = prongs[1];
            }
        }
    }

    // The two rows this wire joins. False unless BOTH prongs are in holes.
    public bool TryGetRows(out int rowA, out int rowB)
    {
        rowA = rowB = -1;
        if (prongA == null || prongB == null) return false;
        if (!prongA.IsSnapped || !prongB.IsSnapped) return false;

        rowA = prongA.Row;
        rowB = prongB.Row;
        return true;
    }
}