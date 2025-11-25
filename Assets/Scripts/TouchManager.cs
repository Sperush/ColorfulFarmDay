using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchManager : MonoBehaviour
{
    public static bool IsPanelOpen = false;

    private Vector2 clickStartPos;
    private Vector3 camStartPos;
    private float tapThreshold = 0.2f;
    private bool isTouching = false;

    void Update()
    {
        if (IsPointerOverBlockingUI())
        {
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            camStartPos = Camera.main.transform.position;
            isTouching = true;
        }

        if (Input.GetMouseButtonUp(0) && isTouching)
        {
            isTouching = false;
            if (IsPanelOpen)
            {
                return;
            }

            Vector2 endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float distance = Vector2.Distance(clickStartPos, endPos);
            float camMove = Vector3.Distance(Camera.main.transform.position, camStartPos);

            if (distance < tapThreshold && camMove < 0.05f)
            {
                ProcessClick(endPos);
            }
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    clickStartPos = worldPos;
                    camStartPos = Camera.main.transform.position;
                    isTouching = true;
                    break;

                case TouchPhase.Ended:
                    if (!isTouching || IsPanelOpen) return;
                    isTouching = false;

                    float distance = Vector2.Distance(clickStartPos, worldPos);
                    float camMove = Vector3.Distance(Camera.main.transform.position, camStartPos);

                    if (distance < tapThreshold && camMove < 0.05f)
                    {
                        ProcessClick(worldPos);
                    }
                    break;
            }
        }
    }

    bool IsPointerOverBlockingUI()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);

#if UNITY_ANDROID || UNITY_IOS
    if (Input.touchCount == 0)
        return false;
    pointerData.position = Input.GetTouch(0).position;
#else
        pointerData.position = Input.mousePosition;
#endif

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            GameObject go = result.gameObject;

            // Bỏ qua nếu không có GraphicRaycaster / không phải UI thực
            if (go.GetComponent<GraphicRaycaster>() == null && go.GetComponent<CanvasRenderer>() == null)
                continue;

            // Nếu có CanvasGroup và đang chặn raycast → true
            CanvasGroup cg = go.GetComponentInParent<CanvasGroup>();
            if (cg == null || cg.blocksRaycasts)
                return true;
        }

        return false;
    }


    void ProcessClick(Vector2 pos)
    {
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero);
        if (hit.collider != null)
        {
            AudioManager.Instance.Play(GameSound.ButtonClick);

            ClickableObject clickable = hit.collider.GetComponent<ClickableObject>();
            if (clickable != null)
            {
                ProDuceManager produceManager = clickable.GetComponent<ProDuceManager>();
                if (produceManager != null)
                {
                    ButtonBounceEffect bounce = clickable.GetComponent<ButtonBounceEffect>();
                    if (bounce != null) bounce.TriggerBounce();

                    clickable.OnClicked(produceManager);
                }
            }
        }
    }
}
