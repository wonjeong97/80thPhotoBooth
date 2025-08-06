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
/// UI ������ ���� ����, �˾� ó��, �κ��丮 ���� ���� ����ϴ� Ŭ����
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Canvas")]
    [SerializeField] private Transform titleCanvas;     // Ÿ��Ʋ ĵ����
    [SerializeField] private Transform gameCanvas;      // ���� ĵ����

    [Header("UI ������")]
    [SerializeField] private GameObject buttonPrefab;   // ���� ���� ��ư ������
    [SerializeField] private GameObject textPrefab;     // ���� ���� �ؽ�Ʈ ������
    [SerializeField] private GameObject imagePrefab;    // ���� ���� �̹��� ������

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource; // UI ���� �ҽ�

    private int itemFoundCount = 0;                     // ���� ��ư�� ���� ��

    private float inactivityTimer;
    private float inactivityThreshold = 60f; // �Է��� ���� ��� Ÿ��Ʋ�� �ǵ��ư��� �ð�

    private GameObject gameBackground;       // ���� ĵ���� ��׶��� �̹���
    private GameObject inventory;            // �κ��丮 ������Ʈ

    private Dictionary<string, Image> itemIcons = new Dictionary<string, Image>();
    private Dictionary<string, AudioClip> soundMap = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        StartCoroutine(LoadSoundsFromSettings()); // ���� �ε�
    }

    private void Start()
    {
        CreateTitle();  // Ÿ��Ʋ �̹��� ����

        if (JsonLoader.Instance.Settings != null)
        {   
            // JSON ���ÿ��� ���Է½ð��� �޾ƿ�
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

            // �Ӱ谪 �ʰ� �� �� ��ε�
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
            // ��ŸƮ ��ư Ŭ�� �� ���� ����

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
        bg.transform.SetAsFirstSibling(); // �� �ڷ� ������

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
    /// �ؽ�Ʈ ����
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
                uiText.alignment = TextAlignmentOptions.Center; // �߾� ����
            }            

            if (go.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
                rt.localRotation = Quaternion.Euler(0, 0, setting.rotationZ);
            }           
        }
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
    /// ��ư ���� �� Ŭ�� �̺�Ʈ ����
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

        // ��� �̹��� ����
        if (go.TryGetComponent<Image>(out Image image) && setting.buttonImage != null)
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
            
            if (text.TryGetComponent<RectTransform>(out RectTransform textRT))
            {
                textRT.localRotation = Quaternion.Euler(0, 0, setting.buttonText.rotationZ);
            }            
        }

        // ��ư Ŭ�� �̺�Ʈ
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
            StartCoroutine(AfterCloseStartPopup(gameBackground));
        });
    }

    /// <summary>
    /// ��ŸƮ �˾� â�� ���� �� �κ��丮, ���� ��ư ���� �� �̺�Ʈ ����
    /// </summary>
    /// <param name="parent">�κ��丮 �� ���� ��ư�� �ٴ� ���� ĵ������ ��� �̹���</param>
    /// <returns></returns>
    private IEnumerator AfterCloseStartPopup(GameObject parent)
    {
        // 1 ������ ��ٷȴٰ� ���� (SetActive ���� ���������� ó��)
        yield return null;

        Game1Setting game1Setting = JsonLoader.Instance.Settings.game1Setting;
        CreateInventory(game1Setting.inventorySetting, parent); // �κ��丮 ����

        // ���� ��ư ���� �� �̺�Ʈ ����
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
                        itemImage.material = null;  // ����� ������ �̹����� �� ����
                        itemFoundCount++;

                        btn.gameObject.SetActive(false);
                        CreateExplainPopup(index); // ���� �˾� ����
                    }
                });
            }
        }
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

    private void CreateInventory(InventorySetting setting, GameObject parent)
    {
        // ��� �̹��� ����
        GameObject bg = Instantiate(imagePrefab, parent.transform);
        bg.name = setting.inventoryBackgroundImage.name;
        inventory = bg;

        if(bg.TryGetComponent<Image>(out Image image))
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
        itemIcons.Clear(); // ���� ���� �� �ʱ�ȭ

        Vector2 cellSize = new Vector2(
            (parentRT.sizeDelta.x - padding * (columns + 1)) / columns,
            (parentRT.sizeDelta.y - padding * (rows + 1)) / rows
        );

        // Column, Row�� ���� ������ �̹��� ����
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
    /// JSON�� font Ű��(font1, font2 ��)�� ���� ��Ʈ ���� �̸����� �����մϴ�.
    /// FontMapping Ŭ������ �ʵ���� ���÷������� ã�� ��ȯ�մϴ�.
    /// </summary>
    /// <param name="key">JSON ���� ��õ� font Ű (��: "font1")</param>
    /// <returns>���� ��Ʈ ���ҽ� ���� �̸� (��: "NanumGothic-Regular SDF")</returns>
    private string ResolveFont(string key)
    {
        FontMapping fontMap = JsonLoader.Instance.Settings.fontMap;
        if (fontMap == null) return key;

        // ���÷����� ���� key�� �ش��ϴ� �ʵ� ������ ����
        var field = typeof(FontMapping).GetField(key);
        if (field != null)
        {
            return field.GetValue(fontMap) as string ?? key; // ���� ��Ʈ �̸� ��ȯ
        }

        return key; // ���� ���� �� ���� key ��ȯ
    }

    /// <summary>
    /// JSON�� ���ǵ� ���� ����� StreamingAssets/Audio ��ο��� �ε��Ͽ� soundMap�� �����մϴ�.
    /// �� ����� key-clip ������ ����Ǹ�, ��� �� key�� �����մϴ�.
    /// </summary>
    private IEnumerator LoadSoundsFromSettings()
    {
        soundMap.Clear();

        SoundSetting[] soundEntries = JsonLoader.Instance.Settings.sounds;
        if (soundEntries == null) yield break;

        foreach (SoundSetting entry in soundEntries)
        {
            // ��ü ���� ��� ����
            string fullPath = Path.Combine(Application.streamingAssetsPath, "Audio", entry.clipPath).Replace("\\", "/");

            // ���Ͽ��� ����� Ŭ�� �ε� (WAV ���� ����)
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // ���������� �ε�Ǹ� Dictionary�� ����
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                soundMap[entry.key] = clip;
            }
            else
            {
                Debug.LogWarning($"[SoundLoader] ����: {entry.clipPath} - {www.error}");
            }
        }
    }

    /// <summary>
    /// ������ ���� Ű�� �ش��ϴ� AudioClip�� ã�� ����մϴ�.
    /// ����� UI ��ư Ŭ�� �� ���˴ϴ�.
    /// </summary>
    /// <param name="key">JSON�� ��ϵ� ���� Ű (��: "click1")</param>
    private void PlayClickSound(string key)
    {
        // AudioSource�� �ְ�, �ش� key�� ���� clip�� ������ ���
        if (uiAudioSource != null && soundMap.TryGetValue(key, out AudioClip clip))
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }
    #endregion
}
