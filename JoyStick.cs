using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoyStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region SINGLETON
    private static JoyStick _singleton;
    public static JoyStick Instance { get { return _singleton; } }

    private void Awake()
    {
        if (_singleton != null) Destroy(gameObject);
        _singleton = this;
    }

    #endregion

    #region JOYUI
    public RectTransform background = null;
    public RectTransform handle = null;
    public Canvas canvas;
    public float radious;
    #endregion

    public Vector2 Input = Vector2.zero;

    void Start()
    {
        radious = background.rect.width / 2.0f;
    }


    public void OnDrag(PointerEventData eventData)
    {
        Vector2 to = ((Vector2)eventData.position - (Vector2)background.position) / canvas.scaleFactor;
        if (to.magnitude > radious)
        {
            to = (Vector2)to / (to.magnitude / radious);
            //Input = to.normalized;
        }
        else
        {
            //Input = to;
        }
        handle.anchoredPosition = to;
        Input = to.normalized;

    }


    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        Input = Vector3.zero;
    }

}
