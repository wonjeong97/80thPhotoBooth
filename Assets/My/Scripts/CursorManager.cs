using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [SerializeField] private Texture2D cursorTexture;

    private Vector2 hotspot;
    private CursorMode cursorMode = CursorMode.ForceSoftware;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            hotspot = new Vector2(0, 1);
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.ForceSoftware);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        //Debug.Log("Cursor.visible: " + Cursor.visible);
        //Debug.Log("Cursor.lockState: " + Cursor.lockState);

        if (cursorTexture != null)
        {
            //Debug.Log($"Cursor texture: {cursorTexture.name}, Size: {cursorTexture.width}x{cursorTexture.height}");
            Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width / 2, cursorTexture.height / 2), CursorMode.ForceSoftware);
        }
    }

    private void Update()
    {
        if (Cursor.visible == false)
        {
            Cursor.visible = true;
        }
    }
}
