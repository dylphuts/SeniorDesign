
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SpawnObject : MonoBehaviour
{
    public GameObject prefab;

    private bool spawned = false;

    public void OnGrab()
    {
        if (spawned)
        {
            return;
        }

        Instantiate(prefab, transform.position, transform.rotation);
        spawned = true;
    }
}

