
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject objectPrefab;

    private bool spawned = false;

    public void OnGrab()
    {
        Debug.Log("OBJECT GRABBED!");

        Instantiate(objectPrefab, transform.position, transform.rotation);
    }
}
