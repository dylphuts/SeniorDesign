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

    // Hole.row holds the NODE number: holes with the same number are electrically connected.
    public int NodeCount => slotsByNode.Count;

    private readonly List<List<Hole>> slotsByNode = new List<List<Hole>>();
    private readonly Dictionary<string, int> nodeIds = new Dictionary<string, int>();
    private readonly List<Hole> litHoles = new List<Hole>();

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
        litHoles.Clear();

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

    // Turns off only the holes that were lit, instead of touching every hole.
    public void ClearPower()
    {
        foreach (var h in litHoles) h.SetGreen(false);
        litHoles.Clear();
    }

    public void SetNodePowered(int node)
    {
        if (node < 0 || node >= slotsByNode.Count) return;

        foreach (var h in slotsByNode[node])
        {
            h.SetGreen(true);
            litHoles.Add(h);
        }
    }

    [ContextMenu("Log Block Summary")]
    void LogBlockSummary()
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            int slots = b.rows * b.columnsPerGroup * b.groups;
            Debug.Log($"[{i}] {b.name} | {b.connection} | net '{b.netName}' | z {b.origin.z:F4} | " +
                      $"rows {b.rows} x cols {b.columnsPerGroup} x groups {b.groups} = {slots} slots");
        }
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

        var longRows = new List<int>();
        var shortBlocks = new List<int>();
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].columnsPerGroup * blocks[i].groups >= 20) longRows.Add(i);
            else shortBlocks.Add(i);
        }

        longRows.Sort((a, b) => blocks[a].origin.z.CompareTo(blocks[b].origin.z));
        shortBlocks.Sort((a, b) => blocks[a].origin.z.CompareTo(blocks[b].origin.z));

        // Estimate the spacing between rows from the long rows.
        float rowPitch = 0f;
        for (int k = 1; k < longRows.Count; k++)
        {
            float gap = blocks[longRows[k]].origin.z - blocks[longRows[k - 1]].origin.z;
            if (gap > 1e-5f && (rowPitch == 0f || gap < rowPitch)) rowPitch = gap;
        }
        float tolerance = rowPitch > 0f ? rowPitch * 0.5f : 0.004f;

        int expectedNodes = 0;

        // ---- Terminal strips ----
        if (longRows.Count >= 2)
        {
            int split = 1;
            float biggest = 0f;
            for (int k = 1; k < longRows.Count; k++)
            {
                float gap = blocks[longRows[k]].origin.z - blocks[longRows[k - 1]].origin.z;
                if (gap > biggest) { biggest = gap; split = k; }
            }

            for (int k = 0; k < longRows.Count; k++)
            {
                var b = blocks[longRows[k]];
                bool lower = k < split;
                string net = lower ? "StripsA" : "StripsB";
                b.connection = ConnectionMode.SharedColumn;
                b.netName = net;
                b.name = $"{net}_row{(lower ? k : k - split)}";
            }

            int colsA = blocks[longRows[0]].columnsPerGroup * blocks[longRows[0]].groups;
            int colsB = blocks[longRows[split]].columnsPerGroup * blocks[longRows[split]].groups;
            expectedNodes += colsA + colsB;

            Debug.Log($"Strips: half A has {split} rows, half B has {longRows.Count - split} rows " +
                      $"(biggest gap {biggest:F4} vs normal row spacing {rowPitch:F4}).");
        }
        else
        {
            Debug.LogWarning("Auto Assign: found fewer than 2 long rows, so strips were left alone.");
        }

        // ---- Rails ----
        int railCount = 0, inRail = 0;
        float lastZ = 0f;
        var railSizes = new List<int>();
        foreach (int i in shortBlocks)
        {
            var b = blocks[i];
            if (railCount == 0 || b.origin.z - lastZ > tolerance)
            {
                railCount++;
                inRail = 0;
                railSizes.Add(0);
            }
            lastZ = b.origin.z;

            string net = "Rail" + railCount;
            b.connection = ConnectionMode.WholeBlock;
            b.netName = net;
            b.name = $"{net}_{inRail++}";
            railSizes[railCount - 1]++;
        }

        for (int k = 0; k < railSizes.Count; k++)
            Debug.Log($"Rail{k + 1}: {railSizes[k]} blocks");

        expectedNodes += railCount;
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