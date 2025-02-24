using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BadmintonView : MonoBehaviour
{
    private Canvas _canvas;

    public void Init()
    {
        _canvas = GetComponent<Canvas>();
    }

    public void SwitchCanvasState(bool isShow)
    {
        _canvas.enabled = isShow;
    }

}
