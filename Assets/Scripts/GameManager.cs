using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject dummyPrefab;

    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> dummies = new Dictionary<int, GameObject>();

    private CameraFollow cameraFollow;

    void Start()
    {
        cameraFollow = Camera.main?.GetComponent<CameraFollow>();

        // NetworkManager 이벤트 구독
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateReceived += HandleGameState;
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
        }
    }

    void HandleGameState(JObject obj)
    {
        // 플레이어 업데이트
        UpdatePlayers(obj);

        // 더미 업데이트
        UpdateDummies(obj);
    }

    void UpdatePlayers(JObject obj)
    {
        JArray playersArray = (JArray)obj["players"];
        HashSet<int> currentPlayers = new HashSet<int>();

        foreach (JObject playerData in playersArray)
        {
            int id = playerData["id"].Value<int>();
            string nickname = playerData["nickname"].Value<string>();
            JArray pos = (JArray)playerData["pos"];
            JArray vel = (JArray)playerData["vel"];

            currentPlayers.Add(id);

            Vector3 position = new Vector3(
                pos[0].Value<float>(),
                pos[1].Value<float>(),
                pos[2].Value<float>()
            );

            Vector3 velocity = new Vector3(
                vel[0].Value<float>(),
                vel[1].Value<float>(),
                vel[2].Value<float>()
            );

            // 색상 정보 파싱 (서버에서 제공하는 경우)
            Color playerColor = Color.white;
            if (playerData.ContainsKey("color"))
            {
                JArray colorArray = (JArray)playerData["color"];
                playerColor = new Color(
                    colorArray[0].Value<float>(),
                    colorArray[1].Value<float>(),
                    colorArray[2].Value<float>()
                );
            }

            // 플레이어가 없으면 생성
            if (!players.ContainsKey(id))
            {
                CreatePlayer(id, nickname, position, playerColor);
            }
            else
            {
                // 위치 업데이트
                GameObject player = players[id];
                PlayerController controller = player.GetComponent<PlayerController>();

                if (controller != null)
                {
                    controller.UpdateFromServer(position, velocity);
                }
            }
        }

        // 연결 끊긴 플레이어 제거
        List<int> toRemove = new List<int>();
        foreach (var kvp in players)
        {
            if (!currentPlayers.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            Debug.Log($"Removing disconnected player {id}");
            Destroy(players[id]);
            players.Remove(id);
        }
    }

    void CreatePlayer(int id, string nickname, Vector3 position, Color color)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned!");
            return;
        }

        GameObject playerObj = Instantiate(playerPrefab, position, Quaternion.identity);
        playerObj.name = $"Player_{id}_{nickname}";

        // 색상 적용
        Renderer renderer = playerObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        PlayerController controller = playerObj.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SetPlayerId(id);
            controller.SetNickname(nickname);

            // 내 플레이어인지 확인
            bool isLocalPlayer = (id == NetworkManager.Instance.MyPlayerId);
            controller.SetAsLocalPlayer(isLocalPlayer);

            // 내 플레이어면 카메라 타겟 설정
            if (isLocalPlayer && cameraFollow != null)
            {
                cameraFollow.SetTarget(playerObj.transform);
            }
        }

        players[id] = playerObj;
        Debug.Log($"Created player {id} ({nickname}) with color ({color.r}, {color.g}, {color.b})");
    }

    void UpdateDummies(JObject obj)
    {
        if (!obj.ContainsKey("dummies"))
            return;

        JArray dummiesArray = (JArray)obj["dummies"];
        HashSet<int> currentDummies = new HashSet<int>();

        foreach (JObject dummyData in dummiesArray)
        {
            int id = dummyData["id"].Value<int>();
            JArray pos = (JArray)dummyData["pos"];

            currentDummies.Add(id);

            Vector3 position = new Vector3(
                pos[0].Value<float>(),
                pos[1].Value<float>(),
                pos[2].Value<float>()
            );

            // 더미가 없으면 생성
            if (!dummies.ContainsKey(id))
            {
                CreateDummy(id, position);
            }
            else
            {
                // 위치 업데이트 (부드러운 보간)
                GameObject dummy = dummies[id];
                dummy.transform.position = Vector3.Lerp(
                    dummy.transform.position,
                    position,
                    Time.deltaTime * 10f
                );
            }
        }

        // 서버에 없는 더미 제거
        List<int> toRemove = new List<int>();
        foreach (var kvp in dummies)
        {
            if (!currentDummies.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            Debug.Log($"Removing dummy {id}");
            Destroy(dummies[id]);
            dummies.Remove(id);
        }
    }

    void CreateDummy(int id, Vector3 position)
    {
        if (dummyPrefab == null)
        {
            Debug.LogWarning("Dummy Prefab is not assigned!");
            return;
        }

        GameObject dummyObj = Instantiate(dummyPrefab, position, Quaternion.identity);
        dummyObj.name = $"Dummy_{id}";

        dummies[id] = dummyObj;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateReceived -= HandleGameState;
        }
    }

    // 공개 메서드들
    public int GetPlayerCount() => players.Count;
    public int GetDummyCount() => dummies.Count;
}
