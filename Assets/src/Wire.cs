using UnityEngine;

public class Wire : MonoBehaviour
{
    private Hole firstHole;
    private Hole secondHole;

    private bool connectedToPower;

    void OnTriggerEnter(Collider other)
    {
        PowerSource power = other.GetComponentInParent<PowerSource>();

        if (power != null)
        {
            connectedToPower = true;
            Debug.Log("WIRE CONNECTED TO POWER");
            UpdateCircuit();
            return;
        }

        Hole hole = other.GetComponentInParent<Hole>();

        if (hole == null)
            return;

        Debug.Log("WIRE TOUCHED ROW " + hole.row);

        if (firstHole == null)
        {
            firstHole = hole;
        }
        else if (secondHole == null && hole != firstHole)
        {
            secondHole = hole;
        }

        UpdateCircuit();
    }

    void OnTriggerExit(Collider other)
    {
        PowerSource power = other.GetComponentInParent<PowerSource>();

        if (power != null)
        {
            connectedToPower = false;
            Debug.Log("WIRE DISCONNECTED FROM POWER");
            UpdateCircuit();
            return;
        }

        Hole hole = other.GetComponentInParent<Hole>();

        if (hole == null)
            return;

        if (hole == firstHole)
        {
            firstHole = null;
        }

        if (hole == secondHole)
        {
            secondHole = null;
        }

        UpdateCircuit();
    }

    public bool IsConnectedToPower()
    {
        return connectedToPower;
    }

    // NEW: gives CircuitManager the two nodes this wire joins.
    public bool TryGetRows(out int rowA, out int rowB)
    {
        rowA = rowB = -1;

        if (firstHole == null || secondHole == null)
            return false;

        rowA = firstHole.row;
        rowB = secondHole.row;
        return true;
    }

    public bool ConnectsRows(int row1, int row2)
    {
        if (firstHole == null || secondHole == null)
            return false;

        if (firstHole.row == row1 && secondHole.row == row2)
            return true;

        if (firstHole.row == row2 && secondHole.row == row1)
            return true;

        return false;
    }

    public bool ConnectsPowerToRow(int row)
    {
        if (!connectedToPower)
            return false;

        if (firstHole != null && firstHole.row == row)
            return true;

        if (secondHole != null && secondHole.row == row)
            return true;

        return false;
    }

    void UpdateCircuit()
    {
        CircuitManager circuit = FindAnyObjectByType<CircuitManager>();

        if (circuit != null)
        {
            circuit.UpdateCircuit();
        }
    }
}