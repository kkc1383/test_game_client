using TMPro;
using UnityEngine;
public class PlayerController : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private string playerNickname;
    [SerializeField] private int playerId;

    [Header("Nickname UI")]
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private Canvas nicknameCanvas;

    [Header("Movement - Client side Only")]
    [SerializeField] private float moveSpeed = 5f;

    private bool isLocalPlayer = false; //보간용 변수
    private Vector3 targetPosition;
    private Vector3 targetVelocity;
    private float positionLerpSpeed = 10f;
    private CameraFollow cameraFollow;
    void Start()
    {
        targetPosition = transform.position; //카메라 참조 가져오기
        if (Camera.main != null) { cameraFollow = Camera.main.GetComponent<CameraFollow>(); }
    }
    void Update()
    {
        if (isLocalPlayer)
        {
            //로컬 플레이어: 입력만 서버로 전송
            HandleInput();
        }
        else
        {
            //다른 플레이어: 서버 위치로 부드럽게 이동(보간)
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
        }
        //닉네임 빌보드
        if (nicknameCanvas != null && Camera.main != null)
        {
            nicknameCanvas.transform.LookAt(Camera.main.transform);
            nicknameCanvas.transform.Rotate(0, 180, 0);
        }
    }
    void HandleInput()
    {
        //WASD 입력
        float horizontal = Input.GetAxisRaw("Horizontal");// A / D
        float vertical = Input.GetAxisRaw("Vertical"); // W / S  이동 입력 처리

        if (horizontal != 0 || vertical != 0)
        {
            //카메라 기준으로 방향 변환
            Vector3 movement = GetCameraRelativeMovement(horizontal, vertical);
            //정규화(대각선 이동 시 속도 일정하게)
            if (movement.magnitude > 1f)
            {
                movement.Normalize();
            }
            //네트워크로 전송
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendInput(movement);
            }
        }
        else
        {
            //입력 없으면 정지
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendInput(Vector3.zero);
            }
        }
        //점프 입력 처리
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendJumpCommand(playerId);
            }
        }
    }
    //카메라 회전 기준으로 입력 변환
    Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
    {
        if (cameraFollow == null)
        {
            //카메라 없으면 월드 기준
            return new Vector3(horizontal, 0, vertical);
        }
        //카메라 회전 각도 가져오기
        float cameraRotation = cameraFollow.CurrentRotationY;
        //회전 행렬 적용
        float angleRad = cameraRotation * Mathf.Deg2Rad;
        //입력을 카메라 기준으로 회전
        float rotatedX = horizontal * Mathf.Cos(angleRad) - vertical * Mathf.Sin(angleRad); float rotatedZ = horizontal * Mathf.Sin(angleRad) + vertical * Mathf.Cos(angleRad); return new Vector3(rotatedX, 0, rotatedZ);
    }
    //서버로부터 받은 위치 업데이트
    public void UpdateFromServer(Vector3 position, Vector3 velocity)
    {
        targetPosition = position; targetVelocity = velocity;
        //로컬 플레이어는 서버가 보낸 위치로 즉시 스냅(서버 조정 - Server Reconciliation)
        if (isLocalPlayer)
        {
            //위치 차이가 크면 즉시 보정
            float positionError = Vector3.Distance(transform.position, position);

            if (positionError > 0.5f)  //오차 임계값
            {
                transform.position = position;
            }
            else
            {
                //작은 오차는 부드럽게 보정
                transform.position = Vector3.Lerp(transform.position, position, 0.1f);
            }
        }
    }
    public void SetNickname(string nickname)
    {
        playerNickname = nickname;
        if (nicknameText != null)
        {
            nicknameText.text = nickname;
        }
    }
    public void SetPlayerId(int id)
    {
        playerId = id;
    }
    public void SetAsLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal; if (isLocal)
        {
            //카메라가 이 플레이어를 따라가도록 설정
            if (cameraFollow != null) { cameraFollow.SetTarget(this.transform); }
            //로컬 플레이어 표시(선택사항: 색상은 GameManager가 설정)
            Debug.Log($"This is my player: {playerNickname}");
        }
    }
    public string GetNickname()
    {
        return playerNickname;
    }
    public int GetPlayerId()
    {
        return playerId;
    }
}