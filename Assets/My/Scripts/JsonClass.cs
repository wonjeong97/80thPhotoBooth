using System;
using UnityEngine;

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
public class VideoSetting
{   
    public string name;
    public string fileName;
    public float volume;
    public Vector2 position;
    public Vector2 size;
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
    public Vector2 inventorySize;
    public Vector2 inventoryPosition;
    public int columns;
    public int rows;
    public float itemPadding;
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
}

[Serializable]
public class Settings
{
    public CloseSetting closeSetting;
    public TitleSetting titleSetting;
    public Game1Setting game1Setting;
    public PopupSetting gameEndPopupSetting;
    public Vector2 gameEndInventoryPosition;
}

public class JsonClass : MonoBehaviour
{
   
}
