using UnityEngine;

/// <summary>
/// 마우스 커서를 제어하며, 씬이 바뀌어도 커서가 유지되도록 관리하는 클래스
/// </summary>
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [SerializeField] private Texture2D cursorTexture; // 커서로 사용할 텍스처
    private Vector2 hotspot;    // 커서의 기준 지점

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
        // 커서가 비활성화되었을 경우 다시 보이게 설정
        if (Cursor.visible == false)
        {
            Cursor.visible = true;
        }
    }
}
