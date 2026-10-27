using UnityEngine;

public class LEDcode : MonoBehaviour
{
    public Renderer ledRenderer;
    public Material noGlow;
    public Material Glow;

    public float voltage = 0;

    void Update()
    {
        if (ledRenderer == null)
            return;

        if (voltage > 0)
            ledRenderer.material = Glow;
        else
            ledRenderer.material = noGlow;
    }
}