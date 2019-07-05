using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HoloLensButtonTap : MonoBehaviour
{

    public UnityEvent OnClick;

    public bool IsGlobal = false;

    private bool _isInitialize = false;

    public virtual void OnDestroy()
    {
#if WINDOWS_UWP || UNITY_WSA
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;
#endif
    }
    
    public virtual void Start()
    {
        if (!_isInitialize)
        {
#if WINDOWS_UWP || UNITY_WSA
            UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
#endif
            _isInitialize = true;
        }
    }

    public void OnEnable()
    {
        Start();
    }

#if WINDOWS_UWP || UNITY_WSA
    /// <summary>
    /// Handles the HoloLens interaction event.
    /// </summary>
    /// <param name="obj">The <see cref="UnityEngine.XR.WSA.Input.InteractionSourcePressedEventArgs"/> instance containing the event data.</param>
    private void InteractionManager_InteractionSourcePressed(UnityEngine.XR.WSA.Input.InteractionSourcePressedEventArgs obj)
    {
        if((_isFocus || IsGlobal ) && !_isTap)
        {
             _isTap=true;
             OnClick?.Invoke();
        }
    }
#endif
    private bool _isTap = false;
    private bool _isFocus;
    // Update is called once per frame
    void Update()
    {
        Debug.Log(_isFocus);
        RaycastHit hitInfo;

        if (Physics.Raycast(new Ray(Camera.main.transform.position, Camera.main.transform.forward), out hitInfo))
        {
            if (hitInfo.transform.gameObject.name.Equals(name))
            {
                _isFocus = true;
                return;
            }
        }
        _isFocus = false;
    }
}
