using UnityEngine;

public class Hole : MonoBehaviour
{
    public int row;

    private Renderer holeRenderer;

    void Awake()
    {
        holeRenderer = GetComponent<Renderer>();

    }

    public void SetGreen(bool green)
    {
        if (green)
        {
            holeRenderer.material.color = Color.green;
        }
        else
        {
            holeRenderer.material.color = Color.white;
        }
    }
}