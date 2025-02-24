using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BadmintonModel
{
    private ShuttleOption _shuttle;
    private RacketOption _racket;

    public BadmintonModel(ShuttleOption shuttle, RacketOption racket)
    {
        _shuttle = shuttle;
        _racket = racket;

        _shuttle.Init();
        _racket.Init();        
    }
    
    public void ResetScene()
    {
        SceneManager.LoadScene(0);//シーンをリセットする
    }

}
