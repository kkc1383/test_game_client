using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public string serverUrl = "ws://13.125.69.84:9002";
    public bool isLocalTest;

    private WebSocket websocket;
    private int myPlayerId = -1;
    private string myNickname;

    // 이벤트: 게임 로직과 분리
    public event Action<JObject> OnJoinResponse;
    public event Action<JObject> OnGameStateReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public int MyPlayerId => myPlayerId;
    public string MyNickname => myNickname;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        serverUrl = isLocalTest ? "ws://localhost:9002" : serverUrl;
    }

    private async void Start()
    {
        myNickname = PlayerPrefs.GetString("PlayerNickname", "Player");

        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += OnWebSocketConnected;
        websocket.OnMessage += OnMessageReceived;
        websocket.OnClose += OnWebSocketDisconnected;
        websocket.OnError += (e) => Debug.LogError($"WebSocket Error: {e}");

        await websocket.Connect();
    }

    private void OnWebSocketConnected()
    {
        Debug.Log("Connected to server!");
        OnConnected?.Invoke();

        // 저장된 색상 불러오기
        float colorR = PlayerPrefs.GetFloat("PlayerColorR", 1.0f);
        float colorG = PlayerPrefs.GetFloat("PlayerColorG", 0.3f);
        float colorB = PlayerPrefs.GetFloat("PlayerColorB", 0.3f);

        var joinRequest = new
        {
            type = 1, // JOIN_REQUEST
            nickname = myNickname,
            color = new float[] { colorR, colorG, colorB }
        };

        string json = JsonConvert.SerializeObject(joinRequest);
        SendMessage(json);
        Debug.Log($"Join request sent: {myNickname}, Color: ({colorR}, {colorG}, {colorB})");
    }

    private void OnMessageReceived(byte[] data)
    {
        string json = System.Text.Encoding.UTF8.GetString(data);

        try
        {
            JObject obj = JObject.Parse(json);
            int msgType = obj["type"].Value<int>();

            switch (msgType)
            {
                case 2: // JOIN_RESPONSE
                    HandleJoinResponse(obj);
                    break;
                case 4: // GAME_STATE
                    OnGameStateReceived?.Invoke(obj);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON Parse Error: {e.Message}\n{json}");
        }
    }

    private void HandleJoinResponse(JObject obj)
    {
        bool success = obj["success"].Value<bool>();

        if (success)
        {
            myPlayerId = obj["playerId"].Value<int>();
            string nickname = obj["nickname"].Value<string>();
            Debug.Log($"Joined as Player {myPlayerId} with nickname '{nickname}'");
        }
        else
        {
            string message = obj["message"].Value<string>();
            Debug.LogError($"Join failed: {message}");

            // 서버가 꽉 찼을 경우 LoginScene으로 돌아가기
            if (message.Contains("full") || message.Contains("Full"))
            {
                PlayerPrefs.SetString("ServerFullError", message);
                PlayerPrefs.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
            }
        }

        OnJoinResponse?.Invoke(obj);
    }

    public void SendInput(Vector3 movement)
    {
        if (myPlayerId < 0 || websocket == null || websocket.State != WebSocketState.Open)
            return;

        var input = new
        {
            type = 3, // PLAYER_INPUT
            playerId = myPlayerId,
            x = movement.x,
            y = movement.y,
            z = movement.z
        };

        string json = JsonConvert.SerializeObject(input);
        SendMessage(json);
    }

    public void SendJumpCommand(int playerId)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
            return;

        var jumpMsg = new
        {
            type = 5, // JUMP_COMMAND
            playerId = playerId
        };

        string json = JsonConvert.SerializeObject(jumpMsg);
        SendMessage(json);
    }

    public void SendSpawnDummies(int count = 10)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
            return;

        var msg = new
        {
            type = 6, // SPAWN_DUMMIES
            count = count
        };

        string json = JsonConvert.SerializeObject(msg);
        SendMessage(json);
        Debug.Log($"Spawn {count} dummies requested");
    }

    public void SendDeleteAllDummies()
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
            return;

        var msg = new
        {
            type = 7 // DELETE_ALL_DUMMIES
        };

        string json = JsonConvert.SerializeObject(msg);
        SendMessage(json);
        Debug.Log("Delete all dummies requested");
    }

    private new void SendMessage(string message)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.SendText(message);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private void OnWebSocketDisconnected(WebSocketCloseCode closeCode)
    {
        Debug.Log($"Disconnected from server: {closeCode}");
        OnDisconnected?.Invoke();
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }
}
