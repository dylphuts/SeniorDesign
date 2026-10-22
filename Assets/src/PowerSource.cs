using UnityEngine;

public class PowerSource : MonoBehaviour
{
    public float voltage = 9f;          // was 900, which looks like a typo
    public Prong positiveProng;
    public Prong negativeProng;

    public bool TryGetPositiveRow(out int row)
    {
        row = -1;
        if (positiveProng == null || !positiveProng.IsSnapped) return false;
        row = positiveProng.Row;
        return true;
    }

    public bool TryGetNegativeRow(out int row)
    {
        row = -1;
        if (negativeProng == null || !negativeProng.IsSnapped) return false;
        row = negativeProng.Row;
        return true;
    }
}