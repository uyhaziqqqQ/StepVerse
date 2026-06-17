using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARDebugState : MonoBehaviour
{
    void Update()
    {
        Debug.Log("AR Session State: " + ARSession.state);
    }
}