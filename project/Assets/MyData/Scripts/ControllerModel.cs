using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.BuildingBlocks;
using UnityEngine.Events;

public class ControllerModel
{
    private ControllerButtonsMapper _mapper;

    private bool _isShowMenu = true;
    private UnityEvent<bool> _onMenuStateChanged = new UnityEvent<bool>();
    private UnityEvent _onShootInvoked = new UnityEvent();
    private UnityEvent _onResetInvoked = new UnityEvent();

#region 公開プロパティ
    public UnityEvent<bool> OnMenuStateChanged{ get => _onMenuStateChanged;}
    public UnityEvent OnShootInvoked{ get => _onShootInvoked; }
    public UnityEvent OnResetInvoked{ get => _onResetInvoked; }
#endregion

    public ControllerModel(ControllerButtonsMapper mapper)
    {
        _mapper = mapper;

        // 右コントローラーAボタン
        AddOVRInputEvent(
            _mapper,
            "ShowMenu",
            OVRInput.Button.One,
            ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            ShowMenu
        );

        // 右の人差し指、マーカー位置合わせ
        AddOVRInputEvent(
            _mapper,
            "RegistMarker",
            OVRInput.Button.SecondaryIndexTrigger,
            ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            () => _onShootInvoked?.Invoke()
            );
        // 左の人差し指、マーカー位置合わせ
        AddOVRInputEvent(
            _mapper,
            "RegistMarker",
            OVRInput.Button.PrimaryIndexTrigger,
            ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown,
            () =>  _onResetInvoked?.Invoke()
            );
    }



    private void ShowMenu()
    {
        _isShowMenu = !_isShowMenu;
        _onMenuStateChanged?.Invoke(_isShowMenu);
    }

    /// <summary>
    /// Questで使用するコントローラーへのメソッドのマッピングを実施
    /// https://developers.meta.com/horizon/documentation/unity/unity-ovrinput/?locale=ja_JP
    /// </summary>
    /// <param name="mapper">ボタンマッパー</param>
    /// <param name="title">アクションの名前</param>
    /// <param name="buttonType">入力するボタン</param>
    /// <param name="buttonMode">ボタンの押し方</param>
    /// <param name="call">実際のメソッド</param>
    private void AddOVRInputEvent(
        ControllerButtonsMapper mapper,
        string title,
        OVRInput.Button buttonType,
        ControllerButtonsMapper.ButtonClickAction.ButtonClickMode buttonMode,
        UnityAction call
        )
    {
        var action = new ControllerButtonsMapper.ButtonClickAction();
        action.Title = title;
        action.Button = buttonType;// Primary: 左手、Secondary: 右手
        action.ButtonMode = buttonMode;
        var uEvent = new UnityEvent();
        uEvent.AddListener(call);
        action.Callback = uEvent;

        mapper.ButtonClickActions.Add(action);
    }
}
