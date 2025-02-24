using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShuttleOption : MonoBehaviour
{
    private float _shuttleMass = 0.0055f;
    private Rigidbody _shuttleRigidBody;

    private Vector3 _prevFrameVelocity = Vector3.zero;

    private int _shuttleBaundCount = 0;
    private bool _isFinishInitialization = false;

    //計算で使う諸定数を宣言
    const float AIRDENSITY = 1.226f;//空気の密度
    const float AIRKINEMATICVISCOSITY = 0.00001822f;//空気の動粘度
    float _skirtTipRadius = 0.033f;//半径
    Vector3 _currentFrameVelocity;//速度
    float _dragCoefficient = 0.56f;//抗力係数
    float _reynoldsNum = 0f;//レイノルズ数、代表長さは円の直径
    float _skirtDiameterDownRate = 0f;//スカート径の縮小率.グラフから目算
    float _skirtTipContractedRadius = 0f;//収縮後のスカート径
    float _parallelVelocityArea = 0f;//抗力に使う面積.速度方向に平行
    float _verticalVelocityArea = 0.00371f;//揚力に使う面積。速度方向に垂直
    float _innerProduct = 0f;//シャトルの軸ベクトルと速度ベクトルの内積計算

    float _angleOfAttack = 0f;
    float _clampedInnerProduct = 0f;
    float angleBetweenShaftAndVelocity = 0f;
    float _liftCoefficient = 0f;

    Vector3 _normFromShuttleToRight = Vector3.zero;
    Vector3 _liftacceleration = Vector3.zero;

    Vector3 _startVector = Vector3.zero;
    private float _startVelocity = 10f;
    private float _startTheta = 30f;
    private bool _isShoot = false;

    public void Init()
    {
        _shuttleRigidBody = GetComponent<Rigidbody>();
        _shuttleRigidBody.mass = _shuttleMass;
        _shuttleRigidBody.useGravity = false;
        _isFinishInitialization = true;
    }

    public void TestShoot()
    {
        //最初の加速の準備
        _startVector.x = 0f;
        _startVector.y = 1.0f * _startVelocity * Mathf.Sin(Mathf.PI / 180.0f * _startTheta);//質量の影響を考慮
        _startVector.z = -1.0f * _startVelocity * Mathf.Cos(Mathf.PI / 180.0f * _startTheta);
        _shuttleRigidBody.velocity = _startVector;
        _prevFrameVelocity = _startVector;//最初の速度ベクトルを方向用ベクトルとして保存

        _shuttleRigidBody.useGravity = true;
        _isShoot = true;
    }

    void FixedUpdate()
    {
        if(!_isShoot) return;
        if(!_isFinishInitialization) return;
        if(_shuttleBaundCount > 0) return;

        CalculateResistandLift(_shuttleRigidBody, _shuttleBaundCount, _prevFrameVelocity);
        TestShowShuttleVelocity(_shuttleRigidBody);
        _prevFrameVelocity = _shuttleRigidBody.velocity;
    }

    /// <summary>
    /// 地面と衝突したときの処理。現状カウントアップだけが必要
    /// 論文から読み取った距離と実測値を比較
    /// </summary>
    /// <param name="c"></param>
    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.tag == "Court")
        {
            _shuttleBaundCount += 1;
        }
    }

    /// <summary>
    /// シャトルの慣性抵抗と揚力を計算する
    /// </summary>
    /// <param name="shuttleRb">シャトルのRigidbody。速度や重力加速度を取得する</param>
    /// <param name="count">シャトルが落ちるときのカウント。シャトルが着地しない間は向きの修正を行う</param>
    /// <param name="prevFrameVelocity">前フレームの速度。迎角を計算とシャトルの向き修正を行うため使用する</param>
    /// <出力>
    /// rb.velocity : シャトルの速度ベクトル。慣性抵抗と揚力を考慮した速度に直す
    /// </summary>
    private void CalculateResistandLift(Rigidbody shuttleRb, int count, Vector3 prevFrameVelocity)
    {
        // 軌道に合わせてシャトルの向き（角度）を調整する
        //落ちた時にこの動作を切るようにする
        if (count == 0)
        {
            var diff = prevFrameVelocity.normalized;
            this.transform.rotation = Quaternion.FromToRotation(Vector3.up, diff);
        }

        _currentFrameVelocity = shuttleRb.velocity;//速度

        _reynoldsNum = AIRDENSITY * 2 * _skirtTipRadius * _currentFrameVelocity.magnitude / AIRKINEMATICVISCOSITY;//レイノルズ数、代表長さは円の直径
        _skirtDiameterDownRate = -0.021f * Mathf.Atan(0.02f * _currentFrameVelocity.magnitude);//スカート径の縮小率.グラフから目算
        _skirtTipContractedRadius = _skirtTipRadius * (1f + _skirtDiameterDownRate);//収縮後のスカート径
        _parallelVelocityArea = _skirtTipContractedRadius * _skirtTipContractedRadius * Mathf.PI;//抗力に使う面積.速度方向に平行
        _verticalVelocityArea = 0.00371f;//揚力に使う面積。速度方向に垂直

        //迎角の計算を行うための変数
        _innerProduct = Vector3.Dot(_currentFrameVelocity, prevFrameVelocity.normalized) / Mathf.Abs(_currentFrameVelocity.magnitude);//シャトルの軸ベクトルと速度ベクトルの内積計算

        _clampedInnerProduct = Mathf.Clamp(_innerProduct, -1.0f, 1.0f);//NaNの回避処理
        angleBetweenShaftAndVelocity = Mathf.Acos(_clampedInnerProduct);//シャトルの軸ベクトルと速度ベクトルの角度を出す

        _angleOfAttack = angleBetweenShaftAndVelocity / Mathf.Abs(_currentFrameVelocity.magnitude);//３次元の迎角


        
        //迎角に対応した慣性抵抗係数の計算
        if (_angleOfAttack >= 0)
        {
            _dragCoefficient = 0.1654583f * Mathf.Pow(_angleOfAttack, 6) - 1.57398385f * Mathf.Pow(_angleOfAttack, 5) + 5.58847377f * Mathf.Pow(_angleOfAttack, 4)
              - 9.09943781f * Mathf.Pow(_angleOfAttack, 3) + 6.67174337f * Mathf.Pow(_angleOfAttack, 2) - 1.57536991f * Mathf.Pow(_angleOfAttack, 1) + 0.58945876f;//カーブフィッティングから得た式
        }
        else if (_angleOfAttack == 0)
        {
            _dragCoefficient = 0.58945876f;
        }
        else
        {
            _dragCoefficient = 0.1654583f * Mathf.Pow(-_angleOfAttack, 6) - 1.57398385f * Mathf.Pow(-_angleOfAttack, 5) + 5.58847377f * Mathf.Pow(-_angleOfAttack, 4)
              - 9.09943781f * Mathf.Pow(-_angleOfAttack, 3) + 6.67174337f * Mathf.Pow(-_angleOfAttack, 2) - 1.57536991f * Mathf.Pow(-_angleOfAttack, 1) + 0.58945876f;//カーブフィッティングから得た式
        }
        //迎角に対応した揚力係数の計算
        _liftCoefficient = 0.77f * _angleOfAttack;

        //揚力ベクトルは速度の垂直方向
        _normFromShuttleToRight = Vector3.Cross(shuttleRb.velocity, this.transform.right).normalized;//加速度ベクトルに使う法線ベクトル

        //揚力の加速度計算
        _liftacceleration.x = _normFromShuttleToRight.x * 0.5f * (1f / shuttleRb.mass) * AIRDENSITY * _verticalVelocityArea * _liftCoefficient * shuttleRb.velocity.magnitude * shuttleRb.velocity.magnitude;
        _liftacceleration.y = _normFromShuttleToRight.y * 0.5f * (1f / shuttleRb.mass) * AIRDENSITY * _verticalVelocityArea * _liftCoefficient * shuttleRb.velocity.magnitude * shuttleRb.velocity.magnitude;
        _liftacceleration.z = _normFromShuttleToRight.z * 0.5f * (1f / shuttleRb.mass) * AIRDENSITY * _verticalVelocityArea * _liftCoefficient * shuttleRb.velocity.magnitude * shuttleRb.velocity.magnitude;
        
        Vector3 _resultDiffVelocity = new Vector3(0f, 0f, 0f);
        Vector3 _resultAcceleration = new Vector3(0f, 0f, 0f);
        
        //運動方程式より、慣性抵抗と空気抵抗の加速度を計算
        _resultAcceleration.x = -0.5f * (1f / shuttleRb.mass) * AIRDENSITY * _parallelVelocityArea * _dragCoefficient * shuttleRb.velocity.magnitude * shuttleRb.velocity.x + _liftacceleration.x;
        //ac.y = -9.81f -0.5f * (1f / rb.mass) * ro * s_d * c_d * rb.velocity.magnitude * rb.velocity.y;// rigidbodyのgravityを使用しない場合
        _resultAcceleration.y = -0.5f * (1f / shuttleRb.mass) * AIRDENSITY * _parallelVelocityArea * _dragCoefficient * shuttleRb.velocity.magnitude * shuttleRb.velocity.y + _liftacceleration.y;
        _resultAcceleration.z = -0.5f * (1f / shuttleRb.mass) * AIRDENSITY * _parallelVelocityArea * _dragCoefficient * shuttleRb.velocity.magnitude * shuttleRb.velocity.z + _liftacceleration.z;

        _resultDiffVelocity = _resultAcceleration * Time.fixedDeltaTime;//計算した加速度を積分して速度にする
        shuttleRb.velocity = shuttleRb.velocity + _resultDiffVelocity;//速度を変更
    }

    private void TestShowShuttleVelocity(Rigidbody shuttleRb)
    {
        //ベクトルをscene上で確認するため計算、デバッグ用
        Vector3 localpos = this.transform.localPosition;
        Vector3 forward = localpos + _currentFrameVelocity;
        Vector3 localVelocity = this.transform.InverseTransformVector(shuttleRb.velocity);
        Debug.DrawRay(localpos, shuttleRb.velocity, Color.red);
        Debug.DrawRay(localpos, this.transform.up, Color.green);
        Debug.DrawRay(localpos, _normFromShuttleToRight, Color.black);
        Debug.DrawRay(localpos, this.transform.right, Color.black);
    }
}
