using System;
using System.IO;
using UnityEngine;

public class JsonLoader : MonoBehaviour
{
    [NonSerialized] public Settings Settings;

    private static JsonLoader _instance;
    public static JsonLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<JsonLoader>() ?? new GameObject("JsonLoader").AddComponent<JsonLoader>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Settings = LoadJsonData<Settings>("Settings.json");
    }

    private void Start()
    {
        if (Settings == null) return;

    }

    private T LoadJsonData<T>(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName).Replace("\\", "/");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("[JsonLoader] File is not exits: " + filePath);
            return default;
        }

        string json = File.ReadAllText(filePath);
        Debug.Log("[JsonLoader] JSON load complete: " + json);

        return JsonUtility.FromJson<T>(json);
    }
}

#region Json Settings
[Serializable] public enum UIImageType { Simple = 0, Sliced, Tiled, Filled }

[Serializable]
public class CloseSetting
{
    public Vector2 position;
    public int numToClose;
    public float resetClickTime;
    public float imageAlpha;
}

[Serializable]
public class ImageSetting
{
    public string name;
    public string imagePath;
    public Vector2 position;
    public Vector2 size;
    public Color imageColor;
    public UIImageType imageType;
}
[Serializable]
public class TextSetting
{
    public string name;
    public string text;
    public Vector2 position;
    public float rotationZ;
    public string fontResourceName;
    public int fontSize;
    public Color fontColor;
}

[Serializable]
public class FontMapping
{
    public string font1;
    public string font2;
    public string font3;
    public string font4;
    public string font5;
}

[Serializable]
public class VideoSetting
{
    public string name;
    public string fileName;
    public float volume;
    public Vector2 position;
    public Vector2 size;
}

[Serializable]
public class SoundSetting
{
    public string key;
    public String clipPath;
    public float volume = 1.0f;
}

[Serializable]
public class KeyboardSetting
{
    public string name;
    public Vector2 position;
    public Vector2 size;
}

[Serializable]
public class PageSetting
{
    public string name;
    public TextSetting[] texts;
    public ImageSetting[] images;
    public VideoSetting[] videos;
    public KeyboardSetting[] keyboards;
}

[Serializable]
public class PrintSetting
{
    public string printerName;
    public string printFont;
    public int printFontSize;
    public string[] printKeys;
}

[Serializable]
public class ButtonSetting
{
    public string name;
    public Vector2 buttonSize;
    public Vector2 buttonPosition;
    public ImageSetting buttonBackgroundImage;
    public ImageSetting buttonAdditionalImage;
    public TextSetting buttonText;
    public string buttonSound;
}

[Serializable]
public class TitleSetting
{
    public ImageSetting backgroundImage;
    public TextSetting[] texts;
    public ButtonSetting startButton;
}

[Serializable]
public class PopupSetting
{
    public string name;
    public ImageSetting popupBackgroundImage;
    public TextSetting[] popupTexts;
    public ImageSetting[] popupImages;
    public ButtonSetting popupButton;
}

[Serializable]
public class InventorySetting
{
    public string name;
    public int columns;
    public int rows;
    public ImageSetting inventoryBackgroundImage;
    public ImageSetting[] itemImages;
}

[Serializable]
public class Game1Setting
{
    public ImageSetting game1BackgroundImage;
    public PopupSetting popupSetting;
    public InventorySetting inventorySetting;
    public ButtonSetting[] photoButtons;
    public ButtonSetting goTitleButton;
}

[Serializable]
public class Settings
{
    public float inactivityTime; // 입력이 없을 시 타이틀로 되돌아가는 시간
    public float fadeTime; // 페이드 시간
    public FontMapping fontMap;
    public SoundSetting[] sounds;
    public CloseSetting closeSetting;
    public TitleSetting titleSetting;
    public Game1Setting game1Setting;
    public PopupSetting[] explainPopupSetting;
    public PopupSetting gameEndPopupSetting;
    public Vector2 gameEndInventoryPosition;
}
#endregion