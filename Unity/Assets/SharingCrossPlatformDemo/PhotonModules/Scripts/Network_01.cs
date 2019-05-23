using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class Network_01 : MonoBehaviourPunCallbacks
{
    /////////////////////////////////////////////////////////////////////////////////////
    // Awake & Start ////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////

    // Awake
    private void Awake()
    {
        // シーンの自動同期: 無効
        PhotonNetwork.AutomaticallySyncScene = false;
    }


    // Use this for initialization
    private void Start()
    {
        // Photonに接続
        Connect("1.0");
    }


    /////////////////////////////////////////////////////////////////////////////////////
    // Connect //////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////

    // Photonに接続する
    private void Connect(string gameVersion)
    {
        if (PhotonNetwork.IsConnected == false)
        {
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }


    // ニックネームを付ける
    private void SetMyNickName(string nickName)
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LocalPlayer.NickName = nickName;
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////
    // Join Lobby ///////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////

    // ロビーに入る
    private void JoinLobby()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////
    // Pun Callbacks ////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////

    // Photonに接続した時
    public override void OnConnected()
    {
        Debug.Log("OnConnected");

        // ニックネームを付ける
        SetMyNickName("Knohhoso");
    }


    // Photonから切断された時
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("OnDisconnected");
    }


    // マスターサーバーに接続した時
    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster");

        // ロビーに入る
        JoinLobby();
    }


    // ロビーに入った時
    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby");
    }


    // ロビーから出た時
    public override void OnLeftLobby()
    {
        Debug.Log("OnLeftLobby");
    }


    // 部屋を作成した時
    public override void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom");
    }


    // 部屋の作成に失敗した時
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("OnCreateRoomFailed");
    }


    // 部屋に入室した時
    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
    }


    // 特定の部屋への入室に失敗した時
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("OnJoinRoomFailed");
    }


    // ランダムな部屋への入室に失敗した時
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("OnJoinRandomFailed");
    }


    // 部屋から退室した時
    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");
    }


    // 他のプレイヤーが入室してきた時
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("OnPlayerEnteredRoom");
    }


    // 他のプレイヤーが退室した時
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("OnPlayerLeftRoom");
    }


    // マスタークライアントが変わった時
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("OnMasterClientSwitched");
    }


    // ロビーに更新があった時
    public override void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        Debug.Log("OnLobbyStatisticsUpdate");
    }


    // ルームリストに更新があった時
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("OnRoomListUpdate");
    }


    // ルームプロパティが更新された時
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("OnRoomPropertiesUpdate");
    }


    // プレイヤープロパティが更新された時
    public override void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.Log("OnPlayerPropertiesUpdate");
    }


    // フレンドリストに更新があった時
    public override void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        Debug.Log("OnFriendListUpdate");
    }


    // 地域リストを受け取った時
    public override void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log("OnRegionListReceived");
    }


    // WebRpcのレスポンスがあった時
    public override void OnWebRpcResponse(OperationResponse response)
    {
        Debug.Log("OnWebRpcResponse");
    }


    // カスタム認証のレスポンスがあった時
    public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("OnCustomAuthenticationResponse");
    }


    // カスタム認証が失敗した時
    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log("OnCustomAuthenticationFailed");
    }
}
