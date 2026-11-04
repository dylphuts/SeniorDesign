
using UnityEngine;

public class Transistor : MonoBehaviour
{
    public Prong baseProng;
    public Prong collectorProng;
    public Prong emitterProng;

    bool baseWasSnapped;
    bool collectorWasSnapped;
    bool emitterWasSnapped;

    void Awake()
    {
        if (baseProng == null || collectorProng == null || emitterProng == null)
        {
            Prong[] prongs = GetComponentsInChildren<Prong>();

            if (prongs.Length >= 3)
            {
                baseProng = prongs[0];
                collectorProng = prongs[1];
                emitterProng = prongs[2];
            }
        }
    }

    void Update()
    {
        if (baseProng != null && baseProng.IsSnapped && !baseWasSnapped)
            Debug.Log("Transistor Base is in a hole.");

        if (collectorProng != null && collectorProng.IsSnapped && !collectorWasSnapped)
            Debug.Log("Transistor Collector is in a hole.");

        if (emitterProng != null && emitterProng.IsSnapped && !emitterWasSnapped)
            Debug.Log("Transistor Emitter is in a hole.");

        baseWasSnapped = baseProng != null && baseProng.IsSnapped;
        collectorWasSnapped = collectorProng != null && collectorProng.IsSnapped;
        emitterWasSnapped = emitterProng != null && emitterProng.IsSnapped;
    }

    public bool TryGetRows(out int baseRow, out int collectorRow, out int emitterRow)
    {
        baseRow = emitterRow = collectorRow = -1;

        if (baseProng == null || collectorProng == null || emitterProng == null) return false;
        if (!baseProng.IsSnapped || !collectorProng.IsSnapped || !emitterProng.IsSnapped) return false;

        baseRow = baseProng.Row;
        collectorRow = collectorProng.Row;
        emitterRow = emitterProng.Row;

        return true;
    }
}

