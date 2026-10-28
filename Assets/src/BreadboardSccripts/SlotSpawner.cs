using System.Collections.Generic;
using UnityEngine;

public class SlotSpawner : MonoBehaviour
{
    // NOTE: new modes must be added at the END so existing saved blocks keep their values.
    public enum ConnectionMode
    {
        PerColumn,     // Same group + column, across the rows INSIDE this one block, share a node
        PerRow,        // Same row index inside the block shares a node (rail running along the block)
        WholeBlock,    // Every slot in the block is one node
        PerSlot,       // Nothing is shared
        SharedColumn   // Same column number across ALL blocks with the same Net Name share a node
                       // (use when each block is just one row of a terminal strip)
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

        public float PitchX => columnsPerGroup > 1
            ? (lastColumnHole.x - origin.x) / (columnsPerGroup - 1) : 0f;
        public float PitchZ => rows > 1
            ? (lastRowHole.z - origin.z) / (rows - 1) : 0f;
        public float GroupStride => groups > 1
            ? (lastGroupHole.x - origin.x) / (groups - 1) : 0f;
    }

    public GameObject slotPrefab;
    public Transform breadboard;
    public Vector3 slotScale = new Vector3(0.005f, 0.005f, 0.005f);
    public List<SlotBlock> blocks = new List<SlotBlock>();

    [Header("Auto Assign Connections settings")]
    [Tooltip("Blocks with at least this many slots in one row are treated as terminal-strip rows. Smaller ones are rail pieces.")]
    public int stripMinSlots = 20;
    [Tooltip("Rail pieces whose Z differs by less than this fraction of the rail Z range are the same rail row.")]
    [Range(0.0001f, 0.05f)]
    public float railZTolerance = 0.002f;

    // Hole.row holds the NODE number: holes with the same number are electrically connected.
    public int NodeCount => slotsByNode.Count;

    private readonly List<List<Hole>> slotsByNode = new List<List<Hole>>();
    private readonly Dictionary<string, int> nodeIds = new Dictionary<string, int>();
    private readonly HashSet<int> litNodes = new HashSet<int>();

    // Awake (not Start) so the slots exist before CircuitManager.Start runs.
    void Awake()
    {
        Generate();
    }

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
            float pitchX = b.PitchX, pitchZ = b.PitchZ, groupStride = b.GroupStride;

            for (int g = 0; g < b.groups; g++)
                for (int c = 0; c < b.columnsPerGroup; c++)
                    for (int r = 0; r < b.rows; r++)
                    {
                        Vector3 p = b.origin + new Vector3(
                            g * groupStride + c * pitchX, 0f, r * pitchZ);

                        var go = Instantiate(slotPrefab, parent);
                        go.transform.localPosition = p;
                        go.transform.localRotation = Quaternion.identity;
                        go.transform.localScale = slotScale;
                        go.name = $"slot_{b.name}_{g}_{c}_{r}";

                        var hole = go.GetComponentInChildren<Hole>();
                        int node = GetNode(NodeKey(b, bi, g, c, r));
                        hole.row = node;
                        slotsByNode[node].Add(hole);
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
        var toOff = new List<int>();
        foreach (int n in litNodes)
            if (!wanted.Contains(n)) toOff.Add(n);

        foreach (int n in toOff)
        {
            foreach (var h in slotsByNode[n]) h.SetGreen(false);
            litNodes.Remove(n);
        }

        foreach (int n in wanted)
        {
            if (n < 0 || n >= slotsByNode.Count) continue;
            if (litNodes.Add(n))
                foreach (var h in slotsByNode[n]) h.SetGreen(true);
        }
    }

    public void ClearPower()
    {
        SetLitNodes(new HashSet<int>());
    }

    [ContextMenu("Test Light Node 120")]
    void TestLight()
    {
        SetLitNodes(new HashSet<int> { 120 });
    }

    [ContextMenu("Log Block Summary")]
    void LogBlockSummary()
    {
        var order = new List<int>();
        for (int i = 0; i < blocks.Count; i++) order.Add(i);
        order.Sort((x, y) =>
        {
            int c = blocks[x].origin.y.CompareTo(blocks[y].origin.y);
            return c != 0 ? c : blocks[x].origin.z.CompareTo(blocks[y].origin.z);
        });

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Blocks sorted by Y then Z:  name | mode | net | origin x,y,z | rows x cols x groups = slots");
        foreach (int i in order)
        {
            var bl = blocks[i];
            int slots = bl.rows * bl.columnsPerGroup * bl.groups;
            sb.AppendLine($"  {bl.name} | {bl.connection} | net '{bl.netName}' | " +
                          $"({bl.origin.x:F5}, {bl.origin.y:F5}, {bl.origin.z:F5}) | " +
                          $"{bl.rows} x {bl.columnsPerGroup} x {bl.groups} = {slots}");
        }
        Debug.Log(sb.ToString());
        GUIUtility.systemCopyBuffer = sb.ToString(); // paste it straight into chat
    }

#if UNITY_EDITOR
    // Fills in Connection / Net Name / name for every block based on its shape and position.
    //   Long rows (20+ slots)  -> terminal strip rows. Split into two halves at the biggest gap in Z
    //                             (the center channel). Same column number in a half = one node.
    //   Short pieces           -> power rail pieces. Pieces at the same Z form one rail.
    // Ctrl/Cmd+Z undoes it.
    [ContextMenu("Auto Assign Connections")]
    void AutoAssignConnections()
    {
        UnityEditor.Undo.RecordObject(this, "Auto Assign Connections");

        if (blocks.Count == 0) { Debug.LogWarning("Auto Assign: no blocks."); return; }

        // Columns run along X, so rows differ along Y or Z. Pick whichever actually varies.
        float yMin = float.MaxValue, yMax = float.MinValue, zMin = float.MaxValue, zMax = float.MinValue;
        foreach (var bl in blocks)
        {
            yMin = Mathf.Min(yMin, bl.origin.y); yMax = Mathf.Max(yMax, bl.origin.y);
            zMin = Mathf.Min(zMin, bl.origin.z); zMax = Mathf.Max(zMax, bl.origin.z);
        }
        bool useY = (yMax - yMin) > (zMax - zMin);
        float keyRange = useY ? yMax - yMin : zMax - zMin;
        System.Func<SlotBlock, float> key = bl => useY ? bl.origin.y : bl.origin.z;

        Debug.Log($"Auto Assign: rows differ along {(useY ? "Y" : "Z")} (range {keyRange:F5}).");
        if (keyRange < 1e-6f)
        {
            Debug.LogError("Auto Assign: every block has the same Y and Z origin, so rows can't be told apart. Check each block's Origin.");
            return;
        }

        var longRows = new List<int>();
        var shortBlocks = new List<int>();
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].columnsPerGroup * blocks[i].groups >= stripMinSlots) longRows.Add(i);
            else shortBlocks.Add(i);
        }

        longRows.Sort((x, y) => key(blocks[x]).CompareTo(key(blocks[y])));
        shortBlocks.Sort((x, y) => key(blocks[x]).CompareTo(key(blocks[y])));

        int expectedNodes = 0;

        // ---- Terminal strips: split into two halves at the biggest gap (the center channel) ----
        if (longRows.Count >= 2)
        {
            int split = 1;
            float biggest = 0f;
            for (int k = 1; k < longRows.Count; k++)
            {
                float gap = key(blocks[longRows[k]]) - key(blocks[longRows[k - 1]]);
                if (gap > biggest) { biggest = gap; split = k; }
            }

            for (int k = 0; k < longRows.Count; k++)
            {
                var bl = blocks[longRows[k]];
                bool lower = k < split;
                string net = lower ? "StripsA" : "StripsB";
                bl.connection = ConnectionMode.SharedColumn;
                bl.netName = net;
                bl.name = $"{net}_row{(lower ? k : k - split)}";
            }

            int colsA = blocks[longRows[0]].columnsPerGroup * blocks[longRows[0]].groups;
            int colsB = blocks[longRows[split]].columnsPerGroup * blocks[longRows[split]].groups;
            expectedNodes += colsA + colsB;

            Debug.Log($"Strips: half A has {split} rows, half B has {longRows.Count - split} rows " +
                      $"(biggest gap {biggest:F5}).");
        }
        else
        {
            Debug.LogWarning("Auto Assign: found fewer than 2 long rows, so strips were left alone.");
        }

        // ---- Rails: pieces at (nearly) the same position along the row axis are one rail row ----
        float tol = Mathf.Max(keyRange * railZTolerance, 1e-6f);

        int railCount = 0;
        float clusterStart = 0f;
        var clusterSizes = new List<int>();
        var clusterRows = new List<int>();
        var clusterPos = new List<float>();
        var clusterPerRow = new List<bool>();

        foreach (int i in shortBlocks)
        {
            var bl = blocks[i];
            float kv = key(bl);
            if (railCount == 0 || kv - clusterStart > tol)
            {
                railCount++;
                clusterStart = kv;
                clusterSizes.Add(0);
                clusterRows.Add(1);
                clusterPos.Add(kv);
                clusterPerRow.Add(false);
            }

            int c = railCount - 1;
            string net = "Rail" + railCount;
            bool perRow = bl.rows > 1;

            bl.connection = perRow ? ConnectionMode.PerRow : ConnectionMode.WholeBlock;
            bl.netName = net;
            bl.name = $"{net}_{clusterSizes[c]++}";
            clusterRows[c] = Mathf.Max(clusterRows[c], perRow ? bl.rows : 1);
            clusterPerRow[c] = clusterPerRow[c] || perRow;
        }

        for (int c = 0; c < railCount; c++)
        {
            int nodes = clusterPerRow[c] ? clusterRows[c] : 1;
            expectedNodes += nodes;
            Debug.Log($"Rail{c + 1}: position {clusterPos[c]:F5}, {clusterSizes[c]} blocks, {nodes} node(s)");
        }

        Debug.Log($"Auto Assign done. After pressing Play you should see 'SlotSpawner: {expectedNodes} nodes'.");

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    void OnDrawGizmosSelected()
    {
        if (breadboard == null) return;
        Gizmos.color = Color.green;
        foreach (var b in blocks)
        {
            float pitchX = b.PitchX, pitchZ = b.PitchZ, groupStride = b.GroupStride;

            for (int g = 0; g < b.groups; g++)
                for (int c = 0; c < b.columnsPerGroup; c++)
                    for (int r = 0; r < b.rows; r++)
                    {
                        Vector3 p = b.origin + new Vector3(
                            g * groupStride + c * pitchX, 0f, r * pitchZ);
                        Gizmos.DrawWireCube(breadboard.TransformPoint(p), slotScale * 0.5f);
                    }
        }
    }
}