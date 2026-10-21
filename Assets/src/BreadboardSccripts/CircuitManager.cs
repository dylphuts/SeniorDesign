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
        if (slots == null)
            return;

        slots.ClearPower();

        var powered = new HashSet<int>();
        Wire[] wires = FindObjectsByType<Wire>();

        // // Nodes touched directly by the power source, or by a wire that touches power.
        // for (int node = 0; node < slots.NodeCount; node++)
        // {
        //     if (powerSource != null && powerSource.IsConnectedToRow(node))
        //         powered.Add(node);

        //     for (int i = 0; i < wires.Length; i++)
        //     {
        //         if (wires[i].ConnectsPowerToRow(node))
        //             powered.Add(node);
        //     }
        // }

        // Spread power through wires until nothing changes.
        bool changed = true;
        while (changed)
        {
            changed = false;

            for (int i = 0; i < wires.Length; i++)
            {
                if (!wires[i].TryGetRows(out int a, out int b))
                    continue;

                if (powered.Contains(a) && powered.Add(b)) changed = true;
                else if (powered.Contains(b) && powered.Add(a)) changed = true;
            }
        }

        foreach (int node in powered)
            slots.SetNodePowered(node);
    }
}