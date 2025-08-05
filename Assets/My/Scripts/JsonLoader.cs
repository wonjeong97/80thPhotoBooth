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
            DontDestroyOnLoad(gameObject); // 인스턴스 유지
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
            Debug.LogWarning("[JsonLoader] 파일이 존재하지 않습니다: " + filePath);
            return default;
        }

        string json = File.ReadAllText(filePath);
        Debug.Log("[JsonLoader] JSON 로드 완료: " + json);

        return JsonUtility.FromJson<T>(json);
    }
}
