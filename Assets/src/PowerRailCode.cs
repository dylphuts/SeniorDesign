
using UnityEngine;

public class PowerRailTrigger : MonoBehaviour
{
    public string railType = "Positive";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PowerSource"))
        {
            Debug.Log(railType + " BLUETOOTH CONNECTED");
        }

        if (other.CompareTag("Wire"))
        {
            Debug.Log("Wire connected to " + railType + " ");
        }
    }
}
