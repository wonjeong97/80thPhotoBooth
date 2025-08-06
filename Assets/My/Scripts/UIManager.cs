using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI ������ ���� ����, �˾� ó��, �κ��丮 ���� ���� ����ϴ� Ŭ����
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Canvas")]
    [SerializeField] private Transform titleCanvas;
    [SerializeField] private Transform gameCanvas;

    [Header("UI ������")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private GameObject imagePrefab;

    private int itemFoundCount = 0;

    private float inactivityTimer;
    private float inactivityThreshold = 60f; // �Է��� ���� ��� Ÿ��Ʋ�� �ǵ��ư��� �ð�

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
        // ���� ĵ������ Ȱ��ȭ �� ���,
        // �Է��� �����ð� �̻� ���� �� ���� ��ε�
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
    /// Ÿ��Ʋ ȭ�� ����
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
    /// ����ȭ�� UI ����
    /// </summary>
    private void CreateGameUI()
    {
        Game1Setting setting = JsonLoader.Instance.Settings.game1Setting;
        gameBackground = CreateBackgroundImage(setting.game1BackgroundImage, gameCanvas);

        CreateStartPopup();
    }

    #region UI Creator
    /// <summary>
    /// ��� �̹��� ����
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

        bg.transform.SetAsFirstSibling(); // �� �ڷ� ������
        return bg;
    }

    /// <summary>
    /// Path�κ��� �̹����� �а� �ؽ��ķ� ��ȯ
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
    /// �ؽ�Ʈ ����
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
            uiText.alignment = TextAlignmentOptions.Center; // �߾� ����

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
    /// �̹��� ����
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
    /// ��ư ���� �� Ŭ�� �̺�Ʈ ����
    /// </summary>
    private GameObject CreateButton(ButtonSetting setting, GameObject parent, UnityAction onClickAction)
    {
        GameObject go = Instantiate(buttonPrefab, parent.transform);
        go.name = setting.name;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = setting.buttonSize;
        rt.anchoredPosition = new Vector2(setting.buttonPosition.x, -setting.buttonPosition.y);

        // ��� �̹��� ����
        Image image = go.GetComponent<Image>();
        if (setting.buttonImage != null)
        {
            Texture2D texture = LoadTexture(setting.buttonImage.imagePath);
            if (texture)
            {
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
            image.color = setting.buttonImage.imageColor;

            // �̹��� Ÿ�� ����
            image.type = (Image.Type)setting.buttonImage.imageType;
        }

        // �ؽ�Ʈ ���� (�ڽĿ� TextMeshProUGUI ������Ʈ�� �ִٰ� ����)
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

        // ��ư Ŭ�� �̺�Ʈ (���Ѵٸ� ���� ����)
        Button button = go.GetComponent<Button>();
        if (button != null && onClickAction != null)
        {
            button.onClick.AddListener(onClickAction);
        }

        return go;
    }

    /// <summary>
    /// ���� ���� �ȳ� �˾��� �����մϴ�.
    /// ��� ��ư Ŭ�� �� �κ��丮�� ���� ��ư���� �����մϴ�.
    /// </summary>
    private void CreateStartPopup()
    {
        // ���� ���� �˾� ���� �ҷ�����
        PopupSetting setting = JsonLoader.Instance.Settings.game1Setting.popupSetting;

        // �˾� ���� (�ݱ� �̺�Ʈ ����)
        CreatePopup(setting, gameBackground, () =>
        {
            // �˾� ���� �� ���� �ܰ� ����
            StartCoroutine(DeferredPopupCloseHandler(gameBackground));
        });
    }

    /// <summary>
    /// ���� �˾��� �����մϴ�. �ε����� ���� �ش� ���� ������ �ε��մϴ�.
    /// �ݱ� ��ư Ŭ�� �� ��� �������� ã�Ҵٸ� ���� �˾����� �Ѿ�ϴ�.
    /// </summary>
    /// <param name="index">�ش� ���� ��ư �ε���</param>
    private void CreateExplainPopup(int index)
    {
        var explainSettings = JsonLoader.Instance.Settings.explainPopupSetting;
        if (index < 0 || index >= explainSettings.Length)
        {
            Debug.LogWarning($"�߸��� �ε���: {index}");
            return;
        }

        CreatePopup(explainSettings[index], gameBackground, () =>
        {
            // ���� �˾� �ݱ� �� ������ �� üũ
            if (itemFoundCount == JsonLoader.Instance.Settings.game1Setting.photoButtons.Length)
            {
                CreateGameEndPopup();
            }
        });
    }

    /// <summary>
    /// ���� ���� �˾��� �����ϰ� �κ��丮 ��ġ�� �߾����� �̵���ŵ�ϴ�.
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

            // �˾����� �κ��丮�� ���� �̵�
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
            onClose?.Invoke(); // �ݱ� �� ����
        });

        // �˾��� UI �ֻ������ �̵�
        popupBG.transform.SetAsLastSibling();
    }

    private IEnumerator DeferredPopupCloseHandler(GameObject parent)
    {
        // 1 ������ ��ٷȴٰ� ���� (SetActive ���� ���������� ó��)
        yield return null;

        Game1Setting game1Setting = JsonLoader.Instance.Settings.game1Setting;

        // �κ��丮 ����
        CreateInventory(game1Setting.inventorySetting, parent);

        // ���� ��ư ���� �� �̺�Ʈ ����
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

                        // ���� �˾� ����
                        CreateExplainPopup(index);
                    }
                });
            }
        }
    }

    private void CreateInventory(InventorySetting setting, GameObject parent)
    {
        // ��� �̹��� ����
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
        itemIcons.Clear(); // ���� ���� �� �ʱ�ȭ

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
