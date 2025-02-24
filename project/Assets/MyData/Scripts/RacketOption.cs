using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class RacketOption : MonoBehaviour
{
    private VelocityEstimator _velocityEstimator;
    private Vector3 _shotVelocity;// 両速度を基に計算した接球時の速度
    private Vector3 _resultShotVector;// 最終的なショット時のベクトル（方向×強さ）
    private float _racketMass = 0.085f;
    private float _shuttleMass  = 0.0055f;
    private bool _isFinishInitialization = false;
    private BoxCollider _racketCollider;
    private Vector3 _shotNorm;
    private bool _isAlreadyTouch = false;
    private float _shotStrongCoefficient = 5.0f;

    public void Init()
    {
        _racketCollider = GetComponent<BoxCollider>();
        _velocityEstimator = GetComponent<VelocityEstimator>();
        _isFinishInitialization = true;
    }

    /// <summary>
    /// 初期化終了後にコントローラーの速度を計算する
    /// </summary>
    private void Update()
    {
        if(!_isFinishInitialization) return;

        _shotVelocity = UpdateCalcurateSwingVelocity(_velocityEstimator);
        Debug.DrawRay(this.transform.localPosition, _shotVelocity, Color.red);
    }

    /// <summary>
    /// 衝突時、最終決定速度を用いてシャトルを法線方向へ飛ばす
    /// </summary>
    /// <param name="shuttleColision">シャトルのCollider</param>
    void OnCollisionEnter(Collision shuttleCollider)
    {
        if (_isAlreadyTouch != true)
        {
            if (shuttleCollider.gameObject.CompareTag("Shuttlecock"))
            {
                var rb_shuttle = shuttleCollider.gameObject.GetComponent<Rigidbody>();
                _racketCollider.enabled = false;
                foreach (ContactPoint contact in shuttleCollider.contacts)
                {
                    _shotNorm = contact.normal;//接触点の法線
                }
                //運動量から打たれた後のシャトルの速度を計算
                Vector3 v = ((_shuttleMass - _racketMass) * rb_shuttle.velocity + 2f * _racketMass * _shotVelocity) / (_shuttleMass + _racketMass);
                //接触点の法線ベクトル x 速さ
                _resultShotVector = -_shotNorm * _shotStrongCoefficient * v.magnitude;
                //決定した速度をシャトル速度に代入
                rb_shuttle.velocity = _resultShotVector;
                _isAlreadyTouch = true;
            }
        }
    }

    /// <summary>
    /// Updateのフレームで速度を計算して、回転と並進の両速度を加味した速度を計算
    /// </summary>
    /// <param name="velocityEstimator"></param>
    /// <returns></returns>
    private Vector3 UpdateCalcurateSwingVelocity(VelocityEstimator velocityEstimator)
    {
        var translatinalVelocity = velocityEstimator.GetVelocityEstimate();//ラケットの並進速度
        var angularVelocity = velocityEstimator.GetAngularVelocityEstimate();//ラケットの回転速度

        //ラケットの並進速度と手首による回転速度を決定
        //回転速度は手～スイートスポットまでの長さが約0.5mであるため0.5を乗算している
        return translatinalVelocity + angularVelocity * 0.5f;
    }

}
#region 過去試してうまくいかなかった構文等
// 速度を取得するための構文
// _translatinalVelocity = OVRInput.GetLocalControllerVelocity(activeController);
//_angularVelocity = OVRInput.GetLocalControllerAngularVelocity(activeController);
#endregion