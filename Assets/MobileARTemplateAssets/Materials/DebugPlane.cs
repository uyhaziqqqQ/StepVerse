using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARInspector : MonoBehaviour
{
    void Update()
    {
        Debug.Log("Session State: " + ARSession.state);
        Debug.Log("Plane count: " + FindObjectsOfType<ARPlane>().Length);
    }
}