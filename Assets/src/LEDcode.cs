using UnityEngine;
using UnityEngine.Serialization;

public class LEDcode : MonoBehaviour
{
    public Renderer ledRenderer;
    public Material noGlow;
    public Material dimGlow;
    public Material Glow;
    public enum LedState { Off, Dim, Bright }

    [Header("Legs (long leg = anode)")]
    [FormerlySerializedAs("plusProng")]
    public Prong anodeProng;     // + leg
    [FormerlySerializedAs("negativeProng")]
    public Prong cathodeProng;   // - leg

    void Awake()
    {
        if (anodeProng == null || cathodeProng == null)
        {
            Prong[] prongs = GetComponentsInChildren<Prong>();
            if (prongs.Length >= 2)
            {
                anodeProng = prongs[0];
                cathodeProng = prongs[1];
            }
        }
        SetLit(false);
    }

    public bool TryGetRows(out int anodeRow, out int cathodeRow)
    {
        anodeRow = cathodeRow = -1;
        if (anodeProng == null || cathodeProng == null) return false;
        if (!anodeProng.IsSnapped || !cathodeProng.IsSnapped) return false;

        anodeRow = anodeProng.Row;
        cathodeRow = cathodeProng.Row;
        return true;
    }
    public void SetState(LedState state)
    {
        if (ledRenderer == null) return;

        switch (state)
        {
            case LedState.Off: ledRenderer.material = noGlow; break;
            case LedState.Dim: ledRenderer.material = dimGlow; break;
            case LedState.Bright: ledRenderer.material = Glow; break;
        }
    }

    public void SetLit(bool on)
    {
        SetState(on ? LedState.Bright : LedState.Off);
    }
}