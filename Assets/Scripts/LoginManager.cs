using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Color Selection")]
    [SerializeField] private Button[] colorButtons; // 색상 버튼 배열
    [SerializeField] private Image selectedColorIndicator; // 선택된 색상 표시

    private Color[] availableColors = new Color[]
    {
        new Color(1.0f, 0.3f, 0.3f), // 빨강
        new Color(0.3f, 1.0f, 0.3f), // 초록
        new Color(0.3f, 0.3f, 1.0f), // 파랑
        new Color(1.0f, 1.0f, 0.3f), // 노랑
        new Color(1.0f, 0.3f, 1.0f), // 마젠타
        new Color(0.3f, 1.0f, 1.0f), // 청록
        new Color(1.0f, 0.6f, 0.3f), // 주황
        new Color(0.6f, 0.3f, 1.0f)  // 보라
    };

    private int selectedColorIndex = 0;

    private void Start()
    {
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        nicknameInput.onSubmit.AddListener((text) => OnJoinButtonClicked());

        // 색상 버튼 설정
        SetupColorButtons();

        // 저장된 색상 불러오기
        selectedColorIndex = PlayerPrefs.GetInt("PlayerColorIndex", 0);
        UpdateColorIndicator();

        // 서버 만원으로 돌아왔는지 확인
        if (PlayerPrefs.HasKey("ServerFullError"))
        {
            ShowError(PlayerPrefs.GetString("ServerFullError"));
            PlayerPrefs.DeleteKey("ServerFullError");
        }
    }

    private void SetupColorButtons()
    {
        if (colorButtons == null || colorButtons.Length == 0)
        {
            Debug.LogWarning("Color buttons not assigned!");
            return;
        }

        for (int i = 0; i < colorButtons.Length && i < availableColors.Length; i++)
        {
            int index = i; // 클로저를 위한 로컬 변수
            colorButtons[i].GetComponent<Image>().color = availableColors[i];
            colorButtons[i].onClick.AddListener(() => OnColorSelected(index));
        }
    }

    private void OnColorSelected(int colorIndex)
    {
        selectedColorIndex = colorIndex;
        UpdateColorIndicator();
        Debug.Log($"Selected color index: {colorIndex}");
    }

    private void UpdateColorIndicator()
    {
        if (selectedColorIndicator != null && selectedColorIndex < availableColors.Length)
        {
            selectedColorIndicator.color = availableColors[selectedColorIndex];
        }

        // 모든 버튼의 외곽선 초기화
        if (colorButtons != null)
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                Outline outline = colorButtons[i].GetComponent<Outline>();
                if (outline == null)
                {
                    outline = colorButtons[i].gameObject.AddComponent<Outline>();
                }

                if (i == selectedColorIndex)
                {
                    outline.effectColor = Color.white;
                    outline.effectDistance = new Vector2(3, 3);
                    outline.enabled = true;
                }
                else
                {
                    outline.enabled = false;
                }
            }
        }
    }

    private void OnJoinButtonClicked()
    {
        string nickname = nicknameInput.text.Trim();

        // 유효성 검사
        if (string.IsNullOrEmpty(nickname))
        {
            ShowError("Please enter a nickname!");
            return;
        }

        if (nickname.Length < 2 || nickname.Length > 16)
        {
            ShowError("Nickname must be 2-16 characters!");
            return;
        }

        // 닉네임과 색상 저장
        PlayerPrefs.SetString("PlayerNickname", nickname);
        PlayerPrefs.SetInt("PlayerColorIndex", selectedColorIndex);

        // RGB 값도 저장 (서버 전송용)
        Color selectedColor = availableColors[selectedColorIndex];
        PlayerPrefs.SetFloat("PlayerColorR", selectedColor.r);
        PlayerPrefs.SetFloat("PlayerColorG", selectedColor.g);
        PlayerPrefs.SetFloat("PlayerColorB", selectedColor.b);

        PlayerPrefs.Save();

        Debug.Log($"Saved: {nickname}, Color Index: {selectedColorIndex}");

        // 게임 씬으로 이동
        SceneManager.LoadScene("GameScene");
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            Invoke(nameof(HideError), 3f);
        }
    }

    private void HideError()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }
}
