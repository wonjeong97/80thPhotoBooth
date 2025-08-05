using UnityEngine;
using UnityEngine.UI;

// Ư����ġ ȭ�� ��ġ�� ���� ����
public class GameCloser : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    private CloseSetting closeSetting;

    private int clickCount = 0;
    private float timer = 0f;
    private bool counting = false;

    private void Start()
    {
        closeSetting = JsonLoader.Instance.Settings.closeSetting;

        if (rectTransform != null)
        {
            Vector2 anchor = closeSetting.position;
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.GetComponent<Image>().color = new Color(1, 1, 1, closeSetting.imageAlpha);
        }
    }

    private void Update()
    {
        if (!counting) return;

        timer += Time.deltaTime;

        if (timer >= closeSetting.resetClickTime)
        {
            ResetClickCount();
        }
    }

    /// <summary>
    /// Ŭ�� �� ȣ��Ǿ� Ŭ�� Ƚ���� ������ŵ�ϴ�.
    /// </summary>
    public void Click()
    {
        counting = true;
        clickCount++;

        if (clickCount >= closeSetting.numToClose)
        {
            ExitGame();
        }
    }

    private void ResetClickCount()
    {
        clickCount = 0;
        timer = 0f;
        counting = false;
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
