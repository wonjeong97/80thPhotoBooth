using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI 생성과 게임 시작, 팝업 처리, 인벤토리 관리 등을 담당하는 클래스
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Canvas")]
    [SerializeField] private Transform titleCanvas;     // 타이틀 캔버스
    [SerializeField] private Transform gameCanvas;      // 게임 캔버스

    [Header("UI 프리팹")]
    [SerializeField] private GameObject buttonPrefab;   // 동적 생성 버튼 프리팹
    [SerializeField] private GameObject textPrefab;     // 동적 생성 텍스트 프리팹
    [SerializeField] private GameObject imagePrefab;    // 동적 생성 이미지 프리팹

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource; // UI 사운드 소스

    private int itemFoundCount = 0;                     // 게임 버튼을 누른 수

    private float inactivityTimer;
    private float inactivityThreshold = 60f; // 입력이 없는 경우 타이틀로 되돌아가는 시간

    private GameObject gameBackground;       // 게임 캔버스 백그라운드 이미지
    private GameObject inventory;            // 인벤토리 오브젝트

    private Dictionary<string, Image> itemIcons = new Dictionary<string, Image>();
    private Dictionary<string, AudioClip> soundMap = new Dictionary<string, AudioClip>();
    private Dictionary<string, float> soundVolumeMap = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        StartCoroutine(LoadSoundsFromSettings()); // 사운드 로딩
    }

    private void Start()
    {
        CreateTitle();  // 타이틀 이미지 생성

        if (JsonLoader.Instance.Settings != null)
        {
            // JSON 세팅에서 미입력시간을 받아옴
            inactivityThreshold = JsonLoader.Instance.Settings.inactivityTime;
        }
    }

    private void Update()
    {
        // 게임 캔버스가 활성화 된 경우,
        // 입력이 일정시간 이상 없을 시 씬을 재로드
        if (gameCanvas != null && gameCanvas.gameObject.activeInHierarchy)
        {
            inactivityTimer += Time.deltaTime;

            // 임계값 초과 시 씬 재로드
            if (inactivityTimer >= inactivityThreshold)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            if (Input.anyKeyDown || Input.touchCount > 0 || Input.GetMouseButtonDown(0))
            {
                inactivityTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 타이틀 화면 구성
    /// </summary>
    private void CreateTitle()
    {
        TitleSetting setting = JsonLoader.Instance.Settings.titleSetting;
        GameObject bg = CreateBackgroundImage(setting.backgroundImage, titleCanvas);

        CreateTexts(setting.texts, bg);
        CreateButton(setting.startButton, bg, () =>
        {
            // 스타트 버튼 클릭 시 동작 설정

            if (titleCanvas != null && titleCanvas.gameObject.activeInHierarchy)
            {
                titleCanvas.gameObject.SetActive(false);
                gameCanvas.gameObject.SetActive(true);
                CreateGameUI();
            }
        });
    }

    /// <summary>
    /// 게임화면 UI 구성
    /// </summary>
    private void CreateGameUI()
    {
        Game1Setting setting = JsonLoader.Instance.Settings.game1Setting;
        gameBackground = CreateBackgroundImage(setting.game1BackgroundImage, gameCanvas);

        CreateStartPopup();
    }

    #region UI Creator
    /// <summary>
    /// 배경 이미지 생성
    /// </summary>
    public GameObject CreateBackgroundImage(ImageSetting backgroundSetting, Transform parentCanvas)
    {
        GameObject bg = Instantiate(imagePrefab, parentCanvas);
        bg.name = backgroundSetting.name;
        bg.transform.SetAsFirstSibling(); // 맨 뒤로 보내기

        if (bg.TryGetComponent<Image>(out Image image))
        {
            Texture2D texture = LoadTexture(backgroundSetting.imagePath);
            if (texture)
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            image.color = backgroundSetting.imageColor;
        }

        if (bg.TryGetComponent<RectTransform>(out RectTransform rt))
        {
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = backgroundSetting.size;
            rt.anchoredPosition = new Vector2(backgroundSetting.position.x, -backgroundSetting.position.y);
        }

        return bg;
    }

    /// <summary>
    /// 텍스트 생성
    /// </summary>
    private void CreateTexts(TextSetting[] settings, GameObject parent)
    {
        foreach (var setting in settings)
        {
            string mappedFontName = ResolveFont(setting.fontResourceName);

            GameObject go = Instantiate(textPrefab, parent.transform);
            go.name = setting.name;

            if (go.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI uiText))
            {
                uiText.text = setting.text;
                uiText.font = Resources.Load<TMP_FontAsset>($"Font/{mappedFontName}");
                uiText.fontSize = setting.fontSize;
                uiText.color = setting.fontColor;
                uiText.alignment = TextAlignmentOptions.Center; // 중앙 정렬
            }

            if (go.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
                rt.localRotation = Quaternion.Euler(0, 0, setting.rotationZ);
            }
        }
    }

    /// <summary>
    /// 이미지 생성
    /// </summary>
    private void CreateImages(ImageSetting[] settings, GameObject parent)
    {
        if (settings == null || settings.Length == 0) return;

        foreach (var setting in settings)
        {
            GameObject go = Instantiate(imagePrefab, parent.transform);
            go.name = setting.name;

            if (go.TryGetComponent<Image>(out Image image))
            {
                Texture2D texture = LoadTexture(setting.imagePath);
                if (texture)
                {
                    image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                }
                image.color = setting.imageColor;
                image.type = (Image.Type)setting.imageType;
            }

            if (go.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.sizeDelta = setting.size;
                rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
            }
        }
    }

    /// <summary>
    /// 버튼 생성 및 클릭 이벤트 연결
    /// </summary>
    private GameObject CreateButton(ButtonSetting setting, GameObject parent, UnityAction onClickAction)
    {
        GameObject go = Instantiate(buttonPrefab, parent.transform);
        go.name = setting.name;

        if (go.TryGetComponent<RectTransform>(out RectTransform rt))
        {
            rt.sizeDelta = setting.buttonSize;
            rt.anchoredPosition = new Vector2(setting.buttonPosition.x, -setting.buttonPosition.y);
        }

        // 배경 이미지 설정
        if (go.TryGetComponent<Image>(out Image image) && setting.buttonImage != null)
        {
            Texture2D texture = LoadTexture(setting.buttonImage.imagePath);
            if (texture)
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            image.color = setting.buttonImage.imageColor;

            // 이미지 타입 설정
            image.type = (Image.Type)setting.buttonImage.imageType;
        }

        // 텍스트 설정 (자식에 TextMeshProUGUI 컴포넌트가 있다고 가정)
        TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null && setting.buttonText != null &&
            !string.IsNullOrEmpty(setting.buttonText.text) &&
            !string.IsNullOrEmpty(setting.buttonText.fontResourceName))
        {
            string mappedFontName = ResolveFont(setting.buttonText.fontResourceName);

            text.text = setting.buttonText.text;
            text.font = Resources.Load<TMP_FontAsset>($"Font/{mappedFontName}");
            text.fontSize = setting.buttonText.fontSize;
            text.color = setting.buttonText.fontColor;

            if (text.TryGetComponent<RectTransform>(out RectTransform textRT))
            {
                textRT.localRotation = Quaternion.Euler(0, 0, setting.buttonText.rotationZ);
            }
        }

        // 버튼 클릭 이벤트
        if (go.TryGetComponent<Button>(out Button button) && onClickAction != null)
        {
            string soundKey = setting.buttonSound;

            button.onClick.AddListener(() =>
            {
                PlayClickSound(soundKey);
                onClickAction?.Invoke();
            });
        }

        return go;
    }

    /// <summary>
    /// 게임 시작 안내 팝업을 생성합니다.
    /// 출발 버튼 클릭 시 인벤토리와 사진 버튼들을 생성합니다.
    /// </summary>
    private void CreateStartPopup()
    {
        // 게임 시작 팝업 설정 불러오기
        PopupSetting setting = JsonLoader.Instance.Settings.game1Setting.popupSetting;

        // 팝업 생성 (닫기 이벤트 포함)
        CreatePopup(setting, gameBackground, () =>
        {
            // 팝업 닫힌 후 다음 단계 실행
            StartCoroutine(AfterCloseStartPopup(gameBackground));
        });
    }

    /// <summary>
    /// 스타트 팝업 창이 닫힌 후 인벤토리, 게임 버튼 생성 및 이벤트 연결
    /// </summary>
    /// <param name="parent">인벤토리 및 게임 버튼이 붙는 게임 캔버스의 배경 이미지</param>
    /// <returns></returns>
    private IEnumerator AfterCloseStartPopup(GameObject parent)
    {
        // 1 프레임 기다렸다가 실행 (SetActive 이후 안정적으로 처리)
        yield return null;

        Game1Setting game1Setting = JsonLoader.Instance.Settings.game1Setting;
        CreateInventory(game1Setting.inventorySetting, parent); // 인벤토리 생성

        // 포토 버튼 생성 및 이벤트 연결
        for (int i = 0; i < game1Setting.photoButtons.Length; i++)
        {
            GameObject gameButton = CreateButton(game1Setting.photoButtons[i], parent, null);
            string soundKey = game1Setting.photoButtons[i].buttonSound;

            if (gameButton.TryGetComponent<Button>(out Button btn))
            {
                int index = i;

                btn.onClick.AddListener(() =>
                {
                    if (itemIcons.TryGetValue($"Item_{index}", out Image itemImage))
                    {
                        PlayClickSound(soundKey);
                        itemImage.material = null;  // 연결된 아이템 이미지의 색 복원
                        itemFoundCount++;

                        btn.gameObject.SetActive(false);
                        CreateExplainPopup(index); // 설명 팝업 생성
                    }
                });
            }
        }
    }

    /// <summary>
    /// 설명 팝업을 생성합니다. 인덱스에 따라 해당 설명 내용을 로드합니다.
    /// 닫기 버튼 클릭 시 모든 아이템을 찾았다면 엔딩 팝업으로 넘어갑니다.
    /// </summary>
    /// <param name="index">해당 포토 버튼 인덱스</param>
    private void CreateExplainPopup(int index)
    {
        var explainSettings = JsonLoader.Instance.Settings.explainPopupSetting;
        if (index < 0 || index >= explainSettings.Length)
        {
            Debug.LogWarning($"잘못된 인덱스: {index}");
            return;
        }

        CreatePopup(explainSettings[index], gameBackground, () =>
        {
            // 설명 팝업 닫기 시 아이템 수 체크
            if (itemFoundCount == JsonLoader.Instance.Settings.game1Setting.photoButtons.Length)
            {
                CreateGameEndPopup();
            }
        });
    }

    /// <summary>
    /// 게임 종료 팝업을 생성하고 인벤토리 위치를 중앙으로 이동시킵니다.
    /// </summary>
    private void CreateGameEndPopup()
    {
        PopupSetting setting = JsonLoader.Instance.Settings.gameEndPopupSetting;
        string soundKey = setting.popupButton.buttonSound;

        CreatePopup(setting, gameBackground, () =>
        {
            PlayClickSound(soundKey);
            inventory.SetActive(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        if (inventory != null)
        {
            RectTransform invRT = inventory.GetComponent<RectTransform>();
            invRT.pivot = new Vector2(0.5f, 0.5f);
            invRT.anchoredPosition = new(JsonLoader.Instance.Settings.gameEndInventoryPosition.x, -JsonLoader.Instance.Settings.gameEndInventoryPosition.y);

            // 팝업보다 인벤토리를 위로 이동
            inventory.transform.SetAsLastSibling();
        }
    }

    private void CreatePopup(PopupSetting setting, GameObject parent, UnityAction onClose = null)
    {
        GameObject popupBG = CreateBackgroundImage(setting.popupBackgroundImage, parent.transform);
        popupBG.name = setting.name;

        CreateTexts(setting.popupTexts, popupBG);
        CreateImages(setting.popupImages, popupBG);

        CreateButton(setting.popupButton, popupBG, () =>
        {
            popupBG.SetActive(false);
            onClose?.Invoke(); // 닫기 후 실행
        });

        // 팝업을 UI 최상단으로 이동
        popupBG.transform.SetAsLastSibling();
    }

    private void CreateInventory(InventorySetting setting, GameObject parent)
    {
        // 배경 이미지 생성
        GameObject bg = Instantiate(imagePrefab, parent.transform);
        bg.name = setting.inventoryBackgroundImage.name;
        inventory = bg;

        if (bg.TryGetComponent<Image>(out Image image))
        {
            Texture2D texture = LoadTexture(setting.inventoryBackgroundImage.imagePath);
            if (texture)
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            image.color = setting.inventoryBackgroundImage.imageColor;
            image.type = (Image.Type)setting.inventoryBackgroundImage.imageType;
        }

        if (bg.TryGetComponent<RectTransform>(out RectTransform bgRT))
        {
            bgRT.sizeDelta = setting.inventoryBackgroundImage.size;
            bgRT.anchoredPosition = new(setting.inventoryBackgroundImage.position.x, -setting.inventoryBackgroundImage.position.y);
        }

        CreateInventoryItems(setting.itemImages, bgRT, setting.columns, setting.rows, setting.itemPadding);
    }

    private void CreateInventoryItems(ImageSetting[] settings, RectTransform parentRT, int columns, int rows, float padding)
    {
        itemIcons.Clear(); // 새로 생성 시 초기화

        Vector2 cellSize = new Vector2(
            (parentRT.sizeDelta.x - padding * (columns + 1)) / columns,
            (parentRT.sizeDelta.y - padding * (rows + 1)) / rows
        );

        // Column, Row에 맞춰 아이템 이미지 정렬
        for (int i = 0; i < settings.Length && i < columns * rows; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float x = -parentRT.sizeDelta.x / 2 + padding + col * (cellSize.x + padding) + cellSize.x / 2;
            float y = parentRT.sizeDelta.y / 2 - padding - row * (cellSize.y + padding) - cellSize.y / 2;

            Vector2 anchoredPos = new Vector2(x, y);

            GameObject itemGO = Instantiate(imagePrefab, parentRT);
            itemGO.name = $"Item_{i}";

            if (itemGO.TryGetComponent<Image>(out Image img))
            {
                Texture2D tex = LoadTexture(settings[i].imagePath);
                if (tex)
                {
                    img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                }
                img.color = settings[i].imageColor;
                img.type = (Image.Type)settings[i].imageType;

                Material grayscaleMat = Resources.Load<Material>("Materials/Grayscale"); // Resources/Materials/Grayscale.mat
                img.material = grayscaleMat;
            }

            if (itemGO.TryGetComponent<RectTransform>(out RectTransform itemRT))
            {
                itemRT.sizeDelta = cellSize;
                itemRT.anchorMin = itemRT.anchorMax = new Vector2(0.5f, 0.5f);
                itemRT.pivot = new Vector2(0.5f, 0.5f);
                itemRT.anchoredPosition = anchoredPos;
            }

            itemIcons[$"Item_{i}"] = img;
        }
    }
    #endregion

    #region Utils
    /// <summary>
    /// Path로부터 이미지를 읽고 텍스쳐로 변환
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    private Texture2D LoadTexture(string relativePath)
    {
        string path = Path.Combine(Application.streamingAssetsPath, relativePath);
        if (!File.Exists(path)) return null;

        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    /// <summary>
    /// JSON의 font 키값(font1, font2 등)을 실제 폰트 파일 이름으로 매핑합니다.
    /// FontMapping 클래스의 필드명을 리플렉션으로 찾아 반환합니다.
    /// </summary>
    /// <param name="key">JSON 내에 명시된 font 키 (예: "font1")</param>
    /// <returns>실제 폰트 리소스 파일 이름 (예: "NanumGothic-Regular SDF")</returns>
    private string ResolveFont(string key)
    {
        FontMapping fontMap = JsonLoader.Instance.Settings.fontMap;
        if (fontMap == null) return key;

        // 리플렉션을 통해 key에 해당하는 필드 정보를 얻음
        var field = typeof(FontMapping).GetField(key);
        if (field != null)
        {
            return field.GetValue(fontMap) as string ?? key; // 실제 폰트 이름 반환
        }

        return key; // 매핑 실패 시 원래 key 반환
    }

    /// <summary>
    /// JSON에 정의된 사운드 목록을 StreamingAssets/Audio 경로에서 로드하여 soundMap에 저장합니다.
    /// 각 사운드는 key-clip 쌍으로 저장되며, 재생 시 key로 참조합니다.
    /// </summary>
    private IEnumerator LoadSoundsFromSettings()
    {
        soundMap.Clear();

        SoundSetting[] soundEntries = JsonLoader.Instance.Settings.sounds;
        if (soundEntries == null) yield break;

        foreach (SoundSetting entry in soundEntries)
        {
            // 전체 파일 경로 구성
            string fullPath = Path.Combine(Application.streamingAssetsPath, "Audio", entry.clipPath).Replace("\\", "/");

            // 파일에서 오디오 클립 로드 (WAV 파일 기준)
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // 성공적으로 로드되면 Dictionary에 저장
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                soundMap[entry.key] = clip; // 오디오 클립 저장
                soundVolumeMap[entry.key] = entry.volume;   // 볼륨 저장
            }
            else
            {
                Debug.LogWarning($"[SoundLoader] 실패: {entry.clipPath} - {www.error}");
            }
        }
    }

    /// <summary>
    /// 지정된 사운드 키에 해당하는 AudioClip을 찾아 재생합니다.
    /// 사운드는 UI 버튼 클릭 시 사용됩니다.
    /// </summary>
    /// <param name="key">JSON에 등록된 사운드 키 (예: "click1")</param>
    private void PlayClickSound(string key)
    {
        // AudioSource가 있고, 해당 key에 대한 clip이 있으면 재생
        if (uiAudioSource != null && soundMap.TryGetValue(key, out AudioClip clip))
        {
            // 키에 해당하는 볼륨 없으면 기본값 1.0 사용
            float volume = soundVolumeMap.TryGetValue(key, out float v) ? v : 1.0f;
            uiAudioSource.PlayOneShot(clip);
        }
    }
    #endregion
}