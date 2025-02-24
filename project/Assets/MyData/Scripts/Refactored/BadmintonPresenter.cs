using System.Collections;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks;
using UnityEngine;

public class BadmintonPresenter : MonoBehaviour
{
    [SerializeField] ShuttleOption _shuttle;
    [SerializeField] RacketOption _racket;
    [SerializeField] ControllerButtonsMapper _mapper;
    private BadmintonModel _badModel;
    private ControllerModel _controllerModel;
    private BadmintonView _badView;
    // Start is called before the first frame update
    void Start()
    {
        _badModel = new BadmintonModel(_shuttle, _racket);
        _controllerModel = new ControllerModel(_mapper);
        _badView = FindObjectOfType<BadmintonView>();
        _badView.Init();

        _controllerModel.OnShootInvoked.AddListener(_shuttle.TestShoot);
        _controllerModel.OnResetInvoked.AddListener(_badModel.ResetScene);
        _controllerModel.OnMenuStateChanged.AddListener(_badView.SwitchCanvasState);
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            _shuttle.TestShoot();
        }
    }
}
