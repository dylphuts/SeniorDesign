using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public SlotSpawner slots;
    public PowerSource powerSource;

    [Tooltip("For testing: LED lights even if plugged in backwards.")]
    public bool allowReversedLED = false;

    [Tooltip("Print one status line per circuit update.")]
    public bool debugLog = true;

    void Start()
    {
        if (slots == null) slots = FindAnyObjectByType<SlotSpawner>();
        if (powerSource == null) powerSource = FindAnyObjectByType<PowerSource>();
        UpdateCircuit();
    }

    public void UpdateCircuit()
    {
        if (slots == null) return;

        Wire[] wires = FindObjectsByType<Wire>(FindObjectsSortMode.None);
        LEDcode[] leds = FindObjectsByType<LEDcode>(FindObjectsSortMode.None);

        // Wire-only nets from each battery terminal (used for short + LED checks).
        var positiveNet = new HashSet<int>();
        var negativeNet = new HashSet<int>();

        int pr = -1, nr = -1;
        bool hasP = false, hasN = false;
        if (powerSource != null)
        {
            hasP = powerSource.TryGetPositiveRow(out pr);
            hasN = powerSource.TryGetNegativeRow(out nr);
            if (hasP) Flood(pr, wires, positiveNet);
            if (hasN) Flood(nr, wires, negativeNet);
        }

        bool shorted = positiveNet.Overlaps(negativeNet);

        // What gets drawn green: the + net, plus anything power reaches THROUGH an LED
        // (anode -> cathode), then on through wires. Repeats so LEDs in series work.
        var lit = new HashSet<int>(positiveNet);
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (LEDcode led in leds)
            {
                if (!led.TryGetRows(out int a, out int c)) continue;
                if (!lit.Contains(a) || lit.Contains(c)) continue;
                Flood(c, wires, lit);
                changed = true;
            }
        }
        // The negative side isn't "power", so keep it dark (unless shorted, so you can see the short).
        if (!shorted) lit.ExceptWith(negativeNet);

        slots.SetLitNodes(lit);

        if (debugLog)
        {
            int seated = 0;
            foreach (Wire w in wires) if (w.TryGetRows(out _, out _)) seated++;
            Debug.Log($"[Circuit] powerSource={(powerSource != null)} +row={(hasP ? pr : -1)} -row={(hasN ? nr : -1)} " +
                      $"| wires seated {seated}/{wires.Length} " +
                      $"| posNet=[{string.Join(",", positiveNet)}] negNet=[{string.Join(",", negativeNet)}] " +
                      $"lit=[{string.Join(",", lit)}] shorted={shorted}");
        }

        foreach (LEDcode led in leds)
        {
            bool seatedLed = led.TryGetRows(out int a, out int c);
            bool forward = seatedLed && positiveNet.Contains(a) && negativeNet.Contains(c);
            bool reverse = seatedLed && positiveNet.Contains(c) && negativeNet.Contains(a);
            bool on = !shorted && (forward || (allowReversedLED && reverse));
            led.SetLit(on);

            if (debugLog)
                Debug.Log($"[Circuit] LED '{led.name}' seated={seatedLed} anode={a} cathode={c} " +
                          $"anodeInPos={positiveNet.Contains(a)} cathodeInNeg={negativeNet.Contains(c)} on={on}");
        }
    }

    // Spread through wires until nothing changes. Works on any starting set.
    void Flood(int start, Wire[] wires, HashSet<int> net)
    {
        net.Add(start);
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (Wire w in wires)
            {
                if (!w.TryGetRows(out int a, out int b)) continue;

                if (net.Contains(a) && net.Add(b)) changed = true;
                else if (net.Contains(b) && net.Add(a)) changed = true;
            }
        }
    }
}