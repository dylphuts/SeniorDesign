using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public SlotSpawner slots;
    public PowerSource powerSource;

    [Tooltip("For testing: LED lights even if plugged in backwards.")]
    public bool allowReversedLED = false;

    void Start()
    {
        if (!slots) slots = FindAnyObjectByType<SlotSpawner>();
        if (!powerSource) powerSource = FindAnyObjectByType<PowerSource>();
        UpdateCircuit();
    }

    public void UpdateCircuit()
    {
        if (!slots) return;

        Wire[] wires = FindObjectsByType<Wire>(FindObjectsSortMode.None);
        LEDcode[] leds = FindObjectsByType<LEDcode>(FindObjectsSortMode.None);

        // Everything wired to each battery terminal.
        var pos = new HashSet<int>();
        var neg = new HashSet<int>();
        if (powerSource)
        {
            if (powerSource.TryGetPositiveRow(out int p)) Flood(p, wires, pos);
            if (powerSource.TryGetNegativeRow(out int n)) Flood(n, wires, neg);
        }

        bool shorted = pos.Overlaps(neg);

        // Green nodes: the + net, plus anything power reaches through an LED
        // (anode -> cathode). Repeats so LEDs in series work.
        var lit = new HashSet<int>(pos);
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (LEDcode led in leds)
            {
                if (led.TryGetRows(out int a, out int c) && lit.Contains(a) && !lit.Contains(c))
                {
                    Flood(c, wires, lit);
                    changed = true;
                }
            }
        }

        // Negative side stays dark unless shorted (so you can see the short).
        if (!shorted) lit.ExceptWith(neg);
        slots.SetLitNodes(lit);

        // Turn LEDs on if anode is on + side and cathode is on - side.
        foreach (LEDcode led in leds)
        {
            bool on = false;
            if (!shorted && led.TryGetRows(out int a, out int c))
            {
                bool forward = pos.Contains(a) && neg.Contains(c);
                bool reverse = pos.Contains(c) && neg.Contains(a);
                on = forward || (allowReversedLED && reverse);
            }
            led.SetLit(on);
        }
    }

    // Spread through seated wires until nothing changes.
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