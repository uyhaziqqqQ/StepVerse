using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class ARObjectSpawner : MonoBehaviour
{
    [Header("Blender Assets")]
    [Tooltip("Drag your exported 3D Treasure Chest prefab here")]
    public GameObject chestPrefab;

    private GameObject spawnedChest;
    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // Detect user screen interaction
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // Cast a ray from the touch point against detected AR planes
                if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;

                    // Instantiate the chest if it does not exist
                    if (spawnedChest == null)
                    {
                        spawnedChest = Instantiate(chestPrefab, hitPose.position, hitPose.rotation);

                        // Fulfill Task 10 Orientation Requirement: Force the chest to face the user
                        Vector3 lookDirection = Camera.main.transform.position - spawnedChest.transform.position;
                        lookDirection.y = 0; // Keep the chest flat on the floor
                        spawnedChest.transform.rotation = Quaternion.LookRotation(-lookDirection);
                    }
                    else
                    {
                        // Optional: Reposition existing chest on subsequent taps
                        spawnedChest.transform.position = hitPose.position;
                    }
                }
            }
        }
    }
}