using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallShoot_DL_modi_copy : MonoBehaviour
{
    //スタートの初期値
    public float theta, vel = 0f;
    public float k = 1f;
    //rigidbodyの取得
    Rigidbody rb;
    //距離計測用の変数
    Vector3 pos1, pos2 = new Vector3(0, 0, 0);
    int count = 0;

    float m = 0f;//質量取得用

    Vector3 s_force = new Vector3(0, 0, 0);
    //速度保存用

    Vector3 v, v_before;
    //距離計算用
    float dis, dis2 = 0f;
    //シャトルが地面についたか判定するフラグ
    bool s_flag = false;

    // コンポーネントの取得
    private void Awake()
    {
        m = GetComponent<Rigidbody>().mass;
        rb = GetComponent<Rigidbody>();
        pos1 = this.transform.position;
    }

    /// <関数概要>
    /// シャトルをスタートさせるため初速度を加える
    /// </summary>
    private void Start()
    {
        //最初の加速の準備
        s_force.x = 0f;
        s_force.y = 1.0f * vel * Mathf.Sin(Mathf.PI / 180.0f * theta);//質量の影響も加えてみた
        s_force.z = -1.0f * vel * Mathf.Cos(Mathf.PI / 180.0f * theta);
        //rb.velocity = s_force;
        v_before = s_force;//最初の速度ベクトルを方向用ベクトルとして保存
        rb.velocity = Vector3.zero;
        rb.useGravity = false;
    }
    /// <関数概要>
    /// シャトルが地面につくまで、シャトルの空気抵抗の計算を行う
    /// </summary>
    void FixedUpdate()
    {
        if (count == 0 && s_flag == true)
        {
            ResistandLift(rb, count, v_before);
            v_before = rb.velocity;
        }
    }

    /// <関数概要>
    /// シャトルの慣性抵抗と揚力を計算する
    /// <入力>
    ///       rb : シャトルのRigidbody。速度や重力加速度を取得する
    ///    count : シャトルが落ちるときのカウント。シャトルが着地しない間は向きの修正を行う
    /// v_before : 前フレームの速度。迎角を計算とシャトルの向き修正を行うため使用する
    /// <出力>
    /// rb.velocity : シャトルの速度ベクトル。慣性抵抗と揚力を考慮した速度に直す
    /// </summary>
    void ResistandLift(Rigidbody rb, int count, Vector3 v_before)
    {
        //計算で使う諸定数を宣言
        const float ro = 1.226f;//空気の密度
        float r = 0.033f;//半径
        const float eta = 0.00001822f;//空気の動粘度
        v = rb.velocity;//速度
        float vl = rb.velocity.magnitude;//速さ
        float vx = rb.velocity.x;//x成分
        float vy = rb.velocity.y;//y成分
        float vz = rb.velocity.z;//z成分

        float c_d = 0.56f;//抗力係数。レイノルズ数に比例。バドは定数でいい
        float re = ro * 2 * r * vl / eta;//レイノルズ数、代表長さは円の直径
        float delta = -0.021f * Mathf.Atan(0.02f * rb.velocity.magnitude);//スカート径の縮小率.グラフから目算
        float rr = r * (1f + delta);//収縮後のスカート径
        float s_d = rr * rr * Mathf.PI;//抗力に使う面積.速度方向に平行
        float s_l = 0.00371f;//揚力に使う面積。速度方向に垂直

        // 軌道に合わせてシャトルの向き（角度）を調整する
        //落ちた時にこの動作を切るようにする
        if (count == 0)
        {
            var diff = v_before.normalized;
            this.transform.rotation = Quaternion.FromToRotation(Vector3.up, diff);
        }

        //迎角の計算を行うための変数
        float dot = Vector3.Dot(rb.velocity, v_before.normalized) / Mathf.Abs(rb.velocity.magnitude);//シャトルの軸ベクトルと速度ベクトルの内積計算
        float d = Mathf.Clamp(dot, -1.0f, 1.0f);//NaNの回避処理
        float up = Mathf.Acos(d);//シャトルの軸ベクトルと速度ベクトルの角度を出す

        float aoa = up / Mathf.Abs(rb.velocity.magnitude);//３次元の迎角


        //迎角に対応した慣性抵抗係数の計算
        if (aoa >= 0)
        {
            c_d = 0.1654583f * Mathf.Pow(aoa, 6) - 1.57398385f * Mathf.Pow(aoa, 5) + 5.58847377f * Mathf.Pow(aoa, 4)
              - 9.09943781f * Mathf.Pow(aoa, 3) + 6.67174337f * Mathf.Pow(aoa, 2) - 1.57536991f * Mathf.Pow(aoa, 1) + 0.58945876f;//カーブフィッティングから得た式
        }
        else if (aoa == 0)
        {
            c_d = 0.58945876f;
        }
        else
        {
            c_d = 0.1654583f * Mathf.Pow(-aoa, 6) - 1.57398385f * Mathf.Pow(-aoa, 5) + 5.58847377f * Mathf.Pow(-aoa, 4)
              - 9.09943781f * Mathf.Pow(-aoa, 3) + 6.67174337f * Mathf.Pow(-aoa, 2) - 1.57536991f * Mathf.Pow(-aoa, 1) + 0.58945876f;//カーブフィッティングから得た式
        }
        //迎角に対応した揚力係数の計算
        float c_l = 0.77f * aoa;

        //揚力ベクトルは速度の垂直方向
        Vector3 L_before = Vector3.Cross(rb.velocity, this.transform.right).normalized;//加速度ベクトルに使う法線ベクトル

        //揚力の加速度計算
        Vector3 L_ac = new Vector3(0f, 0f, 0f);
        L_ac.x = L_before.x * 0.5f * (1f / rb.mass) * ro * s_l * c_l * rb.velocity.magnitude * rb.velocity.magnitude;
        L_ac.y = L_before.y * 0.5f * (1f / rb.mass) * ro * s_l * c_l * rb.velocity.magnitude * rb.velocity.magnitude;
        L_ac.z = L_before.z * 0.5f * (1f / rb.mass) * ro * s_l * c_l * rb.velocity.magnitude * rb.velocity.magnitude;


        Vector3 mod = new Vector3(0f, 0f, 0f);
        Vector3 n = v_before.normalized;
        Vector3 ac = new Vector3(0f, 0f, 0f);

        //運動方程式より、慣性抵抗と空気抵抗の加速度を計算
        ac.x = -0.5f * (1f / rb.mass) * ro * s_d * c_d * rb.velocity.magnitude * rb.velocity.x + L_ac.x;
        //ac.y = -9.81f -0.5f * (1f / rb.mass) * ro * s_d * c_d * rb.velocity.magnitude * rb.velocity.y;
        ac.y = -0.5f * (1f / rb.mass) * ro * s_d * c_d * rb.velocity.magnitude * rb.velocity.y + L_ac.y;
        ac.z = -0.5f * (1f / rb.mass) * ro * s_d * c_d * rb.velocity.magnitude * rb.velocity.z + L_ac.z;


        mod = ac * Time.fixedDeltaTime;//計算した加速度を積分して速度にする
        rb.velocity = rb.velocity + mod;//速度を変更


        //ベクトルをscene上で確認するため計算、デバッグ用
        Vector3 localpos = this.transform.localPosition;
        Vector3 forward = localpos + v;
        Vector3 localVelocity = this.transform.InverseTransformVector(rb.velocity);
        Debug.DrawRay(localpos, rb.velocity, Color.red);
        Debug.DrawRay(localpos, this.transform.up, Color.green);
        Debug.DrawRay(localpos, L_before, Color.black);
        Debug.DrawRay(localpos, this.transform.right, Color.black);
    }

    //スタートしてから落下するまでの距離を測る関数
    //論文から読み取った距離と実測値を比較するためにこの値をとった
    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.tag == "Court")
        {
            pos2 = this.transform.position;
            dis = (pos2 - pos1).magnitude;
            dis2 = (pos2.z - pos1.z);
            //Debug.Log("移動距離:" + dis);
            //rb.velocity = Vector3.zero;

            if (count < 2)
            {
                //Debug.Log("移動距離2:" + dis2);
                float t = Time.time;
                //Debug.Log("時間 :" + t);
            }
            count += 1;
        }

    }
    public void Shoot()
    {
        s_flag = true;
        rb.useGravity = true;
        rb.velocity = s_force;
    }
}
