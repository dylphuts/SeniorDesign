using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public BreadboardGenerator breadboard;
    public PowerSource powerSource;

    void Start()
    {
        if (breadboard == null)
        {
            breadboard = FindAnyObjectByType<BreadboardGenerator>();
        }

        if (powerSource == null)
        {
            powerSource = FindAnyObjectByType<PowerSource>();
        }

        UpdateCircuit();
    }

    public void UpdateCircuit()
    {
        if (breadboard == null)
            return;

        breadboard.ClearRows();

        bool[] powered = new bool[breadboard.rows];

        for (int row = 0; row < breadboard.rows; row++)
        {
            if (powerSource != null && powerSource.IsConnectedToRow(row))
            {
                powered[row] = true;
            }
        }

        Wire[] wires = FindObjectsByType<Wire>();

        for (int i = 0; i < wires.Length; i++)
        {
            for (int row = 0; row < breadboard.rows; row++)
            {
                if (wires[i].ConnectsPowerToRow(row))
                {
                    powered[row] = true;
                }
            }
        }

        bool changed = true;

        while (changed)
        {
            changed = false;

            for (int i = 0; i < wires.Length; i++)
            {
                for (int row1 = 0; row1 < breadboard.rows; row1++)
                {
                    for (int row2 = 0; row2 < breadboard.rows; row2++)
                    {
                        if (powered[row1] && wires[i].ConnectsRows(row1, row2))
                        {
                            if (!powered[row2])
                            {
                                powered[row2] = true;
                                changed = true;
                            }
                        }
                    }
                }
            }
        }

        for (int row = 0; row < breadboard.rows; row++)
        {
            if (powered[row])
            {
                breadboard.MakeRowGreen(row);
            }
        }
    }
}