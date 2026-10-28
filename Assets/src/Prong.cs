using UnityEngine;

public class Prong : MonoBehaviour
{
    [Header("Grip strength")]
    public float breakDistance = 0.06f;     // sideways pull to escape
    public float liftDistance = 0.05f;      // upward pull to escape
    public float holdTime = 0.15f;          // must stay past the threshold this long
    public float seatDepth = 0.02f;         // how far a prong can be pushed DOWN into the hole without issue

    [Header("Snapping")]
    public float reSnapCooldown = 0.3f;
    public float snapRadius = 0.01f;        // tune to your hole spacing, in world units
    public float rearmDistance = 0.02f;     // after release, must move this far before snapping again

    [Tooltip("The object that moves as a whole (wire root, battery root, LED root). Auto-found if empty.")]
    public Transform bodyRoot;

    private float pullingSince = -1f;
    private Prong[] siblings;
    private Rigidbody rb;
    private Hole connectedHole;
    private CircuitManager circuit;
    private bool snapped;
    private Vector3 snapPosition;
    private float nextSnapTime;
    private Vector3 releasePos;
    private bool needsRearm;

    public bool IsSnapped => snapped;
    public int Row => connectedHole != null ? connectedHole.row : -1;

    void Awake()
    {
        if (bodyRoot == null)
        {
            Wire wire = GetComponentInParent<Wire>();
            PowerSource ps = GetComponentInParent<PowerSource>();
            LEDcode led = GetComponentInParent<LEDcode>();
            bodyRoot = wire != null ? wire.transform
                     : ps != null ? ps.transform
                     : led != null ? led.transform
                     : transform;
        }
        rb = bodyRoot.GetComponent<Rigidbody>();
        siblings = bodyRoot.GetComponentsInChildren<Prong>();
    }

    void OnTriggerEnter(Collider other) { TrySnap(other); }
    void OnTriggerStay(Collider other) { TrySnap(other); } // retries after cooldown

    void TrySnap(Collider other)
    {
        if (snapped || Time.time < nextSnapTime) return;
        if (other.GetComponentInParent<Hole>() == null) return;

        // After a release, the prong must move away before it can snap again.
        if (needsRearm)
        {
            if ((transform.position - releasePos).magnitude < rearmDistance) return;
            needsRearm = false;
        }

        // Snap to the NEAREST hole, not the first one touched.
        Hole best = null;
        float bestDist = float.MaxValue;
        foreach (Collider c in Physics.OverlapSphere(transform.position, snapRadius,
                     ~0, QueryTriggerInteraction.Collide))
        {
            Hole h = c.GetComponentInParent<Hole>();
            if (h == null) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = h; }
        }
        if (best != null) Snap(best);
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

        Debug.Log($"{bodyRoot.name}/{name} snapped into node {hole.row}");
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

        // The non-driver end of a rigid body gets extra slack.
        bool driver = IsDriver();
        float limit = driver ? breakDistance : breakDistance * 1.5f;
        bool pulledPastLimit = sideways > limit || delta.y > liftDistance;

        if (pulledPastLimit)
        {
            if (pullingSince < 0f) pullingSince = Time.time;

            // Only release after a sustained pull.
            if (Time.time - pullingSince >= holdTime)
            {
                pullingSince = -1f;
                Release();
                return;
            }
        }
        else
        {
            pullingSince = -1f;
        }

        // Always pull the body back toward the hole while still held in.
        if (driver)
            MoveBodySoProngIsAt(snapPosition);
    }

    void Release()
    {
        snapped = false;
        connectedHole = null;
        nextSnapTime = Time.time + reSnapCooldown;
        releasePos = transform.position;
        needsRearm = true;

        Debug.Log($"{bodyRoot.name}/{name} released");
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