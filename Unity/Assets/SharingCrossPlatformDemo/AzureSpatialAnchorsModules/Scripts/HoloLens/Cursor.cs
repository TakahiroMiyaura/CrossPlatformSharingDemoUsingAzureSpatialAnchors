using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private MeshRenderer _focusRenderer;

    public Material FocusIn;
    public Material FocusOut;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0f, 5f, 0f));

        RaycastHit hitInfo;
        if (Physics.Raycast(new Ray(Camera.main.transform.position, Camera.main.transform.forward), out hitInfo))
        {
            transform.position = hitInfo.transform.position;
            var focusObject = hitInfo.collider.gameObject;
            _focusRenderer = focusObject.GetComponent<MeshRenderer>();
            if(_focusRenderer != null)
            { 
                _focusRenderer.material = FocusIn;
            }
        }
        else
        {
            transform.position = Camera.main.transform.position + Camera.main.transform.forward * 10f;
            if (_focusRenderer != null)
            {
                _focusRenderer.material = FocusOut;
            }
        }
    }
}
