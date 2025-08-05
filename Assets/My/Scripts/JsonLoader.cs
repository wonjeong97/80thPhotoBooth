using System;
using System.IO;
using UnityEngine;

[Serializable]
public enum UIImageType { Simple = 0, Sliced, Tiled, Filled }
[Serializable] public class CloseSetting { public Vector2 position; public int numToClose; public float resetClickTime; public float imageAlpha; }
[Serializable] public class ImageSetting { public string imagePath; public Vector2 position; public Vector2 size; public Color imageColor; public UIImageType imageType; }
[Serializable] public class TextSetting { public string text; public Vector2 position; public float rotationZ; public string fontResourceName; public int fontSize; public Color fontColor; }
[Serializable] public class VideoSetting { public string fileName; public float volume; public Vector2 position; public Vector2 size; }
[Serializable] public class KeyboardSetting { public Vector2 position; public Vector2 size; }
[Serializable] public class PageSetting { public TextSetting[] texts; public ImageSetting[] images; public VideoSetting[] videos; public KeyboardSetting[] keyboards; }
[Serializable] public class PrintSetting { public string printerName; public string printFont; public int printFontSize; public string[] printKeys; }
[Serializable]
public class ButtonSetting
{
    public Vector2 buttonSize;
    public Vector2 buttonPosition;
    public ImageSetting buttonImage;
    public TextSetting buttonText;
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
    public ImageSetting popupBackgroundImage;
    public TextSetting[] popupTexts;
    public ImageSetting[] popupImages;
    public ButtonSetting popupButton;
}

[Serializable]
public class InventorySetting
{
    public ImageSetting inventoryBackgroundImage;
    public ImageSetting[] itemImages;
}

[Serializable]
public class Game1Setting
{
    public ImageSetting game1BackgroundImage;
    public PopupSetting popupSetting;
    public InventorySetting inventorySetting;
    //public ButtonSetting[] photoButtons;
}

[Serializable]
public class Settings
{
    public CloseSetting closeSetting;
    public TitleSetting titleSetting;
    public Game1Setting game1Setting;
}

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
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }

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
            Debug.LogWarning("[JsonLoader] 파일이 존재하지 않습니다: " + filePath);
            return default;
        }

        string json = File.ReadAllText(filePath);
        Debug.Log("[JsonLoader] JSON 로드 완료: " + json);

        return JsonUtility.FromJson<T>(json);
    }
}
