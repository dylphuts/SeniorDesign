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
        Resistor[] resistors = FindObjectsByType<Resistor>(FindObjectsSortMode.None);
        LEDcode[] leds = FindObjectsByType<LEDcode>(FindObjectsSortMode.None);

        // Nets that spread through wires AND resistors (used for power/lighting).
        var pos = new HashSet<int>();
        var neg = new HashSet<int>();
        // Nets that spread through wires only (used to detect "no resistor needed").
        var posW = new HashSet<int>();
        var negW = new HashSet<int>();

        if (powerSource)
        {
            if (powerSource.TryGetPositiveRow(out int p))
            {
                Flood(p, wires, resistors, pos);
                Flood(p, wires, new Resistor[0], posW);
            }
            if (powerSource.TryGetNegativeRow(out int n))
            {
                Flood(n, wires, resistors, neg);
                Flood(n, wires, new Resistor[0], negW);
            }
        }

        bool shorted = pos.Overlaps(neg);

        var lit = new HashSet<int>(pos);
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (LEDcode led in leds)
            {
                if (led.TryGetRows(out int a, out int c) && lit.Contains(a) && !lit.Contains(c))
                {
                    Flood(c, wires, resistors, lit);
                    changed = true;
                }
            }
        }

        if (!shorted) lit.ExceptWith(neg);
        slots.SetLitNodes(lit);

        foreach (LEDcode led in leds)
        {
            var state = LEDcode.LedState.Off;

            if (!shorted && led.TryGetRows(out int a, out int c))
            {
                bool forward = pos.Contains(a) && neg.Contains(c);
                bool reverse = pos.Contains(c) && neg.Contains(a);

                if (forward || (allowReversedLED && reverse))
                {
                    // Would it light with wires alone? If not, a resistor is in the path.
                    bool directForward = posW.Contains(a) && negW.Contains(c);
                    bool directReverse = posW.Contains(c) && negW.Contains(a);
                    bool noResistorNeeded = directForward || (allowReversedLED && directReverse);

                    state = noResistorNeeded ? LEDcode.LedState.Bright : LEDcode.LedState.Dim;
                }
            }

            led.SetState(state);
        }
    }

    // Spread through seated wires until nothing changes.
    void Flood(int start, Wire[] wires, Resistor[] resistors, HashSet<int> net)
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

            foreach (Resistor r in resistors)
            {
                if (!r.TryGetRows(out int a, out int b)) continue;
                if (net.Contains(a) && net.Add(b)) changed = true;
                else if (net.Contains(b) && net.Add(a)) changed = true;
            }
        }
    }
}