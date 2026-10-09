using System.Collections.Generic;
using UnityEngine;

public class SlotSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SlotBlock
    {
        public string name = "Block";

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
    public Vector3 slotScale = new Vector3(0.05f, 0.05f, 0.05f);
    public List<SlotBlock> blocks = new List<SlotBlock>();

    [ContextMenu("Generate Slots")]
    public void Generate()
    {
        Clear();
        var parent = new GameObject("Slots").transform;
        parent.SetParent(breadboard, false);

        foreach (var b in blocks)
        {
            float pitchX = b.PitchX;
            float pitchZ = b.PitchZ;
            float groupStride = b.GroupStride;

            for (int g = 0; g < b.groups; g++)
                for (int c = 0; c < b.columnsPerGroup; c++)
                    for (int r = 0; r < b.rows; r++)
                    {
                        Vector3 p = b.origin + new Vector3(
                            g * groupStride + c * pitchX, 0f, r * pitchZ);

                        var s = Instantiate(slotPrefab, parent);
                        s.transform.localPosition = p;
                        s.transform.localRotation = Quaternion.identity;
                        s.transform.localScale = slotScale;
                        s.name = $"slot_{b.name}_{g}_{c}_{r}";
                    }
        }
    }

    void Start()
    {
        Generate();
    }

    [ContextMenu("Clear Slots")]
    public void Clear()
    {
        var old = breadboard.Find("Slots");
        if (old != null) DestroyImmediate(old.gameObject);
    }


    void OnDrawGizmosSelected()
    {
        if (breadboard == null) return;
        Gizmos.color = Color.green;
        foreach (var b in blocks)
        {
            float pitchX = b.PitchX;
            float pitchZ = b.PitchZ;
            float groupStride = b.GroupStride;

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