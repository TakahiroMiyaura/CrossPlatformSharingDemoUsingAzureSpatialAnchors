// Copyright(c) 2019 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;

public class DeleteAzureSpatialAnchorsButtonVisualizer : MonoBehaviour
{
    public AzureSpatialAnchorsSharedAnchorDemoScript DemoScript;

    public void Update()
    {
        if (DemoScript.IsAzureSpatialAnchorsDeleted)
            Destroy(gameObject);
    }
}