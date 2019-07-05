// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// // Copyright(c) 2019 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;
public class FollowUserGaze : MonoBehaviour
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

        if (!Physics.Raycast(headPosition, gazeDirection, out hitInfo, 5.0f, 1 << 30))
        {
            // If the raycast did not hit the canvas, update canvas position
            Vector3 nextPos = headPosition + gazeDirection*2.3f  + Vector3.down * 0.1f;
            transform.position = Vector3.Lerp(transform.position, nextPos, 1f / 3f);
            transform.LookAt(mainCamera.transform);
            transform.Rotate(Vector3.up, 180);
        }
    }
}
