// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// // Copyright(c) 2019 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;

public class LaucherConnectButton : InputInteractionBase
{

    public Launcher Launcher;
    private bool _isTap = false;

    protected override void OnSelectObjectInteraction(Vector3 hitPoint, object target)
    {
        if (!_isTap)
        {
            _isTap = true;
                Launcher.Connect();
        }
    }
}
