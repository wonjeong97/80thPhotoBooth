using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private bool isGameCleared = false;

    private GameObject gameBackground;
    private GameObject inventory;

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
    /// Title ĵ���� ����
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
        gameBackground = CreateBackgroundImage(setting.game1BackgroundImage, gameCanvas);

        CreatePopup(setting.popupSetting, gameBackground);
    }

    #region UI Creator
    /// <summary>
    /// Ÿ��Ʋ UI�� ���ȭ�� ����
    /// </summary>
    /// <param name="backgroundSetting"></param>
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
        rt.anchorMin = rt.anchorMax = backgroundSetting.position;
        rt.sizeDelta = backgroundSetting.size;
        rt.anchoredPosition = Vector2.zero;

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
    /// <param name="settings"></param>
    /// <param name="parent"></param>
    private void CreateTexts(TextSetting[] settings, GameObject parent)
    {
        foreach (var setting in settings)
        {
            GameObject go = Instantiate(textPrefab, parent.transform);
            go.name = setting.name;
            TextMeshProUGUI uiText = go.GetComponent<TextMeshProUGUI>();

            uiText.text = setting.text;
            uiText.font = Resources.Load<TMP_FontAsset>($"Font/{setting.fontResourceName}");
            uiText.fontSize = setting.fontSize;
            uiText.color = setting.fontColor;
            uiText.alignment = TextAlignmentOptions.Center; // �߾� ���� (���û���)

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = setting.position;
            rt.anchoredPosition = Vector2.zero; // �� ��ġ�� ��ġ�� �� ����
            rt.localRotation = Quaternion.Euler(0, 0, setting.rotationZ);
        }
    }
    /// <summary>
    /// �̹��� ����
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="parent"></param>
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
            rt.anchorMin = rt.anchorMax = setting.position;
            rt.sizeDelta = setting.size;
            rt.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// ��ư ����
    /// </summary>
    /// <param name="title"></param>
    /// <param name="parent"></param>
    private GameObject CreateButton(ButtonSetting setting, GameObject parent, UnityAction onClickAction)
    {
        GameObject go = Instantiate(buttonPrefab, parent.transform);
        go.name = setting.name;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = setting.buttonPosition;
        rt.sizeDelta = setting.buttonSize;
        rt.anchoredPosition = Vector2.zero;

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

        // ��ư Ŭ�� �̺�Ʈ (���Ѵٸ� ���� ����)
        Button button = go.GetComponent<Button>();
        if (button != null && onClickAction != null)
        {
            button.onClick.AddListener(onClickAction);
        }

        return go;
    }

    private void CreatePopup(PopupSetting setting, GameObject parent)
    {
        GameObject popupBG = CreateBackgroundImage(setting.popupBackgroundImage, parent.transform);
        popupBG.name = setting.name;

        CreateTexts(setting.popupTexts, popupBG);
        CreateImages(setting.popupImages, popupBG);
        CreateButton(setting.popupButton, popupBG, () =>
        {
            if (popupBG != null && popupBG.gameObject.activeInHierarchy)
            {
                popupBG.gameObject.SetActive(false);
                // �񵿱�� ���� �����ӿ� ����ǵ��� �ڷ�ƾ ��� (�����ϰ�)
                StartCoroutine(DeferredPopupCloseHandler(parent));
            }
        });
    }

    private IEnumerator DeferredPopupCloseHandler(GameObject parent)
    {
        // 1 ������ ��ٷȴٰ� ���� (SetActive ���� ���������� ó��)
        yield return null;

        if (isGameCleared == false)
        {
            Game1Setting game1Setting = JsonLoader.Instance.Settings.game1Setting;

            // �κ��丮 ����
            CreateInventory(game1Setting.inventorySetting, parent);

            // ���� ��ư ����
            for (int i = 0; i < game1Setting.photoButtons.Length; i++)
            {
                GameObject gameButton = CreateButton(game1Setting.photoButtons[i], parent, null);
                Button btn = gameButton.GetComponent<Button>();
                if (btn != null)
                {
                    int index = i;
                    btn.onClick.AddListener(() =>
                    {
                        string itemName = $"Item_{index}";
                        GameObject itemIcon = GameObject.Find(itemName);
                        if (itemIcon != null && itemIcon.TryGetComponent<Image>(out Image itemImage))
                        {
                            itemImage.material = null;

                            itemFoundCount++;
                            if (itemFoundCount == game1Setting.photoButtons.Length)
                            {                               
                                CreatePopup(JsonLoader.Instance.Settings.gameEndPopupSetting, gameBackground);
                                if (inventory != null)
                                {
                                    RectTransform invRT = inventory.GetComponent<RectTransform>();
                                    invRT.anchorMin = invRT.anchorMax = JsonLoader.Instance.Settings.gameEndInventoryPosition;
                                    invRT.pivot = new Vector2(0.5f, 0.5f);
                                    invRT.anchoredPosition = Vector2.zero;
                                }

                                isGameCleared = true;
                            }

                            btn.gameObject.SetActive(false);
                        }
                    });
                }
            }
        }
        else if (isGameCleared == true)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void CreateInventory(InventorySetting setting, GameObject parent)
    {
        // �г� ����
        GameObject panel = new GameObject("Panel_Inventory", typeof(RectTransform));
        panel.transform.SetParent(parent.transform, false);

        inventory = panel;

        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = setting.inventoryPosition;
        panelRT.sizeDelta = setting.inventorySize;
        panelRT.anchoredPosition = Vector2.zero;

        // ��� �̹��� ���� �� �гο� �߰�
        GameObject bg = Instantiate(imagePrefab, panel.transform);
        bg.name = setting.inventoryBackgroundImage.name;

        Image image = bg.GetComponent<Image>();
        Texture2D texture = LoadTexture(setting.inventoryBackgroundImage.imagePath);
        if (texture)
        {
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
        image.color = setting.inventoryBackgroundImage.imageColor;
        image.type = (Image.Type)setting.inventoryBackgroundImage.imageType;

        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = setting.inventoryBackgroundImage.position;
        bgRT.sizeDelta = setting.inventoryBackgroundImage.size;
        bgRT.anchoredPosition = Vector2.zero;

        CreateInventoryItems(setting.itemImages, bgRT, setting.columns, setting.rows, setting.itemPadding);
    }

    private void CreateInventoryItems(ImageSetting[] settings, RectTransform parentRT, int columns, int rows, float padding)
    {
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
        }
    }
    #endregion
}
