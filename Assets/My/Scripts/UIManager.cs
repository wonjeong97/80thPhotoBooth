using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI 생성과 게임 시작, 팝업 처리, 인벤토리 관리 등을 담당하는 클래스
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource; // UI 사운드 소스

    private int itemFoundCount = 0;                     // 게임 버튼을 누른 수

    private float inactivityTimer;
    private float inactivityThreshold = 60f; // 입력이 없는 경우 타이틀로 되돌아가는 시간
    private float fadeTime = 1f; // 페이드 시간

    private GameObject titleCanvasInstance;
    private GameObject gameCanvasInstance;
    private GameObject gameBackgroundInstance;       // 게임 캔버스 백그라운드 이미지
    private GameObject inventoryInstance;            // 인벤토리 오브젝트
    private GameObject goTitleButton;

    private Dictionary<string, Image> itemIcons = new Dictionary<string, Image>();
    private Dictionary<string, AudioClip> soundMap = new Dictionary<string, AudioClip>();
    private Dictionary<string, float> soundVolumeMap = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(LoadSoundsFromSettings()); // 사운드 로딩
    }

    private void Start()
    {
        if (JsonLoader.Instance.Settings == null)
        {
            Debug.LogError("[UIManager] Settings are not loaded yet.");
            return;
        }

        inactivityThreshold = JsonLoader.Instance.Settings.inactivityTime;
        fadeTime = JsonLoader.Instance.Settings.fadeTime;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeUI();
        FadeManager.Instance?.FadeIn(fadeTime);
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        // 게임 캔버스가 활성화 된 경우,
        // 입력이 일정시간 이상 없을 시 씬을 재로드
        if (gameCanvasInstance != null && gameCanvasInstance.activeInHierarchy)
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

    #region UIManager Main Logics
    /// <summary>
    /// UI 초기화: 타이틀과 게임 캔버스 생성 및 설정
    /// </summary>
    private void InitializeUI()
    {
        // 기존 캔버스 제거 (씬 전환 시 메모리 해제)
        if (titleCanvasInstance != null) Destroy(titleCanvasInstance);
        if (gameCanvasInstance != null) Destroy(gameCanvasInstance);

        Addressables.LoadAssetAsync<GameObject>("TitleCanvas").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject titlePrefab = handle.Result;
                titleCanvasInstance = Instantiate(titlePrefab); // 필요 시 부모 지정
                titleCanvasInstance.SetActive(true);

                CreateTitle(); // 타이틀 화면 생성
            }
            else
            {
                Debug.LogWarning("[UIManager] Failed to load TitleCanvas prefab");
            }
        };

        Addressables.LoadAssetAsync<GameObject>("GameCanvas").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject gamePrefab = handle.Result;
                gameCanvasInstance = Instantiate(gamePrefab);
                gameCanvasInstance.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[UIManager] Failed to load GameCanvas prefab");
            }
        };
    }

    /// <summary>
    /// 타이틀 화면 구성
    /// </summary>
    public void CreateTitle()
    {
        var setting = JsonLoader.Instance.Settings.titleSetting;
        string soundKey = setting.startButton.buttonSound;

        // 1. 배경 이미지 생성
        CreateBackgroundImage(setting.backgroundImage, titleCanvasInstance.transform, bg =>
        {
            if (bg != null)
            {
                // 2. 배경 텍스트 및 버튼 생성
                CreateTexts(setting.texts, bg);
                CreateButton(setting.startButton, bg, btn =>
                {
                    // 3. 타이틀 "시작하기" 버튼 클릭 이벤트 연결
                    if (btn != null && btn.TryGetComponent<Button>(out Button button))
                    {
                        button.onClick.AddListener(() =>
                        {
                            PlayClickSound(soundKey);
                            CreateGameUI();
                        });
                    }
                });
            }
        });
    }

    /// <summary>
    /// 게임화면 UI 구성
    /// </summary>
    public void CreateGameUI()
    {
        var setting = JsonLoader.Instance.Settings.game1Setting;

        CreateBackgroundImage(setting.game1BackgroundImage, gameCanvasInstance.transform, bg =>
        {
            gameBackgroundInstance = bg;    // 게임 배경 인스턴스 저장

            Destroy(titleCanvasInstance);
            gameCanvasInstance.SetActive(true);

            // 게임 시작 전 스타트 팝업 생성
            var startPopupSetting = JsonLoader.Instance.Settings.game1Setting.popupSetting;
            CreatePopup(startPopupSetting, gameBackgroundInstance.transform, startPopup =>
            {

                StartCoroutine(AfterCloseStartPopup(gameBackgroundInstance));
            });
        });
    }

    /// <summary>
    /// 스타트 팝업 창이 닫힌 후 인벤토리, 게임 버튼 생성 및 이벤트 연결
    /// </summary>
    private IEnumerator AfterCloseStartPopup(GameObject parent)
    {
        yield return null;

        var setting = JsonLoader.Instance.Settings;

        // 1. 인벤토리 이미지 생성
        CreateInventory(setting.game1Setting.inventorySetting, parent);

        // 2. 게임 내 "핀 포인트 버튼" 생성"
        CreateButton(setting.game1Setting.pinPointButton, parent, pinBtn => {
            if (pinBtn != null && pinBtn.TryGetComponent<Button>(out Button button))
            {
                button.onClick.AddListener(() =>
                {
                    Debug.Log("Pin btn clicked1");
                    CreatePopup(setting.pinPointPopupSetting, gameBackgroundInstance.transform, pinPointPopup =>
                    {
                        Debug.Log("Pin btn clicked2");
                    });
                });
            }    

        });

        // 3. 게임 내 "카메라 이미지 버튼" 생성
        for (int i = 0; i < setting.game1Setting.photoButtons.Length; i++)
        {
            int index = i;
            string soundKey = setting.game1Setting.photoButtons[i].buttonSound;

            CreateButton(setting.game1Setting.photoButtons[i], parent, btnGO =>
            {
                if (btnGO != null && btnGO.TryGetComponent<Button>(out Button btn))
                {
                    // 3. 카메라 이미지 버튼 클릭 시 동작 이벤트 연결
                    btn.onClick.AddListener(() =>
                    {
                        // 기존 방식: 아이템 인덱스에 맞춰 인벤토리 아이템 색상 켜짐
                        //if (itemIcons.TryGetValue($"Item_{index}", out Image itemImage))
                        //{
                        //    PlayClickSound(soundKey);       // 버튼 클릭음 재생
                        //    itemImage.material = null;      // 연결된 아이템 이미지 색 원복
                        //    itemFoundCount++;               // 아이템 찾음 수 증가
                        //    Destroy(btn.gameObject);        // 버튼 파괴

                        //    // 4. 각 카메라 버튼에 맞는 설명 팝업 생성
                        //    CreatePopup(setting.explainPopupSetting[index],
                        //        gameBackgroundInstance.transform, explainPopup =>
                        //    {
                        //        if (itemFoundCount == setting.game1Setting.inventorySetting.itemImages.Length)
                        //        {
                        //            CreateGameEndPopup();
                        //        }
                        //    });
                        //}
                        // 변경방식: 왼쪽부터 빈칸 채우기                        
                        if (itemFoundCount < itemIcons.Count)
                        {
                            PlayClickSound(soundKey);

                            // 현재 버튼에 있는 카메라 이미지를 가져와서 인벤토리 칸에 적용
                            if (btn.TryGetComponent<Image>(out Image btnImage) &&
                                itemIcons.TryGetValue($"Item_{itemFoundCount}", out Image slotImage))
                            {
                                slotImage.sprite = btnImage.sprite;
                                slotImage.color = Color.white; // 원본 색상
                            }

                            itemFoundCount++;
                            Destroy(btn.gameObject); // 버튼 제거

                            CreatePopup(setting.explainPopupSetting[index],
                                gameBackgroundInstance.transform, explainPopup =>
                                {
                                    if (itemFoundCount == setting.game1Setting.inventorySetting.itemImages.Length)
                                    {
                                        CreateGameEndPopup();
                                    }
                                });
                        }
                    });
                }
            });
        }

        // "처음으로" 버튼 생성 및 이벤트 연결
        CreateButton(setting.game1Setting.goTitleButton, parent, goTitleBtn =>
        {
            if (goTitleBtn != null && goTitleBtn.TryGetComponent<Button>(out Button btn))
            {
                goTitleButton = goTitleBtn;
                btn.onClick.AddListener(() =>
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                });
            }
        });
    }

    /// <summary>
    /// 게임 종료 팝업을 생성하고 인벤토리 위치를 중앙으로 이동시킵니다.
    /// </summary>
    private void CreateGameEndPopup()
    {
        PopupSetting setting = JsonLoader.Instance.Settings.gameEndPopupSetting;

        // 1. 팝업 백그라운드 이미지 생성
        CreateBackgroundImage(setting.popupBackgroundImage, gameBackgroundInstance.transform, popupBG =>
        {
            popupBG.transform.SetAsLastSibling();

            // 2. 텍스트 생성
            //foreach (var textSetting in setting.popupTexts)
            //    CreateTexts(new[] { textSetting }, popupBG);

            // 3. 이미지 생성
            foreach (var imgSetting in setting.popupImages)
            {
                CreateImage(imgSetting, popupBG, null);
            }
                

            // 버튼 생성
            CreateButton(setting.popupButton, popupBG, btnGO =>
            {
                if (btnGO != null && btnGO.TryGetComponent<Button>(out Button btn))
                {
                    btn.onClick.AddListener(() =>
                    {
                        Destroy(popupBG);
                        FadeManager.Instance?.FadeOut(fadeTime, () =>
                        {
                            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                        });
                    });
                }
            });

            goTitleButton.SetActive(false);

            if (inventoryInstance != null)
            {
                inventoryInstance.transform.SetParent(popupBG.transform);
                if (inventoryInstance.TryGetComponent<RectTransform>(out var invRT))
                {
                    invRT.pivot = new Vector2(0.5f, 0.5f);
                    invRT.anchoredPosition = new(JsonLoader.Instance.Settings.gameEndInventoryPosition.x,
                        -JsonLoader.Instance.Settings.gameEndInventoryPosition.y);
                }
            }
        });
    }

    private void CreatePopup(PopupSetting setting, Transform parent, UnityAction<GameObject> OnClose = null)
    {
        // 1. 팝업 백그라운드 이미지 생성
        CreateBackgroundImage(setting.popupBackgroundImage, parent, popupBG =>
        {
            popupBG.transform.SetAsLastSibling();

            // 2. 텍스트 생성
            foreach (var textSetting in setting.popupTexts)
                CreateTexts(new[] { textSetting }, popupBG);

            // 3. 이미지 생성
            foreach (var imgSetting in setting.popupImages)
                CreateImage(imgSetting, popupBG, null);

            // 버튼 생성
            CreateButton(setting.popupButton, popupBG, btnGO =>
            {
                if (btnGO != null && btnGO.TryGetComponent<Button>(out Button btn))
                {
                    btn.onClick.AddListener(() =>
                    {
                        Destroy(popupBG);
                        OnClose?.Invoke(popupBG);
                    });
                }
            });
        });
    }

    private void CreateInventory(InventorySetting setting, GameObject parent)
    {
        CreateImage(setting.inventoryBackgroundImage, parent, bg =>
        {
            if (bg != null)
            {
                inventoryInstance = bg;
                CreateInventoryItems(setting.itemImages, bg.transform);
            }
        });
    }

    private void CreateInventoryItems(ImageSetting[] items, Transform parent)
    {
        itemIcons.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            int index = i; // 비동기 콜백에서 안전하게 참조하기 위한 복사

            CreateImage(items[index], parent.gameObject, createdImgGO =>
            {
                if (createdImgGO != null && createdImgGO.TryGetComponent<Image>(out Image img))
                {
                    // 처음엔 빈칸: 스프라이트 제거, 색상만 유지
                    img.sprite = null;
                    img.color = new Color(1, 1, 1, 0);

                    // itemIcons에 등록
                    itemIcons[$"Item_{index}"] = img;
                }
            });
        }
    }
    #endregion

    #region UI Create
    /// <summary>
    /// 배경 이미지 생성
    /// </summary>
    private void CreateBackgroundImage(ImageSetting setting, Transform parent, Action<GameObject> onComplete)
    {
        LoadPrefabAndInstantiate("ImagePrefab", parent, background =>
        {
            if (background == null) { onComplete?.Invoke(null); return; }
            background.name = setting.name;
            background.transform.SetAsFirstSibling();

            if (background.TryGetComponent<Image>(out Image image))
            {
                Texture2D texture = LoadTexture(setting.imagePath);
                if (texture)
                    image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                image.color = setting.imageColor;
            }

            if (background.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = setting.size;
                rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
            }

            onComplete?.Invoke(background);
        });
    }

    /// <summary>
    /// 이미지 생성
    /// </summary>
    private void CreateImage(ImageSetting setting, GameObject parent, Action<GameObject> onComplete)
    {
        LoadPrefabAndInstantiate("ImagePrefab", parent.transform, go =>
        {
            if (go == null) { onComplete?.Invoke(null); return; }
            go.name = setting.name;

            if (go.TryGetComponent<Image>(out Image image))
            {
                Texture2D texture = LoadTexture(setting.imagePath);
                if (texture)
                    image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                image.color = setting.imageColor;
                image.type = (Image.Type)setting.imageType;
            }

            if (go.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
                rt.sizeDelta = setting.size;
            }

            onComplete?.Invoke(go);
        });
    }

    /// <summary>
    /// 텍스트 생성
    /// </summary>
    private void CreateTexts(TextSetting[] settings, GameObject parent, Action onComplete = null)
    {
        int remaining = settings.Length;
        if (remaining == 0) { onComplete?.Invoke(); return; }

        foreach (var setting in settings)
        {
            LoadPrefabAndInstantiate("TextPrefab", parent.transform, go =>
            {
                if (go != null)
                {
                    go.name = setting.name;
                    if (go.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI uiText))
                    {
                        LoadFontAndApply(uiText, setting.fontResourceName, setting.text, setting.fontSize, setting.fontColor);
                    }
                    if (go.TryGetComponent<RectTransform>(out RectTransform rt))
                    {
                        rt.anchoredPosition = new Vector2(setting.position.x, -setting.position.y);
                        rt.localRotation = Quaternion.Euler(0, 0, setting.rotationZ);
                    }
                }

                remaining--;
                if (remaining <= 0) onComplete?.Invoke();
            });
        }
    }

    /// <summary>
    /// 버튼 생성 및 클릭 이벤트 연결
    /// </summary>
    private void CreateButton(ButtonSetting setting, GameObject parent, Action<GameObject> onComplete = null)
    {
        LoadPrefabAndInstantiate("ButtonPrefab", parent.transform, go =>
        {
            if (go == null) { onComplete?.Invoke(null); return; }
            go.name = setting.name;

            // 1) 버튼 배경 이미지
            if (go.TryGetComponent<Image>(out Image bgImage) && setting.buttonBackgroundImage != null)
            {
                Texture2D texture = LoadTexture(setting.buttonBackgroundImage.imagePath);
                if (texture)
                    bgImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                bgImage.color = setting.buttonBackgroundImage.imageColor;
                bgImage.type = (Image.Type)setting.buttonBackgroundImage.imageType;
            }

            // 2) 버튼 추가 이미지 (자식 오브젝트로 생성)
            if (setting.buttonAdditionalImage != null && !string.IsNullOrEmpty(setting.buttonAdditionalImage.imagePath))
            {
                CreateImage(setting.buttonAdditionalImage, go, addImgGO =>
                {
                    if (addImgGO != null && addImgGO.TryGetComponent<RectTransform>(out RectTransform addRT))
                    {
                        addRT.anchoredPosition = new Vector2(setting.buttonAdditionalImage.position.x, -setting.buttonAdditionalImage.position.y);
                        addRT.sizeDelta = setting.buttonAdditionalImage.size;
                    }
                });
            }

            // 3) 버튼 텍스트
            var textComp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null && setting.buttonText != null && !string.IsNullOrEmpty(setting.buttonText.text))
            {
                LoadFontAndApply(textComp, setting.buttonText.fontResourceName,
                                 setting.buttonText.text, setting.buttonText.fontSize, setting.buttonText.fontColor);
                if (textComp.TryGetComponent<RectTransform>(out RectTransform textRT))
                {
                    textRT.anchoredPosition = new Vector2(setting.buttonText.position.x, setting.buttonText.position.y);
                    textRT.localRotation = Quaternion.Euler(0, 0, setting.buttonText.rotationZ);
                }
            }

            // 4) 버튼 크기와 위치
            if (go.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.sizeDelta = setting.buttonSize;
                rt.anchoredPosition = new Vector2(setting.buttonPosition.x, -setting.buttonPosition.y);
            }

            onComplete?.Invoke(go);
        });
    }
    #endregion

    #region Utilities
    /// <summary>
    /// Path로부터 이미지를 읽고 텍스쳐로 변환
    /// </summary>    
    private Texture2D LoadTexture(string relativePath)
    {
        // 경로가 비어있음
        if (string.IsNullOrEmpty(relativePath)) return null;

        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

        // Path가 존재하지 않음, 잘못된 경로
        if (!File.Exists(fullPath)) return null;

        byte[] fileData = File.ReadAllBytes(fullPath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        return texture;
    }

    /// <summary>
    /// JSON의 font 키값(font1, font2 등)을 실제 폰트 파일 이름으로 매핑
    /// FontMapping 클래스의 필드명을 리플렉션으로 찾아 반환
    /// </summary>    
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
    /// JSON에 정의된 사운드 목록을 StreamingAssets/Audio 경로에서 로드하여 soundMap에 저장
    /// 각 사운드는 key-clip 쌍으로 저장되며, 재생 시 key로 참조
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
    /// 지정된 사운드 키에 해당하는 AudioClip을 찾아 재생
    /// 사운드는 UI 버튼 클릭 시 사용
    /// </summary>
    private void PlayClickSound(string key)
    {
        // AudioSource가 있고, 해당 key에 대한 clip이 있으면 재생
        if (uiAudioSource != null && soundMap.TryGetValue(key, out AudioClip clip))
        {
            // 키에 해당하는 볼륨 없으면 기본값 1.0 사용
            float volume = soundVolumeMap.TryGetValue(key, out float v) ? v : 1.0f;
            uiAudioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Addressable로 폰트를 로드 후 텍스트 설정
    /// </summary>
    private void LoadFontAndApply(TextMeshProUGUI uiText, string fontKey, string textValue, int fontSize, Color fontColor, Action onComplete = null)
    {
        if (uiText == null || string.IsNullOrEmpty(fontKey))
            return;

        uiText.enabled = false; // 로딩 전 렌더 방지      

        string mappedFontName = ResolveFont(fontKey);

        Addressables.LoadAssetAsync<TMP_FontAsset>(mappedFontName).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                uiText.font = handle.Result;
                uiText.fontSize = fontSize;
                uiText.color = fontColor;
                uiText.alignment = TextAlignmentOptions.Center;
                uiText.text = textValue;
                uiText.enabled = true;

                onComplete?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[UIManager] Font load failed: {mappedFontName}");
            }
        };
    }

    /// <summary>
    /// 타깃 이미지에 Addressable로 로드한 머티리얼을 적용함
    /// </summary>
    private void LoadMaterialAndApply(Image targetImage, string materialKey)
    {
        if (targetImage == null || string.IsNullOrEmpty(materialKey))
            return;

        Addressables.LoadAssetAsync<Material>(materialKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                targetImage.material = handle.Result;
            }
            else
            {
                Debug.LogWarning($"[UIManager] Material load failed: {materialKey}");
            }
        };
    }

    /// <summary>
    /// 프리팹을 로드 후 Instantiate,
    /// 성공 시 연결된 이벤트 호출하면서 생성된 프리팹 오브젝트 반환
    /// </summary>
    private void LoadPrefabAndInstantiate(string key, Transform parent, Action<GameObject> onComplete)
    {
        Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject go = Instantiate(handle.Result, parent);
                onComplete?.Invoke(go);
            }
            else
            {
                Debug.LogWarning($"[UIManager] Failed to load prefab: {key}");
                onComplete?.Invoke(null);
            }
        };
    }

    private IEnumerator FadeInProperties(Image[] images, float durationPerImage, Action onComplete = null)
    {
        foreach (var img in images)
        {
            if (img == null) continue;

            // 시작 알파 0
            Color c = img.color;
            c.a = 0f;
            img.color = c;

            float elapsed = 0f;
            while (elapsed < durationPerImage)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / durationPerImage);

                c.a = alpha;
                img.color = c;

                yield return null;
            }

            // 안전하게 알파 1 고정
            c.a = 1f;
            img.color = c;
        }

        onComplete?.Invoke();
    }
    #endregion
}