using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CreateTitle();
    }
    
    /// <summary>
    /// Title 캔버스 생성
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
                CreateGameUI();
            }
        });
    }

    private void CreateGameUI()
    {
        Game1Setting setting = JsonLoader.Instance.Settings.game1Setting;        
        GameObject bg = CreateBackgroundImage(setting.game1BackgroundImage, gameCanvas);

        CreatePopup(setting.popupSetting, bg);
    }

    #region UI Creator
    /// <summary>
    /// 타이틀 UI의 배경화면 생성
    /// </summary>
    /// <param name="backgroundSetting"></param>
    public GameObject CreateBackgroundImage(ImageSetting backgroundSetting, Transform parentCanvas)
    {
        GameObject bg = Instantiate(imagePrefab, parentCanvas);
        bg.name = "Image_Background";

        Image image = bg.GetComponent<Image>();
        Texture2D texture = LoadTexture(backgroundSetting.imagePath);
        if (texture)
        {
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
        image.color = backgroundSetting.imageColor;

        RectTransform rt = bg.GetComponent<RectTransform>();        
        rt.anchorMin = rt.anchorMax = backgroundSetting.position;
        rt.sizeDelta = backgroundSetting.size;
        rt.anchoredPosition = Vector2.zero;

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
    /// <param name="texts"></param>
    /// <param name="parent"></param>
    private void CreateTexts(TextSetting[] texts, GameObject parent)
    {
        foreach (var setting in texts)
        {
            GameObject go = Instantiate(textPrefab, parent.transform);
            TextMeshProUGUI uiText = go.GetComponent<TextMeshProUGUI>();

            uiText.text = setting.text;
            uiText.font = Resources.Load<TMP_FontAsset>($"Font/{setting.fontResourceName}");
            uiText.fontSize = setting.fontSize;
            uiText.color = setting.fontColor;
            uiText.alignment = TextAlignmentOptions.Center; // 중앙 정렬 (선택사항)

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = setting.position;
            rt.anchoredPosition = Vector2.zero; // ← 위치가 겹치는 것 방지
            rt.localRotation = Quaternion.Euler(0, 0, setting.rotationZ);
        }
    }
    /// <summary>
    /// 이미지 생성
    /// </summary>
    /// <param name="images"></param>
    /// <param name="parent"></param>
    private void CreateImages(ImageSetting[] images, GameObject parent)
    {
        if (images == null || images.Length == 0) return;

        foreach (var setting in images)
        {
            GameObject go = Instantiate(imagePrefab, parent.transform);
            go.name = $"Image_{Path.GetFileNameWithoutExtension(setting.imagePath)}";

            Image image = go.GetComponent<Image>();
            Texture2D texture = LoadTexture(setting.imagePath);
            if (texture)
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            image.color = setting.imageColor;
            image.type = (Image.Type)setting.imageType;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = setting.position;
            rt.sizeDelta = setting.size;
            rt.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 버튼 생성
    /// </summary>
    /// <param name="title"></param>
    /// <param name="parent"></param>
    private void CreateButton(ButtonSetting setting, GameObject parent, UnityAction onClickAction)
    {
        GameObject go = Instantiate(buttonPrefab, parent.transform);
        go.name = "Button";

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = setting.buttonPosition;
        rt.sizeDelta = setting.buttonSize;
        rt.anchoredPosition = Vector2.zero;

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
        if (text != null && setting.buttonText != null)
        {
            text.text = setting.buttonText.text;
            text.font = Resources.Load<TMP_FontAsset>($"Font/{setting.buttonText.fontResourceName}");
            text.fontSize = setting.buttonText.fontSize;
            text.color = setting.buttonText.fontColor;

            RectTransform textRT = text.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            textRT.localRotation = Quaternion.Euler(0, 0, setting.buttonText.rotationZ);
        }

        // 버튼 클릭 이벤트 (원한다면 연결 가능)
        Button button = go.GetComponent<Button>();
        if (button != null && onClickAction != null)
        {
            button.onClick.AddListener(onClickAction);
        }
    }

    private void CreatePopup(PopupSetting setting, GameObject parent)
    {
        GameObject popupBG = CreateBackgroundImage(setting.popupBackgroundImage, parent.transform);

        CreateTexts(setting.popupTexts, popupBG);
        CreateImages(setting.popupImages, popupBG);
        CreateButton(setting.popupButton, popupBG, () => 
        { 
            if (popupBG != null && popupBG.gameObject.activeInHierarchy)
            {
                popupBG.gameObject.SetActive(false);
                CreateInventory(JsonLoader.Instance.Settings.game1Setting.inventorySetting, parent);
            }
        });
    }

    private void CreateInventory(InventorySetting setting, GameObject parent)
    {
        GameObject inventoryBG = CreateBackgroundImage(setting.inventoryBackgroundImage, parent.transform);


    }
    #endregion
}
