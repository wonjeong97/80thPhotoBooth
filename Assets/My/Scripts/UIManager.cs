using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI 생성과 게임 시작, 팝업 처리, 인벤토리 관리 등을 담당하는 클래스
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Canvas")]
    [SerializeField] private Transform titleCanvas;
    [SerializeField] private Transform gameCanvas;

    [Header("UI 프리팹")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private GameObject imagePrefab;

    private int itemFoundCount = 0;

    private float inactivityTimer;
    private float inactivityThreshold = 60f; // 입력이 없는 경우 타이틀로 되돌아가는 시간

    private GameObject gameBackground;
    private GameObject inventory;

    private Dictionary<string, Image> itemIcons = new Dictionary<string, Image>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CreateTitle();

        if (JsonLoader.Instance.Settings != null)
        {
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

        Image image = bg.GetComponent<Image>();
        Texture2D texture = LoadTexture(backgroundSetting.imagePath);
        if (texture)
        {
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
        image.color = backgroundSetting.imageColor;

        RectTransform rt = bg.GetComponent<RectTransform>();        
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = backgroundSetting.size;
        rt.anchoredPosition = new Vector2(backgroundSetting.position.x, -backgroundSetting.position.y);

        bg.transform.SetAsFirstSibling(); // 맨 뒤로 보내기
        return bg;
    }

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
    /// 텍스트 생성
    /// </summary>
    private void CreateTexts(TextSetting[] settings, GameObject parent)
    {
        foreach (var setting in settings)
        {
            string mappedFontName = ResolveFont(setting.fontResourceName);

            GameObject go = Instantiate(textPrefab, parent.transform);
            go.name = setting.name;

            TextMeshProUGUI uiText = go.GetComponent<TextMeshProUGUI>();
            uiText.text = setting.text;
            uiText.font = Resources.Load<TMP_FontAsset>($"Font/{mappedFontName}");
            uiText.fontSize = setting.fontSize;
            uiText.color = setting.fontColor;
            uiText.alignment = TextAlignmentOptions.Center; // 중앙 정렬

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
            rt.localRotation = Quaternion.Euler(0, 0, setting.rotationZ);
        }
    }

    private string ResolveFont(string key)
    {
        var fontMap = JsonLoader.Instance.Settings.fontMap;
        if (fontMap == null) return key;

        var field = typeof(FontMapping).GetField(key);
        if (field != null)
        {
            return field.GetValue(fontMap) as string ?? key;
        }

        return key;
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

            Image image = go.GetComponent<Image>();
            Texture2D texture = LoadTexture(setting.imagePath);
            if (texture)
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            image.color = setting.imageColor;
            image.type = (Image.Type)setting.imageType;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = setting.size;
            rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
        }
    }

    /// <summary>
    /// 버튼 생성 및 클릭 이벤트 연결
    /// </summary>
    private GameObject CreateButton(ButtonSetting setting, GameObject parent, UnityAction onClickAction)
    {
        GameObject go = Instantiate(buttonPrefab, parent.transform);
        go.name = setting.name;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = setting.buttonSize;
        rt.anchoredPosition = new Vector2(setting.buttonPosition.x, -setting.buttonPosition.y);

        // 배경 이미지 설정
        Image image = go.GetComponent<Image>();
        if (setting.buttonImage != null)
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

            RectTransform textRT = text.GetComponent<RectTransform>();
            textRT.localRotation = Quaternion.Euler(0, 0, setting.buttonText.rotationZ);
        }

        // 버튼 클릭 이벤트 (원한다면 연결 가능)
        Button button = go.GetComponent<Button>();
        if (button != null && onClickAction != null)
        {
            button.onClick.AddListener(onClickAction);
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
            StartCoroutine(DeferredPopupCloseHandler(gameBackground));
        });
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
        CreatePopup(JsonLoader.Instance.Settings.gameEndPopupSetting, gameBackground, () =>
        {
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

    private IEnumerator DeferredPopupCloseHandler(GameObject parent)
    {
        // 1 프레임 기다렸다가 실행 (SetActive 이후 안정적으로 처리)
        yield return null;

        Game1Setting game1Setting = JsonLoader.Instance.Settings.game1Setting;

        // 인벤토리 생성
        CreateInventory(game1Setting.inventorySetting, parent);

        // 포토 버튼 생성 및 이벤트 연결
        for (int i = 0; i < game1Setting.photoButtons.Length; i++)
        {
            GameObject gameButton = CreateButton(game1Setting.photoButtons[i], parent, null);
            Button btn = gameButton.GetComponent<Button>();

            if (btn != null)
            {
                int index = i;

                btn.onClick.AddListener(() =>
                {
                    if (itemIcons.TryGetValue($"Item_{index}", out Image itemImage))
                    {
                        itemImage.material = null;
                        itemFoundCount++;

                        btn.gameObject.SetActive(false);

                        // 설명 팝업 생성
                        CreateExplainPopup(index);
                    }
                });
            }
        }
    }

    private void CreateInventory(InventorySetting setting, GameObject parent)
    {
        // 배경 이미지 생성
        GameObject bg = Instantiate(imagePrefab, parent.transform);
        bg.name = setting.inventoryBackgroundImage.name;
        inventory = bg;

        Image image = bg.GetComponent<Image>();
        Texture2D texture = LoadTexture(setting.inventoryBackgroundImage.imagePath);
        if (texture)
        {
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
        image.color = setting.inventoryBackgroundImage.imageColor;
        image.type = (Image.Type)setting.inventoryBackgroundImage.imageType;

        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.sizeDelta = setting.inventoryBackgroundImage.size;
        bgRT.anchoredPosition = new(setting.inventoryBackgroundImage.position.x, -setting.inventoryBackgroundImage.position.y);

        CreateInventoryItems(setting.itemImages, bgRT, setting.columns, setting.rows, setting.itemPadding);
    }

    private void CreateInventoryItems(ImageSetting[] settings, RectTransform parentRT, int columns, int rows, float padding)
    {
        itemIcons.Clear(); // 새로 생성 시 초기화

        Vector2 cellSize = new Vector2(
            (parentRT.sizeDelta.x - padding * (columns + 1)) / columns,
            (parentRT.sizeDelta.y - padding * (rows + 1)) / rows
        );

        for (int i = 0; i < settings.Length && i < columns * rows; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float x = -parentRT.sizeDelta.x / 2 + padding + col * (cellSize.x + padding) + cellSize.x / 2;
            float y = parentRT.sizeDelta.y / 2 - padding - row * (cellSize.y + padding) - cellSize.y / 2;

            Vector2 anchoredPos = new Vector2(x, y);

            GameObject itemGO = Instantiate(imagePrefab, parentRT);
            itemGO.name = $"Item_{i}";

            Image img = itemGO.GetComponent<Image>();
            Texture2D tex = LoadTexture(settings[i].imagePath);
            if (tex)
            {
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            }
            img.color = settings[i].imageColor;
            img.type = (Image.Type)settings[i].imageType;

            Material grayscaleMat = Resources.Load<Material>("Materials/Grayscale"); // Resources/Materials/Grayscale.mat
            img.material = grayscaleMat;

            RectTransform itemRT = itemGO.GetComponent<RectTransform>();
            itemRT.sizeDelta = cellSize;
            itemRT.anchorMin = itemRT.anchorMax = new Vector2(0.5f, 0.5f);
            itemRT.pivot = new Vector2(0.5f, 0.5f);
            itemRT.anchoredPosition = anchoredPos;

            itemIcons[$"Item_{i}"] = img;
        }
    }
    #endregion
}
