using System.Collections.Generic;
using UnityEngine;

public class SlotSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SlotBlock
    {
        public string name = "Block";
        //Local Origin
        public Vector3 origin;
        // slots runing Vertically 
        public int rows;
        //Slot numbers  IE 60 
        public int columnsPerGroup;
        public int groups;
        //spacing between slots
        public float pitchX;
        //spacing betweens rows
        public float pitchZ;
        public float groupStride;// distance from start of one group to start of next
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
            for (int g = 0; g < b.groups; g++)
                for (int c = 0; c < b.columnsPerGroup; c++)
                    for (int r = 0; r < b.rows; r++)
                    {
                        Vector3 p = b.origin + new Vector3(
                            g * b.groupStride + c * b.pitchX, 0f, r * b.pitchZ);

                        var s = Instantiate(slotPrefab, parent);
                        s.transform.localPosition = p;
                        s.transform.localRotation = Quaternion.identity;
                        s.transform.localScale = slotScale;
                        s.name = $"slot_{b.name}_{g}_{c}_{r}";
                    }
    }

    [ContextMenu("Clear Slots")]
    public void Clear()
    {
        var old = breadboard.Find("Slots");
        if (old != null) DestroyImmediate(old.gameObject);
    }

    // Preview the layout in the Scene view while tuning (select this object)
    void OnDrawGizmosSelected()
    {
        if (breadboard == null) return;
        Gizmos.color = Color.green;
        foreach (var b in blocks)
            for (int g = 0; g < b.groups; g++)
                for (int c = 0; c < b.columnsPerGroup; c++)
                    for (int r = 0; r < b.rows; r++)
                    {
                        Vector3 p = b.origin + new Vector3(
                            g * b.groupStride + c * b.pitchX, 0f, r * b.pitchZ);
                        Gizmos.DrawWireCube(breadboard.TransformPoint(p), slotScale * 0.5f);
                    }
    }
}