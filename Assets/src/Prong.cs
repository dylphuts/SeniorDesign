using UnityEngine;

public class Prong : MonoBehaviour
{
    public float breakDistance = 0.02f;
    public float liftDistance = 0.01f;
    public float reSnapCooldown = 0.3f;

    [Tooltip("The object that moves as a whole (wire root or battery root). Auto-found if empty.")]
    public Transform bodyRoot;

    private Prong[] siblings;
    private Rigidbody rb;
    private Hole connectedHole;
    private CircuitManager circuit;
    private bool snapped;
    private Vector3 snapPosition;
    private float nextSnapTime;

    public bool IsSnapped => snapped;
    public int Row => connectedHole != null ? connectedHole.row : -1;

    void Awake()
    {
        if (bodyRoot == null)
        {
            Wire wire = GetComponentInParent<Wire>();
            PowerSource ps = GetComponentInParent<PowerSource>();
            bodyRoot = wire != null ? wire.transform
                     : ps != null ? ps.transform
                     : transform;
        }
        rb = bodyRoot.GetComponent<Rigidbody>();
        siblings = bodyRoot.GetComponentsInChildren<Prong>();
    }

    void OnTriggerEnter(Collider other) { TrySnap(other); }
    void OnTriggerStay(Collider other)  { TrySnap(other); } // retries after cooldown

    void TrySnap(Collider other)
    {
        if (snapped || Time.time < nextSnapTime) return;

        Hole hole = other.GetComponentInParent<Hole>();
        if (hole == null) return;

        Snap(hole);
    }

    void Snap(Hole hole)
    {
        Vector3 holePos = hole.transform.position;
        snapPosition = new Vector3(holePos.x, transform.position.y, holePos.z);

        connectedHole = hole;
        snapped = true;

        // Only the first snapped prong positions the body.
        if (IsDriver())
            MoveBodySoProngIsAt(snapPosition);

        Debug.Log(name + " snapped into row " + hole.row);
        NotifyCircuit();
    }

    // The first snapped prong on this body is the one that moves it.
    bool IsDriver()
    {
        foreach (Prong p in siblings)
            if (p.snapped) return p == this;
        return false;
    }

    void MoveBodySoProngIsAt(Vector3 target)
    {
        Vector3 offset = target - transform.position;
        if (offset.sqrMagnitude < 1e-10f) return;

        if (rb != null)
        {
            rb.position += offset;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        bodyRoot.position += offset;
        Physics.SyncTransforms();
    }

    void LateUpdate()
    {
        if (!snapped) return;

        Vector3 delta = transform.position - snapPosition;
        float sideways = new Vector2(delta.x, delta.z).magnitude;

        if (sideways > breakDistance || delta.y > liftDistance)
        {
            Release();
            return;
        }

        if (IsDriver())
            MoveBodySoProngIsAt(snapPosition);
    }

    void Release()
    {
        snapped = false;
        connectedHole = null;
        nextSnapTime = Time.time + reSnapCooldown;

        Debug.Log(name + " released");
        NotifyCircuit();
    }

    void NotifyCircuit()
    {
        if (circuit == null)
            circuit = FindAnyObjectByType<CircuitManager>();
        if (circuit != null)
            circuit.UpdateCircuit();
    }
}