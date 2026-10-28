using UnityEngine;

public class Hole : MonoBehaviour
{
    // The node number. Holes with the same number are electrically connected.
    public int row;

    [SerializeField] Color litColor = Color.green;

    Renderer holeRenderer;
    MaterialPropertyBlock block;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        holeRenderer = GetComponentInChildren<Renderer>();
        if (holeRenderer == null)
            Debug.LogError($"Hole {name} has no Renderer", this);
        block = new MaterialPropertyBlock();
    }

    public void SetGreen(bool on)
    {
        if (holeRenderer == null) return;

        if (!on)
        {
            holeRenderer.SetPropertyBlock(null); // back to the material's own color
            return;
        }

        holeRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, litColor);
        block.SetColor(ColorId, litColor);
        holeRenderer.SetPropertyBlock(block);
    }
}