// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS
using Microsoft.Azure.SpatialAnchors.Unity.Samples.IOS.ARKit;
using UnityEngine.XR.iOS;
#elif UNITY_ANDROID
using Microsoft.Azure.SpatialAnchors.Unity.Samples.Android;
#elif UNITY_WSA || WINDOWS_UWP
using UnityEngine.XR.WSA;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
{
    /// <summary>
    /// Use this behavior to manage an Azure Spatial Service session for your game or app.
    /// アプリケーションのためのAzure Spatial Anchorを管理します。
    /// </summary>
    public class AzureSpatialAnchorsDemoWrapper : MonoBehaviour
    {
        /// <summary>
        /// 静的インスタンスフィールド
        /// </summary>
        private static AzureSpatialAnchorsDemoWrapper _instance;

        /// <summary>
        /// Single instance of the anchor manager.
        /// Anchor管理クラスの唯一のインスタンスを取得します。
        /// </summary>
        public static AzureSpatialAnchorsDemoWrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AzureSpatialAnchorsDemoWrapper>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Set this string to the Spatial Anchors account ID provided in the Spatial Anchors resource.
        /// Azure Spatial Anchorで提供されるSpatial Anchors account IDの文字列を設定します。
        /// </summary>
        public string SpatialAnchorsAccountId = "";

        /// <summary>
        /// Set this string to the Spatial Anchors account key provided in the Spatial Anchors resource.
        /// Azure Spatial Anchorで提供されるSpatial Anchors account keyの文字列を設定します。
        /// </summary>
        public string SpatialAnchorsAccountKey = "";

        /// <summary>
        /// These events are not wired to the actual CloudSpatialAnchorSession, but will
        /// act as a proxy to forward events from the CloudSpatialAnchorSession to the
        /// subscriber.
        /// これらのイベントはCloudSpatialAnchorSessionのイベントとして直接接続されていませんが、
        /// CloudSpatialAnchorSessionのイベントを転送するためのプロキシとして機能します。
        /// </summary>
        public event AnchorLocatedDelegate OnAnchorLocated;
        public event LocateAnchorsCompletedDelegate OnLocateAnchorsCompleted;
        public event SessionErrorDelegate OnSessionError;
        public event SessionUpdatedDelegate OnSessionUpdated;
        public event OnLogDebugDelegate OnLogDebug;

        /// <summary>
        /// 現在のセッション状況を表す列挙体です。
        /// </summary>
        public enum SessionStatusIndicatorType
        {
            RecommendedForCreate = 0,
            ReadyForCreate,
            RecommendedForQuery,
            ReadyForQuery
        }

        private readonly float[] SessionStatusIndicators = new float[4];

        /// <summary>
        /// 現在のセッション状況に対する状況を取得します。
        /// </summary>
        /// <param name="indicatorType"><see cref="SessionStatusIndicatorType"/></param>
        /// <returns></returns>
        public float GetSessionStatusIndicator(SessionStatusIndicatorType indicatorType)
        {

            return SessionStatusIndicators[(int)indicatorType];
        }

        private bool enableProcessing;

        /// <summary>
        /// Azure Spatial Anchorの管理昨日の処理中/停止を設定します。
        /// このプロパティをTrueで設定すると<see cref="CloudSpatialAnchorSession"/>のセッションが開始されます。
        /// </summary>
        public bool EnableProcessing
        {
            get
            {
                return enableProcessing;
            }
            set
            {
                if (enableProcessing != value)
                {
                    enableProcessing = value;
                    Debug.Log($"Processing {enableProcessing}");
                    if (enableProcessing)
                    {
                        cloudSpatialAnchorSession.Start();
                    }
                    else
                    {
                        cloudSpatialAnchorSession.Stop();
                    }
                }
            }
        }

        public bool EnoughDataToCreate => this.GetSessionStatusIndicator(SessionStatusIndicatorType.RecommendedForCreate) >= 1;

        public bool EnoughDataToQuery => this.GetSessionStatusIndicator(SessionStatusIndicatorType.ReadyForQuery) >= 1;

        private readonly Queue<Action> dispatchQueue = new Queue<Action>();

        private readonly List<string> AnchorIdsToLocate = new List<string>();

        private CloudSpatialAnchorSession cloudSpatialAnchorSession = null;

        private AnchorLocateCriteria anchorLocateCriteria = null;

        // Use this for initialization
        /// <summary>
        /// cloud 
        /// </summary>
        private void Start()
        {
            
            //クラウド上に格納されたAnchorの情報を検索するためのクラスをインスタンス化します。
            anchorLocateCriteria = new AnchorLocateCriteria();

            //CloudSpatialAnchorSessionのインスタンス化とセッション接続の準備のためにCreateNewCloudSessionを呼出します。
            //デバイス毎に前処理が発生します。
#if UNITY_IOS
        arkitSession = UnityARSessionNativeInterface.GetARSessionNativeInterface();
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += UnityARSessionNativeInterface_ARFrameUpdatedEvent;
#endif
#if UNITY_ANDROID
            UnityAndroidHelper.Instance.DispatchUiThread(unityActivity =>
            {
                using (AndroidJavaClass cloudServices = new AndroidJavaClass("com.microsoft.CloudServices"))
                {
                    cloudServices.CallStatic("initialize", unityActivity);
                    this.CreateNewCloudSession();
                }
            });
#else
        CreateNewCloudSession();
#endif
        }

        private void Update()
        {
#if UNITY_ANDROID
            ProcessLatestFrame();
#endif
            lock (dispatchQueue)
            {
                while (dispatchQueue.Count > 0)
                {
                    dispatchQueue.Dequeue()();
                }
            }
        }

        private void OnDestroy()
        {
            enableProcessing = false;

            if (cloudSpatialAnchorSession != null)
            {
                cloudSpatialAnchorSession.Dispose();
                cloudSpatialAnchorSession = null;
            }

            if (anchorLocateCriteria != null)
            {
                anchorLocateCriteria = null;
            }

            _instance = null;
        }

        private void CloudSpatialAnchorSession_LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            OnLocateAnchorsCompleted?.Invoke(sender, args);
        }

        private void CloudSpatialAnchorSession_AnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            OnAnchorLocated?.Invoke(sender, args);
        }

        private void CloudSpatialAnchorSession_Error(object sender, SessionErrorEventArgs args)
        {
            OnSessionError?.Invoke(sender, args);
        }

        private void CloudSpatialAnchorSession_SessionUpdated(object sender, SessionUpdatedEventArgs args)
        {
            SessionStatusIndicators[(int)SessionStatusIndicatorType.ReadyForCreate] = args.Status.ReadyForCreateProgress;
            SessionStatusIndicators[(int)SessionStatusIndicatorType.RecommendedForCreate] = args.Status.RecommendedForCreateProgress;

            OnSessionUpdated?.Invoke(sender, args);
        }

        private void CloudSpatialAnchorSession_OnLogDebug(object sender, OnLogDebugEventArgs args)
        {
            OnLogDebug?.Invoke(sender, args);
        }

        public void ResetSession(Action completionRoutine = null)
        {
            bool processingWasEnabled = this.EnableProcessing;
            this.EnableProcessing = false;
            this.cloudSpatialAnchorSession.Reset();
            this.ResetSessionStatusIndicators();
            Task.Run(async () =>
            {
                while (this.LocateOperationInFlight())
                {
                    await Task.Yield();
                }
                lock (this.dispatchQueue)
                {
                    this.dispatchQueue.Enqueue(() =>
                    {
                        this.EnableProcessing = processingWasEnabled;
                        completionRoutine?.Invoke();
                    });
                }
            });
        }

        public void ResetSessionStatusIndicators()
        {
            for (int i = 0; i < this.SessionStatusIndicators.Length; i++)
            {
                this.SessionStatusIndicators[i] = 0;
            }
        }

        private void CreateNewCloudSession()
        {
            //クラウド用のセッションクラスをインスタンス化します。
            cloudSpatialAnchorSession = new CloudSpatialAnchorSession();

            //Azure Spatial Anchorの各種キーを設定します。
            cloudSpatialAnchorSession.Configuration.AccountId = SpatialAnchorsAccountId.Trim();
            cloudSpatialAnchorSession.Configuration.AccountKey = SpatialAnchorsAccountKey.Trim();

            //セッション状態をトレースするためのログ出力の設定を行います。
            cloudSpatialAnchorSession.LogLevel = SessionLogLevel.Information;

            //ログ出力時に発生するイベント
            cloudSpatialAnchorSession.OnLogDebug += CloudSpatialAnchorSession_OnLogDebug;

            //セション状態が変わった場合に発生するイベントです。
            cloudSpatialAnchorSession.SessionUpdated += CloudSpatialAnchorSession_SessionUpdated;

            //Anchor情報アンカーの探知/探知失敗時に発生するイベントです。
            cloudSpatialAnchorSession.AnchorLocated += CloudSpatialAnchorSession_AnchorLocated;

            //監視対象のアンカーをすべて探知し終わった際に発生するイベントです。
            cloudSpatialAnchorSession.LocateAnchorsCompleted += CloudSpatialAnchorSession_LocateAnchorsCompleted;

            //エラー発生時に発生するイベントです。
            cloudSpatialAnchorSession.Error += CloudSpatialAnchorSession_Error;

            //以下の処理はアカウントキーの代わりにAzureのトークンを使用する場合に有効化します。
#if UNITY_WSA && !UNITY_EDITOR
            // AAD user token scenario to get an authentication token
            //cloudSpatialAnchorSession.TokenRequired += async (object sender, SpatialServices.TokenRequiredEventArgs args) =>
            //{
            //    CloudSpatialAnchorSessionDeferral deferral = args.GetDeferral();
            //    // AAD user token scenario to get an authentication token
            //    args.AuthenticationToken = await AuthenticationHelper.GetAuthenticationTokenAsync();
            //    deferral.Complete();
            //};
#endif

        //アンカーの配置を処理するために使用するセションを設定します。
        //おそらく各デバイス毎の空間マッピングに関する情報を利用するための設定と思われます。
#if UNITY_IOS
        cloudSpatialAnchorSession.Session = arkitSession.GetNativeSessionPtr();
#elif UNITY_ANDROID
            cloudSpatialAnchorSession.Session = GoogleARCoreInternal.ARCoreAndroidLifecycleManager.Instance.NativeSession.SessionHandle;
#elif UNITY_WSA || WINDOWS_UWP
        // No need to set a native session pointer for HoloLens.
#else
        throw new NotSupportedException("The platform is not supported.");
#endif
        }

        public bool SessionValid()
        {
            return cloudSpatialAnchorSession != null;
        }

        public bool LocateOperationInFlight()
        {
            return (SessionValid() && cloudSpatialAnchorSession.GetActiveWatchers().Count > 0);
        }

        public void SetAnchorIdsToLocate(IEnumerable<string> anchorIds)
        {
            if (anchorIds == null)
            {
                throw new ArgumentNullException(nameof(anchorIds));
            }

            AnchorIdsToLocate.Clear();
            AnchorIdsToLocate.AddRange(anchorIds);
            anchorLocateCriteria.Identifiers = AnchorIdsToLocate.ToArray();
        }

        public void ResetAnchorIdsToLocate()
        {
            AnchorIdsToLocate.Clear();
            anchorLocateCriteria.Identifiers = new string[0];
        }

        public void SetNearbyAnchor(CloudSpatialAnchor nearbyAnchor, float DistanceInMeters, int MaxNearAnchorsToFind)
        {
            if (nearbyAnchor == null)
            {
                anchorLocateCriteria.NearAnchor = new NearAnchorCriteria();
                return;
            }

            NearAnchorCriteria nac = new NearAnchorCriteria();
            nac.SourceAnchor = nearbyAnchor;
            nac.DistanceInMeters = DistanceInMeters;
            nac.MaxResultCount = MaxNearAnchorsToFind;
            anchorLocateCriteria.NearAnchor = nac;
        }

        public void SetGraphEnabled(bool UseGraph, bool JustGraph = false)
        {
            anchorLocateCriteria.Strategy = UseGraph ?
                (JustGraph ? LocateStrategy.Relationship : LocateStrategy.AnyStrategy) :
                LocateStrategy.VisualInformation;
        }

        /// <summary>
        /// Bypassing the cache will force new queries to be sent for objects, allowing
        /// for refined poses over time.
        /// </summary>
        /// <param name="BypassCache"></param>
        public void SetBypassCache(bool BypassCache)
        {
            anchorLocateCriteria.BypassCache = BypassCache;
        }

        public CloudSpatialAnchorWatcher CreateWatcher()
        {
            if (SessionValid())
            {
                return cloudSpatialAnchorSession.CreateWatcher(anchorLocateCriteria);
            }
            else
            {
                return null;
            }
        }

        public async Task<CloudSpatialAnchor> StoreAnchorInCloud(CloudSpatialAnchor cloudSpatialAnchor)
        {
            if (SessionStatusIndicators[(int)SessionStatusIndicatorType.ReadyForCreate] < 1)
            {
                return null;
            }

            await cloudSpatialAnchorSession.CreateAnchorAsync(cloudSpatialAnchor);

            return cloudSpatialAnchor;
        }

        public async Task DeleteAnchorAsync(CloudSpatialAnchor cloudSpatialAnchor)
        {
            if (SessionValid())
            {
                await cloudSpatialAnchorSession.DeleteAnchorAsync(cloudSpatialAnchor);
            }
        }

#if UNITY_ANDROID
        long lastFrameProcessedTimeStamp;

        void ProcessLatestFrame()
        {
            if (!EnableProcessing)
            {
                return;
            }

            if (cloudSpatialAnchorSession == null)
            {
                throw new InvalidOperationException("Cloud spatial anchor session is not available.");
            }

            var nativeSession = GoogleARCoreInternal.ARCoreAndroidLifecycleManager.Instance.NativeSession;

            if (nativeSession.FrameHandle == IntPtr.Zero)
            {
                return;
            }

            long latestFrameTimeStamp = nativeSession.FrameApi.GetTimestamp();

            bool newFrameToProcess = latestFrameTimeStamp > lastFrameProcessedTimeStamp;

            if (newFrameToProcess)
            {
                cloudSpatialAnchorSession.ProcessFrame(nativeSession.FrameHandle);
                lastFrameProcessedTimeStamp = latestFrameTimeStamp;
            }
        }
#endif

#if UNITY_IOS
    UnityARSessionNativeInterface arkitSession;
    void UnityARSessionNativeInterface_ARFrameUpdatedEvent(UnityARCamera camera)
    {
        if (cloudSpatialAnchorSession != null && EnableProcessing)
        {
            cloudSpatialAnchorSession.ProcessFrame(arkitSession.GetNativeFramePtr());
        }
    }

    Matrix4x4 GetMatrix4x4FromUnityAr4x4(UnityARMatrix4x4 input)
    {
        Matrix4x4 retval = new Matrix4x4(input.column0, input.column1, input.column2, input.column3);
        return retval;
    }
#endif
    }
}
