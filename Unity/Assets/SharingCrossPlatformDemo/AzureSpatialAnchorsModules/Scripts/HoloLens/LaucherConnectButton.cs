using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaucherConnectButton : InputInteractionBase
{

    public Launcher Launcher;

    protected override void OnSelectObjectInteraction(Vector3 hitPoint, object target)
    {
        if (target is RaycastHit)
        {
            var raycastHit = (RaycastHit)target;
            if (raycastHit.collider.gameObject.name.Equals(this.name))
            {
                Launcher.Connect();
            }

        }
    }
}
