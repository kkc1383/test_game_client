using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬에 배치되어 더미 클라이언트들을 생성하고 관리하는 MonoBehaviour 입니다.
/// </summary>
public class DummyClientManager : MonoBehaviour
{
    [SerializeField]
    private string serverUrl = "ws://13.125.69.84:9002";
    public bool isLocalTest;

    [SerializeField]
    [Tooltip("생성할 더미 클라이언트 수")]
    private int numberOfDummies = 10;

    [SerializeField]
    [Tooltip("더미 생성 시 간격 (서버 부하 분산)")]
    private float spawnInterval = 0.2f;

    private List<DummyClient> dummyClients = new List<DummyClient>();

    private void Awake()
    {
        serverUrl = isLocalTest ? "ws://localhost:9002" : serverUrl;
    }

    void Update()
    {
        // 생성된 모든 더미 클라이언트의 Update()를 호출해줍니다.
        // (메시지 큐 처리 및 입력 시뮬레이션을 위해 필수)
        foreach (var client in dummyClients)
        {
            client.Update(Time.deltaTime);
        }


        // --- 테스트용 UI ---
        // Z 키를 누르면 더미 1개 생성
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnOneDummy();
        }

        // X 키를 누르면 더미 많이 생성
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(SpawnDummies());
        }

        // C 키를 누르면 모든 더미 연결 종료
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            DisconnectAllDummies();
        }
    }

    private void SpawnOneDummy()
    {
        // 고유한 닉네임 생성 (예: Dummy_1, Dummy_2, ...)
        string dummyName = $"Dummy_{dummyClients.Count + 1}";

        Debug.Log($"--- Spawning {dummyName} ---");

        // 새 더미 클라이언트 생성 및 리스트에 추가
        DummyClient newClient = new DummyClient(serverUrl, dummyName);
        dummyClients.Add(newClient);

        Debug.Log($"Total dummies: {dummyClients.Count}");
    }

    private IEnumerator SpawnDummies()
    {
        Debug.Log($"--- Spawning {numberOfDummies} dummies ---");
        for (int i = 0; i < numberOfDummies; i++)
        {
            string dummyName = $"Dummy_{i + 1}";
            DummyClient newClient = new DummyClient(serverUrl, dummyName);
            dummyClients.Add(newClient);

            // 서버에 동시 접속 부하를 주지 않기 위해 약간의 딜레이를 줍니다.
            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log($"--- {dummyClients.Count} dummies created ---");
    }

    private void DisconnectAllDummies()
    {
        Debug.Log($"--- Disconnecting all {dummyClients.Count} dummies ---");
        foreach (var client in dummyClients)
        {
            client.Close();
        }
        dummyClients.Clear();
    }

    // 애플리케이션 종료 시 모든 더미가 깔끔하게 종료되도록 합니다.
    private void OnApplicationQuit()
    {
        DisconnectAllDummies();
    }
}
