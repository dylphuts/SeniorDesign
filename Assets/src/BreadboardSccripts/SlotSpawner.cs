using System.Collections.Generic;
using UnityEngine;

public class SlotSpawner : MonoBehaviour
{
    // New modes must be added at the END so saved blocks keep their values.
    public enum ConnectionMode
    {
        PerColumn,     // Same group + column, across the rows in this block, share a node
        PerRow,        // Same row index in the block shares a node (rail along the block)
        WholeBlock,    // Every slot in the block is one node
        PerSlot,       // Nothing is shared
        SharedColumn   // Same column number across ALL blocks with the same Net Name share a node
    }

    [System.Serializable]
    public class SlotBlock
    {
        public string name = "Block";
        public ConnectionMode connection = ConnectionMode.PerColumn;

        [Tooltip("WholeBlock / PerRow / SharedColumn. Blocks with the same Net Name are wired together. " +
                 "Leave blank for a block on its own.")]
        public string netName = "";

        [Header("Reference points (local space, drag empties onto holes)")]
        public Vector3 origin;
        public Vector3 lastColumnHole;
        public Vector3 lastRowHole;
        public Vector3 lastGroupHole;

        [Header("Counts")]
        public int rows = 5;
        public int columnsPerGroup = 5;
        public int groups = 12;

        // Local position of the slot at group g, column c, row r.
        public Vector3 SlotPosition(int g, int c, int r)
        {
            float pitchX = columnsPerGroup > 1 ? (lastColumnHole.x - origin.x) / (columnsPerGroup - 1) : 0f;
            float pitchZ = rows > 1 ? (lastRowHole.z - origin.z) / (rows - 1) : 0f;
            float stride = groups > 1 ? (lastGroupHole.x - origin.x) / (groups - 1) : 0f;
            return origin + new Vector3(g * stride + c * pitchX, 0f, r * pitchZ);
        }
    }

    public GameObject slotPrefab;
    public Transform breadboard;
    public Vector3 slotScale = new Vector3(0.005f, 0.005f, 0.005f);
    public List<SlotBlock> blocks = new List<SlotBlock>();

    // Hole.row holds the NODE number: holes with the same number are electrically connected.
    public int NodeCount => slotsByNode.Count;

    private readonly List<List<Hole>> slotsByNode = new List<List<Hole>>();
    private readonly Dictionary<string, int> nodeIds = new Dictionary<string, int>();
    private readonly HashSet<int> litNodes = new HashSet<int>();

    // Awake (not Start) so the slots exist before CircuitManager.Start runs.
    void Awake() => Generate();

    [ContextMenu("Generate Slots")]
    public void Generate()
    {
        Clear();

        if (slotPrefab == null || slotPrefab.GetComponentInChildren<Hole>() == null)
        {
            Debug.LogError("SlotSpawner: slotPrefab needs a Hole component (on the same object as its Renderer).");
            return;
        }

        slotsByNode.Clear();
        nodeIds.Clear();
        litNodes.Clear();

        var parent = new GameObject("Slots").transform;
        parent.SetParent(breadboard, false);

        for (int bi = 0; bi < blocks.Count; bi++)
        {
            var b = blocks[bi];
            for (int g = 0; g < b.groups; g++)
                for (int c = 0; c < b.columnsPerGroup; c++)
                    for (int r = 0; r < b.rows; r++)
                    {
                        var go = Instantiate(slotPrefab, parent);
                        go.transform.localPosition = b.SlotPosition(g, c, r);
                        go.transform.localRotation = Quaternion.identity;
                        go.transform.localScale = slotScale;
                        go.name = $"slot_{b.name}_{g}_{c}_{r}";

                        var hole = go.GetComponentInChildren<Hole>();
                        hole.row = GetNode(NodeKey(b, bi, g, c, r));
                        slotsByNode[hole.row].Add(hole);
                    }
        }

        Debug.Log($"SlotSpawner: {NodeCount} nodes");
    }

    string NodeKey(SlotBlock b, int blockIndex, int g, int c, int r)
    {
        string net = string.IsNullOrEmpty(b.netName) ? "block" + blockIndex : b.netName;

        switch (b.connection)
        {
            case ConnectionMode.PerColumn:    return $"strip|{blockIndex}|{g}|{c}";
            case ConnectionMode.PerRow:       return $"rail|{net}|{r}";
            case ConnectionMode.WholeBlock:   return $"net|{net}";
            case ConnectionMode.SharedColumn: return $"col|{net}|{g * b.columnsPerGroup + c}";
            default:                          return $"slot|{blockIndex}|{g}|{c}|{r}";
        }
    }

    // Returns the node id for a key, creating the node the first time it's seen.
    int GetNode(string key)
    {
        if (!nodeIds.TryGetValue(key, out int id))
        {
            id = slotsByNode.Count;
            nodeIds[key] = id;
            slotsByNode.Add(new List<Hole>());
        }
        return id;
    }

    [ContextMenu("Clear Slots")]
    public void Clear()
    {
        if (breadboard == null) return;
        var old = breadboard.Find("Slots");
        if (old != null) DestroyImmediate(old.gameObject);
    }

    // Lights exactly the wanted nodes. Only nodes that changed are touched, so nothing flickers.
    public void SetLitNodes(HashSet<int> wanted)
    {
        foreach (int n in new List<int>(litNodes))
            if (!wanted.Contains(n)) { SetNodeGreen(n, false); litNodes.Remove(n); }

        foreach (int n in wanted)
            if (n >= 0 && n < slotsByNode.Count && litNodes.Add(n))
                SetNodeGreen(n, true);
    }

    public void ClearPower() => SetLitNodes(new HashSet<int>());

    void SetNodeGreen(int node, bool on)
    {
        foreach (var h in slotsByNode[node]) h.SetGreen(on);
    }

    void OnDrawGizmosSelected()
    {
        if (breadboard == null) return;
        Gizmos.color = Color.green;
        foreach (var b in blocks)
            for (int g = 0; g < b.groups; g++)
                for (int c = 0; c < b.columnsPerGroup; c++)
                    for (int r = 0; r < b.rows; r++)
                        Gizmos.DrawWireCube(breadboard.TransformPoint(b.SlotPosition(g, c, r)), slotScale * 0.5f);
    }
}