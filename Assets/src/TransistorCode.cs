
using UnityEngine;

public class Transistor : MonoBehaviour
{
    public Prong baseProng;
    public Prong collectorProng;
    public Prong emitterProng;

    void Awake()
    {
        Prong[] prongs = GetComponentsInChildren<Prong>();

        if (baseProng == null && prongs.Length >= 1)
            baseProng = prongs[0];

        if (collectorProng == null && prongs.Length >= 2)
            collectorProng = prongs[1];

        if (emitterProng == null && prongs.Length >= 3)
            emitterProng = prongs[2];
    }

    public bool TryGetRows(out int baseRow, out int collectorRow, out int emitterRow)
    {
        baseRow = -1;
        collectorRow = -1;
        emitterRow = -1;

        if (baseProng == null || collectorProng == null || emitterProng == null)
            return false;

        if (!baseProng.IsSnapped || !collectorProng.IsSnapped || !emitterProng.IsSnapped)
            return false;

        baseRow = baseProng.Row;
        collectorRow = collectorProng.Row;
        emitterRow = emitterProng.Row;

        return true;
    }

    public bool IsFullyConnected()
    {
        return baseProng != null &&
               collectorProng != null &&
               emitterProng != null &&
               baseProng.IsSnapped &&
               collectorProng.IsSnapped &&
               emitterProng.IsSnapped;
    }
}
