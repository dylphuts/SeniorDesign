using UnityEngine;

public class PowerSource : MonoBehaviour
{
    private Hole connectedHole;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("POWER TOUCHED " + other.gameObject.name);

        Hole hole = other.GetComponentInParent<Hole>();

        if (hole == null)
        {
            Debug.Log("POWER TOUCHED SOMETHING BUT NO LIKE THE HOLE ITS SOMETHING WEIRD");
            return;
        }

        connectedHole = hole;

        Debug.Log("POWER CONNECTED TO ROW " + hole.row);

        CircuitManager circuit = FindAnyObjectByType<CircuitManager>();

        if (circuit != null)
        {
            circuit.UpdateCircuit();
        }
    }

    void OnTriggerExit(Collider other)
    {
        Hole hole = other.GetComponentInParent<Hole>();

        if (hole == null)
            return;

        if (hole == connectedHole)
        {
            connectedHole = null;

            Debug.Log("POWER DISCONNECTED");

            CircuitManager circuit = FindAnyObjectByType<CircuitManager>();

            if (circuit != null)
            {
                circuit.UpdateCircuit();
            }
        }
    }

    public bool IsConnectedToRow(int row)
    {
        if (connectedHole == null)
            return false;

        return connectedHole.row == row;
    }
}