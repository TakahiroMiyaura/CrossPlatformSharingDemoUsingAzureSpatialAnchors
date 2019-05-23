// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;

public class CursorGaze : MonoBehaviour
{
    void Start()
    {
        transform.localScale = Vector3.one * .25f;
    }

    // Update is called once per frame
    void Update()
    {
        Camera mainCamera = Camera.main;

        // Do a raycast into the world based on the user's
        // head position and orientation.
        var headPosition = mainCamera.transform.position;
        var gazeDirection = mainCamera.transform.forward;

        RaycastHit hitInfo;

        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo, 5.0f))
        {

            // If the raycast did not hit the canvas, update canvas position
            Vector3 nextPos = headPosition+hitInfo.transform.position;
            transform.position = nextPos;
            transform.LookAt(mainCamera.transform);
            transform.Rotate(Vector3.up, 180);
        }
    }
}
