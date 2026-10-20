using UnityEngine;

public class Hole : MonoBehaviour
{
    // The node number. Holes with the same number are electrically connected.
    public int row;

    private Renderer holeRenderer;
    private Color offColor;

    void Awake()
    {
        holeRenderer = GetComponent<Renderer>();
        offColor = holeRenderer.sharedMaterial.color; // remember the slot's normal color
    }

    public void SetGreen(bool green)
    {
        holeRenderer.material.color = green ? Color.green : offColor;
    }
}