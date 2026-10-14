using UnityEngine;

public class PowerSource : MonoBehaviour
{
    [Header("Hold-in-place tuning (world units)")]
    public float breakDistance = 0.02f;   // sideways pull needed to pop it out
    public float liftDistance = 0.01f;    // how far it must be raised to pop out
    public float reSnapCooldown = 0.3f;   // pause before it can snap again after popping out

    private Hole connectedHole;
    private bool snapped;
    private Vector3 snapPosition;
    private float nextSnapTime;
    private CircuitManager circuit;

    void OnTriggerEnter(Collider other)
    {
        if (snapped || Time.time < nextSnapTime)
            return;

        Hole hole = other.GetComponentInParent<Hole>();

        if (hole == null)
        {
            Debug.Log("POWER TOUCHED SOMETHING THAT ISN'T A HOLE: " + other.gameObject.name);
            return;
        }

        Snap(hole);
    }

    void Snap(Hole hole)
    {
        Vector3 holePos = hole.transform.position;

        // Snap X and Z to the hole, keep our own height.
        snapPosition = new Vector3(holePos.x, transform.position.y, holePos.z);
        transform.position = snapPosition;

        connectedHole = hole;
        snapped = true;

        Debug.Log("POWER CONNECTED TO ROW " + hole.row);
        NotifyCircuit();
    }

    // Runs after the grab/physics system has moved us this frame, so
    // transform.position here is where the hand is trying to put the object.
    void LateUpdate()
    {
        if (!snapped)
            return;

        Vector3 delta = transform.position - snapPosition;
        float sideways = new Vector2(delta.x, delta.z).magnitude;
        float lift = delta.y;

        if (sideways > breakDistance || lift > liftDistance)
        {
            Release();
            return; // leave it where the hand put it
        }

        // Not pulled hard enough: hold it in the hole.
        transform.position = snapPosition;
    }

    void Release()
    {
        snapped = false;
        connectedHole = null;
        nextSnapTime = Time.time + reSnapCooldown;

        Debug.Log("POWER DISCONNECTED");
        NotifyCircuit();
    }

    public bool IsConnectedToRow(int row)
    {
        if (connectedHole == null)
            return false;

        return connectedHole.row == row;
    }

    void NotifyCircuit()
    {
        if (circuit == null)
            circuit = FindAnyObjectByType<CircuitManager>();

        if (circuit != null)
            circuit.UpdateCircuit();
    }
}