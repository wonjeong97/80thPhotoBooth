using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region static 변수

    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>(true);
            }

            return _instance;
        }
    }

    #endregion

    #region private 변수

    [SerializeField] private Reporter reporter;

    public event Action onReset;

    private float _fTime;
    private float _fResetTime;
    private bool _isInteract = false;

    #endregion

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    private void Start()
    {
        Cursor.visible = false;

        //WaitUntil jsonWait = new WaitUntil(() => CJsonManager.Instance.GeneralSetting != null);
        //yield return jsonWait;

       // _fResetTime = CJsonManager.Instance.GeneralSetting.resetTime;
    }

    private void Update()
    {
        // D키를 눌러 디버그 패널 활성화 / 비활성화
        if (Input.GetKeyDown(KeyCode.D))
        {
            reporter.showGameManagerControl = !reporter.showGameManagerControl;

            if (reporter.show)
            {
                reporter.show = false;
            }
        }

        else if (Input.GetKeyDown(KeyCode.M))
        {
            Cursor.visible = !Cursor.visible;
        }


        // 초기화 로직
        if (_isInteract)
        {
            _fTime += Time.deltaTime;

            if (_fTime >= _fResetTime)
            {
                Reset();
            }
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void Reset()
    {
        onReset?.Invoke();

        _isInteract = false;
        _fTime = 0.0f;
    }

    /// <summary>
    /// 상호작용이 생겼을 때 TimeCount를 중지한다.
    /// </summary>
    public void Interact()
    {
        _fTime = 0.0f;
        _isInteract = true;
    }

    /// <summary>
    /// 초기화 로직을 위해 시간을 재는 것을 중지
    /// </summary>
    public void StopResetCount()
    {
        _fTime = 0.0f;
        _isInteract = false;
    }
}