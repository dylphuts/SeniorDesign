using UnityEngine;

public class Prong : MonoBehaviour
{
    public float powerIn = 0;
    public float powerOut = 0;

    public float breakDistance = 0.02f;
    public float liftDistance = 0.01f;
    public float reSnapCooldown = 0.3f;

    public GameObject parent;

    public Transform wireRoot;

    private Wire wire;
    private Rigidbody rb;
    private Hole connectedHole;
    private CircuitManager circuit;
    private bool snapped;
    private Vector3 snapPosition;
    private float nextSnapTime;

    void Awake()
    {
        wire = GetComponentInParent<Wire>();
        if (wireRoot == null)
            wireRoot = wire != null ? wire.transform : transform;
        rb = wireRoot.GetComponent<Rigidbody>();
        if (transform.parent != null)
        {
            parent = transform.parent.gameObject;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Hole hole = other.GetComponentInParent<Hole>();
        if (hole == null) return;
        if (snapped || Time.time < nextSnapTime) return;

        Snap(hole);
    }

    void Snap(Hole hole)
    {
        Vector3 holePos = hole.transform.position;
        snapPosition = new Vector3(holePos.x, transform.position.y, holePos.z);

        connectedHole = hole;
        snapped = true;

        // First prong in becomes the one that positions the wire.
        if (wire != null && wire.positionDriver == null)
        {
            wire.positionDriver = this;
            MoveWireSoProngIsAt(snapPosition);
        }

        Debug.Log("Prong in Hole " + hole.row + (IsDriver() ? " (driver)" : ""));
        NotifyCircuit();
    }

    bool IsDriver()
    {
        return wire == null || wire.positionDriver == this;
    }

    void MoveWireSoProngIsAt(Vector3 target)
    {
        Vector3 offset = target - transform.position;
        if (offset.sqrMagnitude < 1e-10f) return;

        if (rb != null)
        {
            rb.position += offset;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;   // use rb.velocity on older Unity
                rb.angularVelocity = Vector3.zero;
            }
        }
        wireRoot.position += offset;
        Physics.SyncTransforms();
    }

    void LateUpdate()
    {
        if (!snapped) return;

        // If the driver let go, take over.
        if (wire != null && wire.positionDriver == null)
            wire.positionDriver = this;

        Vector3 delta = transform.position - snapPosition;
        float sideways = new Vector2(delta.x, delta.z).magnitude;

        if (sideways > breakDistance || delta.y > liftDistance)
        {
            Release();
            return;
        }

        if (IsDriver())
            MoveWireSoProngIsAt(snapPosition);
    }

    void Release()
    {
        snapped = false;
        connectedHole = null;
        nextSnapTime = Time.time + reSnapCooldown;

        if (wire != null && wire.positionDriver == this)
            wire.positionDriver = null;

        Debug.Log("POWER DISCONNECTED");
        NotifyCircuit();
    }

    public bool IsConnectedToRow(int row)
    {
        return connectedHole != null && connectedHole.row == row;
    }

    void NotifyCircuit()
    {
        if (circuit == null)
            circuit = FindAnyObjectByType<CircuitManager>();
        if (circuit != null)
            circuit.UpdateCircuit();
    }
}