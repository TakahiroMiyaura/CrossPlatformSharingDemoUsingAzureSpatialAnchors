// Copyright(c) 2019 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SamplesCode : InputInteractionBase
{
    #region private fields and properties

    private readonly List<string> _localAnchorIds = new List<string>();
    private readonly Queue<Action> _dispatchQueue = new Queue<Action>();

    private AzureSpatialAnchorsDemoWrapper _cloudManager;
    private AppState _currentAppState = AppState.Ready;
    private CloudSpatialAnchor _currentCloudAnchor;
    private bool _isErrorActive;

    private Text _feedbackBox;

#if !UNITY_EDITOR
        public AnchorExchanger anchorExchanger = new AnchorExchanger();
#endif

    private AppState currentAppState
    {
        get => _currentAppState;
        set
        {
            if (_currentAppState != value)
            {
                Debug.LogFormat("State from {0} to {1}", _currentAppState, value);
                _currentAppState = value;
            }
        }
    }

    #endregion

    #region public Properties    

    public string BaseSharingUrl = "";

    /// <summary>
    ///     アンカーのオブジェクトを設定します。
    /// </summary>
    public GameObject AnchoredObjectPrefab = null;

    public GameObject UXParent = null;

    public Canvas PunCanvs = null;
    
    [HideInInspector] public bool RetrySavingCloudSpatialAnchor = false;

    private readonly List<GameObject> _otherSpawnedObjects = new List<GameObject>();
    private int _anchorsLocated;
    private int _anchorsExpected;
    private List<CloudSpatialAnchor> _resetAnchors = new List<CloudSpatialAnchor>();

    #endregion

    #region Unity call back

    public override void Start()
    {
        base.Start();

        _feedbackBox = XRUXPicker.Instance.GetFeedbackText();
        if (_feedbackBox == null)
        {
            Debug.Log($"{nameof(_feedbackBox)} not found in scene by XRUXPicker.");
            Destroy(this);
            return;
        }

        _cloudManager = AzureSpatialAnchorsDemoWrapper.Instance;

        if (_cloudManager == null)
        {
            Debug.Log("AzureSpatialAnchorsDemoWrapper doesn't exist in the scene. Make sure it has been added.");
            return;
        }

        if (!SanityCheckAccessConfiguration())
        {
            Debug.Log(
                $"{nameof(AzureSpatialAnchorsDemoWrapper.SpatialAnchorsAccountId)} and {nameof(AzureSpatialAnchorsDemoWrapper.SpatialAnchorsAccountKey)} must be set on the AzureSpatialAnchors object in your scene");
        }


        if (AnchoredObjectPrefab == null)
        {
            Debug.Log("CreationTarget must be set on the demo script.");
            return;
        }

#if !UNITY_EDITOR
            anchorExchanger.WatchKeys(this.BaseSharingUrl);
#endif


        _cloudManager.OnAnchorLocated += CloudManager_OnAnchorLocated;
        _cloudManager.OnLogDebug += CloudManager_OnLogDebug;
        _cloudManager.OnSessionError += CloudManager_OnSessionError;
#if WINDOWS_UWP || UNITY_WSA
        _cloudManager.OnLocateAnchorsCompleted += _cloudManager_OnLocateAnchorsCompleted;
#endif
    }

    private void _cloudManager_OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
    {
        foreach (var cloudSpatialAnchor in _resetAnchors)
        {
            _cloudManager.DeleteAnchorAsync(cloudSpatialAnchor).GetAwaiter().GetResult();
        }
        if (currentAppState == AppState.AnchorReset)
        {
            _resetAnchors.Clear();
            currentAppState = AppState.AnchorSearching;
        }
    }

    public void Hoge()
    {
#if !UNITY_EDITOR
        if (anchorExchanger.AnchorCount > 0)
        {
            currentAppState = AppState.AnchorReset;
            List<string> anchorsToFind = new List<string>();

            anchorsToFind.AddRange(anchorExchanger.AnchorKeys);

            
            _cloudManager.ResetSessionStatusIndicators();
            _cloudManager.EnableProcessing = true;
            _cloudManager.SetAnchorIdsToLocate(anchorsToFind);
            _cloudManager.CreateWatcher();
        }
        else
        {
            currentAppState = AppState.AnchorSearching;
        }

#endif
        
    }

    public override void Update()
    {
        base.Update();
        lock (this._dispatchQueue)
        {
            if (this._dispatchQueue.Count > 0)
            {
                this._dispatchQueue.Dequeue()();
            }
        }

        switch (currentAppState)
        {
            //指定のアンカー名の情報でAzure Spatial Anchorsに問い合わせを行う。
            case AppState.AnchorSearching:

                _cloudManager.ResetSessionStatusIndicators();
                _cloudManager.EnableProcessing = true;
#if !UNITY_EDITOR
                _feedbackBox.text = $"Cloud Spatial Anchor Count -> {anchorExchanger.AnchorCount}";
                if( anchorExchanger.AnchorCount == 0 )
                {
                    currentAppState = AppState.AnchorNotFounded;
                }
                else
                {
                    currentAppState = AppState.AnchorFounded;
                }
#endif

                break;
            //Azure Spatial Anchorsにアンカーが存在しない場合
            case AppState.AnchorNotFounded:
            //ローカルにセットしたアンカーをAzure Spatial Anchorに保存できなかった場合
            //ユーザに任意の場所にアンカー設置を指せる（＝タップ等で空間にCube等のGameObjectを生成し、spawnedObjectフィールドに設定する）
            case AppState.RetrySavingCloudSpatialAnchor:
                if (_otherSpawnedObjects.Count == 0)
                {
                    _feedbackBox.text = "Cloud Spatial Anchor Not Founded. Set an Anchor on spatial surface";
                }
                else
                {
                    currentAppState = AppState.StartSavingCloudSpatialAnchor;
                }

                break;
            //spawnedObjectフィールドのオブジェクト座標をアンカーとして送る
            case AppState.StartSavingCloudSpatialAnchor:
                SaveCurrentObjectAnchorToCloud();
                break;
            case AppState.SavingCloudSpatialAnchor:
                break;
            //Azure Spatial Anchorsへの保存に失敗した場合リトライを即す（以降RetrySavingCloudSpatialAnchorがtrueになるまで待ちになる）。
            case AppState.SavingCloudSpatialAnchorFailed:
                _feedbackBox.text = "failed to create cloud spatial anchor.please try it again.";
                if (RetrySavingCloudSpatialAnchor)
                {
                    currentAppState = AppState.AnchorSearching;
                }

                break;
            //Azure Spatial Anchorsから取得したアンカーを取得できた場合
            case AppState.SavedCloudSpatialAnchor:
                _cloudManager.EnableProcessing = false;
                _cloudManager.ResetSession();
                _feedbackBox.text = "Saved Spatial Anchor in Cloud.";
                currentAppState = AppState.Done;
                break;
            //同じアンカー名ですでにAzure Spatial Anchorsに登録されている場合
            case AppState.AnchorFounded:
                LocateAnchors();
                _feedbackBox.text = "Cloud Spatial Anchor Founded. Set an Anchor in scene";
                currentAppState = AppState.LocatingAnchors;
                break;
            case AppState.LocatingAnchors:
                _feedbackBox.text = "Locating Anchors....";
                break;
            case AppState.LocatedAnchors:
                _feedbackBox.text = "Finish Locating....";
                currentAppState = AppState.Done;
                break;
            case AppState.Done:
                _cloudManager.EnableProcessing = false;
                _cloudManager.ResetSession();
                SceneManager.LoadScene("Launcher");

                currentAppState = AppState.Ready;
                break;


        }
    }

    private void LocateAnchors()
    {
        _cloudManager.ResetSessionStatusIndicators();
        _currentCloudAnchor = null;
        List<string> anchorsToFind = new List<string>();

#if !UNITY_EDITOR
        if(anchorExchanger.AnchorCount > 1)
        {
            anchorsToFind.Add(anchorExchanger.AnchorKeys[0]);
        }
        else
        {
            anchorsToFind.AddRange(anchorExchanger.AnchorKeys);
        }
#endif
        _anchorsExpected = anchorsToFind.Count;
        _cloudManager.SetAnchorIdsToLocate(anchorsToFind);
        _cloudManager.CreateWatcher();

    }

    /// <summary>
    ///     Queues the specified <see cref="Action" /> on update.
    /// </summary>
    /// <param name="updateAction">The update action.</param>
    protected void QueueOnUpdate(Action updateAction)
    {
        lock (_dispatchQueue)
        {
            _dispatchQueue.Enqueue(updateAction);
        }
    }

    public override void OnDestroy()
    {
        // _cloudManager.DeleteAnchorAsync(this.currentCloudAnchor).GetAwaiter().GetResult();
    }

#endregion

    public async void DeleteAnchor()
    {
        await _cloudManager.DeleteAnchorAsync(_currentCloudAnchor);
    }

    /// <summary>
    ///     Saves the current object anchor to the cloud.
    /// </summary>
    private void SaveCurrentObjectAnchorToCloud()
    {
        currentAppState = AppState.SavingCloudSpatialAnchor;
        var localCloudAnchor = new CloudSpatialAnchor();

        localCloudAnchor.LocalAnchor = _otherSpawnedObjects[0].GetNativeAnchorPointer();

        if (localCloudAnchor.LocalAnchor == IntPtr.Zero)
        {
            Debug.Log("Didn't get the local XR anchor pointer...");
            return;
        }

        // In this sample app we delete the cloud anchor explicitly, but here we show how to set an anchor to expire automatically
        localCloudAnchor.Expiration = DateTimeOffset.Now.AddDays(7);

        Task.Run(async () =>
        {
            while (!_cloudManager.EnoughDataToCreate)
            {
                await Task.Delay(330);
                var createProgress = _cloudManager.GetSessionStatusIndicator(AzureSpatialAnchorsDemoWrapper
                    .SessionStatusIndicatorType.RecommendedForCreate);
                QueueOnUpdate(() =>
                    _feedbackBox.text = $"Move your device to capture more environment data: {createProgress:0%}");
            }

            var success = false;
            try
            {
                QueueOnUpdate(() => _feedbackBox.text = "Saving...");
                
                _currentCloudAnchor = await _cloudManager.StoreAnchorInCloud(localCloudAnchor);

                success = _currentCloudAnchor != null;
                localCloudAnchor = null;

                if (success && !_isErrorActive)
                {
                    OnSaveCloudAnchorSuccessful();
                }
                else
                {
                    currentAppState = AppState.SavingCloudSpatialAnchorFailed;
                    Debug.Log(new Exception("Failed to save, but no exception was thrown."));
                }
            }
            catch (Exception ex)
            {
                currentAppState = AppState.SavingCloudSpatialAnchorFailed;
                Debug.Log(ex);
            }
        });
    }

    protected async void OnSaveCloudAnchorSuccessful()
    {
        long anchorNumber = -1;

        this._localAnchorIds.Add(_currentCloudAnchor.Identifier);

#if !UNITY_EDITOR
            anchorNumber = (await this.anchorExchanger.StoreAnchorKey(_currentCloudAnchor.Identifier));
#endif

        this.QueueOnUpdate(new Action(() =>
        {
            Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = _currentCloudAnchor.GetAnchorPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            this.SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

            this.currentAppState = AppState.SavedCloudSpatialAnchor;
        }));
    }

#region CloudManager events

    private void CloudManager_OnSessionError(object sender, SessionErrorEventArgs args)
    {
        _isErrorActive = true;
        Debug.Log(string.Format("Error: {0}", args.ErrorMessage));
    }

    private void CloudManager_OnLogDebug(object sender, OnLogDebugEventArgs args)
    {
        Debug.Log(args.Message);
    }

    private void CloudManager_OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
    {
        Debug.LogFormat("Anchor recognized as a possible anchor {0} {1}", args.Identifier, args.Status);

        if (currentAppState == AppState.AnchorReset)
        {
            if(args.Anchor!=null)
                _resetAnchors.Add(args.Anchor);
        }


        switch (args.Status)
        {
            case LocateAnchorStatus.Located:
                // Go add your anchor to the scene...
                OnCloudAnchorLocated(args);
                break;
            case LocateAnchorStatus.AlreadyTracked:
                // This anchor has already been reported and is being tracked
                break;
            case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                currentAppState = AppState.AnchorNotFounded;
                // The anchor was deleted or never existed in the first place
                // Drop it, or show UI to ask user to anchor the content anew
                break;
            case LocateAnchorStatus.NotLocated:
                // The anchor hasn't been found given the location data
                // The user might in the wrong location, or maybe more data will help
                // Show UI to tell user to keep looking around
                break;
        }
    }


#endregion

#region Anchor Spawn

    private void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
    {
        _currentCloudAnchor = args.Anchor;
        var nextCsa = _currentCloudAnchor;

        QueueOnUpdate(() =>
        {
            var anchorPose = Pose.identity;
            _anchorsLocated++;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = _currentCloudAnchor.GetAnchorPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            var spawnNewAnchoredObject = SpawnNewAnchoredObject(anchorPose.position, anchorPose.rotation, _currentCloudAnchor);
            _otherSpawnedObjects.Add(spawnNewAnchoredObject);

            if (_anchorsLocated >= _anchorsExpected)
            {
                this.currentAppState = AppState.LocatedAnchors;
            }
        });
    }

    /// <summary>
    ///     Spawns a new anchored object and makes it the current object or moves the
    ///     current anchored object if one exists.
    /// </summary>
    /// <param name="worldPos">The world position.</param>
    /// <param name="worldRot">The world rotation.</param>
    /// <param name="currentCloudAnchor"></param>
    private void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot,
        CloudSpatialAnchor currentCloudAnchor=null)
    {
        // Create the object if we need to, and attach the platform appropriate
        // Anchor behavior to the spawned object
        if (_otherSpawnedObjects.Count == 0)
        {
            _otherSpawnedObjects.Add(SpawnNewAnchoredObject(worldPos, worldRot, _currentCloudAnchor));
        }
        else
        {
            MoveAnchoredObject(_otherSpawnedObjects[0], worldPos, worldRot, _currentCloudAnchor);
        }
    }

    private void MoveAnchoredObject(GameObject objectToMove, Vector3 worldPos, Quaternion worldRot,
        CloudSpatialAnchor cloudSpatialAnchor = null)
    {
#if UNITY_ANDROID || UNITY_IOS
        // On Android and iOS, we expect the position and rotation to be passed in.
        objectToMove.RemoveARAnchor();
        objectToMove.transform.position = worldPos;
        objectToMove.transform.rotation = worldRot;
        objectToMove.AddARAnchor();
#elif WINDOWS_UWP || UNITY_WSA
// On HoloLens, if we do not have a cloudAnchor already, we will position the
// object based on the passed in worldPos/worldRot. Then we attach a new world anchor
// so we are ready to commit the anchor to the cloud if requested.
// If we do have a cloudAnchor, we will use it's pointer to setup the world anchor
// This will position the object automatically.
            if (cloudSpatialAnchor == null)
            {
                objectToMove.RemoveARAnchor();
                objectToMove.transform.position = worldPos;
                objectToMove.transform.rotation = worldRot;
                objectToMove.AddARAnchor();
            }
            else
            {
                objectToMove.GetComponent<UnityEngine.XR.WSA.WorldAnchor>().SetNativeSpatialAnchorPtr(cloudSpatialAnchor.LocalAnchor);
            }
#else
        throw new PlatformNotSupportedException();
#endif
    }

    /// <summary>
    ///     Spawns a new anchored object.
    /// </summary>
    /// <param name="worldPos">The world position.</param>
    /// <param name="worldRot">The world rotation.</param>
    /// <returns><see cref="GameObject" />.</returns>
    private GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot)
    {
        var newGameObject = Instantiate(AnchoredObjectPrefab, worldPos, worldRot);
        newGameObject.AddARAnchor();

        return newGameObject;
    }

    /// <summary>
    ///     Spawns a new object.
    /// </summary>
    /// <param name="worldPos">The world position.</param>
    /// <param name="worldRot">The world rotation.</param>
    /// <param name="cloudSpatialAnchor">The cloud spatial anchor.</param>
    /// <returns><see cref="GameObject" />.</returns>
    private GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot,
        CloudSpatialAnchor cloudSpatialAnchor)
    {
        var newGameObject = SpawnNewAnchoredObject(worldPos, worldRot);

#if WINDOWS_UWP || UNITY_WSA
// On HoloLens, if we do not have a cloudAnchor already, we will have already positioned the
// object based on the passed in worldPos/worldRot and attached a new world anchor,
// so we are ready to commit the anchor to the cloud if requested.
// If we do have a cloudAnchor, we will use it's pointer to setup the world anchor,
// which will position the object automatically.
            if (cloudSpatialAnchor != null)
            {
                newGameObject.GetComponent<UnityEngine.XR.WSA.WorldAnchor>().SetNativeSpatialAnchorPtr(cloudSpatialAnchor.LocalAnchor);
            }
#endif

        return newGameObject;
    }

#endregion

    private bool SanityCheckAccessConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_cloudManager.SpatialAnchorsAccountId) ||
            string.IsNullOrWhiteSpace(_cloudManager.SpatialAnchorsAccountKey))
        {
            return false;
        }

        return true;
    }

    private bool IsPlacingObject()
    {
        return this.currentAppState == AppState.RetrySavingCloudSpatialAnchor || this.currentAppState == AppState.AnchorNotFounded;
    }

    /// <summary>
    /// Called when a touch object interaction occurs.
    /// </summary>
    /// <param name="hitPoint">The position.</param>
    /// <param name="target">The target.</param>
    protected override void OnSelectObjectInteraction(Vector3 hitPoint, object target)
    {
        if (this.IsPlacingObject())
        {
            Quaternion rotation = Quaternion.AngleAxis(0, Vector3.up);

            this.SpawnOrMoveCurrentAnchoredObject(hitPoint, rotation);
        }

    }

    /// <summary>
    /// Called when a select interaction occurs.
    /// </summary>
    /// <remarks>Currently only called for HoloLens.</remarks>
    protected override void OnSelectInteraction()
    {
#if WINDOWS_UWP || UNITY_WSA

        // On HoloLens, we just advance the demo.
        this.QueueOnUpdate(new Action(() => this.Hoge()));
#endif

        base.OnSelectInteraction();
    }

    /// <summary>
    /// Called when a touch interaction occurs.
    /// </summary>
    /// <param name="touch">The touch.</param>
    protected override void OnTouchInteraction(Touch touch)
    {
        if (this.IsPlacingObject())
        {
            base.OnTouchInteraction(touch);
        }
    }

    internal enum AppState
    {
        Ready = 0,
        AnchorSearching,
        AnchorFounded,
        AnchorNotFounded,
        StartSavingCloudSpatialAnchor,
        SavedCloudSpatialAnchor,
        SavingCloudSpatialAnchorFailed,
        RetrySavingCloudSpatialAnchor,
        Done,
        LocatingAnchors,
        LocatedAnchors,
        SavingCloudSpatialAnchor,
        AnchorReset,
    }
}