using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SocialPlatforms;

/// <summary>
/// Player manager.
/// Handles fire Input and Beams.
/// </summary>
public class CameraPlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{

    #region IPunObservable implementation


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(localPosition);
            stream.SendNext(localRotation);
        }
        else
        {
            // Network player, receive data
            this.localPosition = (Vector3)stream.ReceiveNext();
            this.localRotation = (Quaternion) stream.ReceiveNext();
        }
    }


    #endregion

    #region Private Fields


    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;

    private MeshRenderer _renderer;

    public Material OtherPlayerMaterial;

    private Vector3 localPosition = Vector3.zero;
    private Quaternion localRotation;

    #endregion

    #region MonoBehaviour CallBacks

    void Start()
    {

        transform.parent = SharedCollection.Instance.transform;

#if UNITY_5_4_OR_NEWER
        // Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, loadingMode) =>
        {
            this.CalledOnLevelWasLoaded(scene.buildIndex);
        };
#endif

    }

#if !UNITY_5_4_OR_NEWER
/// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
void OnLevelWasLoaded(int level)
{
    this.CalledOnLevelWasLoaded(level);
}
#endif


    void CalledOnLevelWasLoaded(int level)
    {
        localRotation = transform.localRotation;
        localPosition = transform.localPosition;
        _renderer = GetComponent<MeshRenderer>();
    }

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    void Awake()
    {

        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            CameraPlayerManager.LocalPlayerInstance = this.gameObject;
        }
        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity on every frame.
    /// </summary>
    void Update()
    {
        if (photonView.IsMine)
        {
            ProcessInputs();
        }
        else
        {
            if(_renderer !=null)
            { 
                _renderer.material = OtherPlayerMaterial;
            }
        }

        transform.localPosition = localPosition;
        transform.localRotation = localRotation;

    }

    #endregion

    #region Custom

    /// <summary>
    /// Processes the inputs. Maintain a flag representing when the user is pressing Fire.
    /// </summary>
    void ProcessInputs()
    {
        transform.position = Camera.main.transform.position;
        transform.rotation = Camera.main.transform.rotation;
        localPosition = transform.localPosition;
        localRotation = transform.localRotation;
    }

    #endregion
}
