using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlaneManager))]
public class ARTrackingManager : MonoBehaviour
{
    private ARPlaneManager arPlaneManager;

    void Awake()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();
    }

    // Call this method when you want to stop visualizing planes (e.g., after placing an object)
    public void TogglePlaneDetectionVisuals(bool isVisible)
    {
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(isVisible);
        }

        // Optional: completely disable the manager to save processing power if no more planes are needed
        arPlaneManager.enabled = isVisible;
    }
}