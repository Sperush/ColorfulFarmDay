using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class CameraController : MonoBehaviour
{
    public float dragSpeed = 5f;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;
    [Header("Camera Limit")]
    public SpriteRenderer backgroundSprite; // gán "nền cỏ" vào đây
    private Vector2 minLimit;
    private Vector2 maxLimit;

    private Vector3 dragOrigin;
    private bool isDragging;
    void Start()
    {
        // Lấy vùng giới hạn từ SpriteRenderer
        Bounds bounds = backgroundSprite.bounds;
        minLimit = bounds.min; // bottom-left corner (world position)
        maxLimit = bounds.max; // top-right corner (world position)
    }
    void Update()
    {
        if (Application.isMobilePlatform)
        {
            // Kiểm tra ngón tay chạm UI
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;

            HandleTouch();
        }
        else
        {
            // Kiểm tra chuột đang trên UI
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            HandleMouseDrag();
            HandleMouseZoom();
        }
        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;

        float halfW = camWidth / 2f;
        float halfH = camHeight / 2f;

        float clampedX = Mathf.Clamp(transform.position.x, minLimit.x + halfW, maxLimit.x - halfW);
        float clampedY = Mathf.Clamp(transform.position.y, minLimit.y + halfH, maxLimit.y - halfH);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }


    void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position += difference;
        }
    }

    void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            Camera.main.orthographicSize -= scroll * zoomSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                dragOrigin = Camera.main.ScreenToWorldPoint(touch.position);
                isDragging = true;
            }

            if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(touch.position);
                transform.position += difference;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
        else if (Input.touchCount == 2)
        {
            // Pinch zoom
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            Camera.main.orthographicSize -= difference * zoomSpeed * 0.01f;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
        }
    }
}
