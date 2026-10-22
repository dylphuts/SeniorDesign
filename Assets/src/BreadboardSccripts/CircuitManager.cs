using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public SlotSpawner slots;
    public PowerSource powerSource;

    void Start()
    {
        if (slots == null) slots = FindAnyObjectByType<SlotSpawner>();
        if (powerSource == null) powerSource = FindAnyObjectByType<PowerSource>();
        UpdateCircuit();
    }

    public void UpdateCircuit()
    {
        if (slots == null) return;

        slots.ClearPower();

        var powered = new HashSet<int>();

        // Seed: the row the battery's positive prong is plugged into.
        if (powerSource != null && powerSource.TryGetPositiveRow(out int startRow))
            powered.Add(startRow);

        // Spread through wires until nothing changes.
        Wire[] wires = FindObjectsByType<Wire>(FindObjectsSortMode.None);
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (Wire w in wires)
            {
                if (!w.TryGetRows(out int a, out int b)) continue;

                if (powered.Contains(a) && powered.Add(b)) changed = true;
                else if (powered.Contains(b) && powered.Add(a)) changed = true;
            }
        }

        foreach (int node in powered)
            slots.SetNodePowered(node);
    }
}