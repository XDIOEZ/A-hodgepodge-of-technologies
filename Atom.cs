using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atom : MonoBehaviour
{
    #region Fields
    //这里挂接着玩家获取的所有电子 
    public List<GameObject> electrons = new List<GameObject>();

    //电子轨道参数
    [Header("轨道设置")]
    public float orbitRadius = 2f;        // 轨道半径
    public float orbitSpeed = 50f;        // 轨道速度

    //混沌运动参数
    [Header("混沌运动")]
    public float chaosStrength = 0.2f;    // 混沌强度
    public float chaosSpeed = 3f;         // 混沌频率

    //爆炸参数
    [Header("爆炸效果")]
    public float explodeRadiusMultiplier = 3f;  // 爆炸时半径倍数
    public float explodeSpeedMultiplier = 5f;   // 爆炸时速度倍数
    public float explodeChaosMultiplier = 8f;   // 爆炸时混沌倍数
    public float explodeDuration = 0.4f;        // 爆炸持续时间
    public float returnDuration = 1.5f;         // 返回持续时间
    public AnimationCurve explodeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 拖尾参数
    [Header("拖尾设置")]
    public Material trailMaterial;              // 拖尾材质
    public float trailTime = 0.5f;              // 拖尾持续时间
    public float trailWidth = 0.1f;             // 拖尾宽度
    public Gradient trailColor = new Gradient(); // 拖尾颜色渐变

    private Dictionary<GameObject, float> electronAngles = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> electronOffsets = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, TrailRenderer> electronTrails = new Dictionary<GameObject, TrailRenderer>();

    private float currentRadiusMultiplier = 1f;
    private float currentSpeedMultiplier = 1f;
    private float currentChaosMultiplier = 1f;

    private bool isExploding = false;
    private bool isReturning = false;
    private float effectTimer = 0f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeElectrons();
    }

    public void Update()
    {
        UpdateEffectParameters();
        UpdateElectronPositions();
        
        // 添加空格键检测以触发爆炸效果
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerExplode();
        }
    }
    #endregion

    #region Electron Management
    private void InitializeElectrons()
    {
        float angleStep = 360f / Mathf.Max(electrons.Count, 1);

        for (int i = 0; i < electrons.Count; i++)
        {
            if (electrons[i] != null)
            {
                electronAngles[electrons[i]] = angleStep * i;
                electronOffsets[electrons[i]] = Random.Range(0f, Mathf.PI * 2);
                AddTrailToElectron(electrons[i]); // 为电子添加拖尾
            }
        }
    }

    private void UpdateElectronPositions()
    {
        // 应用当前的参数倍数
        float activeRadius = orbitRadius * currentRadiusMultiplier;
        float activeSpeed = orbitSpeed * currentSpeedMultiplier;
        float activeChaos = chaosStrength * currentChaosMultiplier;

        for (int i = 0; i < electrons.Count; i++)
        {
            var electron = electrons[i];
            if (electron == null) continue;

            // 初始化新添加的电子
            if (!electronAngles.ContainsKey(electron))
            {
                electronAngles[electron] = Random.Range(0f, 360f);
                electronOffsets[electron] = Random.Range(0f, Mathf.PI * 2);
                AddTrailToElectron(electron); // 为新电子添加拖尾
            }

            // 更新角度(使用动态速度)
            electronAngles[electron] += activeSpeed * Time.deltaTime;

            // 计算基础轨道位置(使用动态半径)
            float angle = electronAngles[electron] * Mathf.Deg2Rad;
            Vector2 orbitPos = new Vector2(
                Mathf.Cos(angle) * activeRadius,
                Mathf.Sin(angle) * activeRadius
            );

            // 添加混沌运动(使用动态混沌强度)
            float chaosTime = Time.time * chaosSpeed + electronOffsets[electron];
            Vector2 chaos = new Vector2(
                Mathf.PerlinNoise(chaosTime, 0) - 0.5f,
                Mathf.PerlinNoise(0, chaosTime) - 0.5f
            ) * activeChaos;

            // 应用最终位置
            electron.transform.position = (Vector2)transform.position + orbitPos + chaos;
        }
    }

    public void AddElectron(GameObject electron)
    {
        if (!electrons.Contains(electron))
        {
            electrons.Add(electron);
            electronAngles[electron] = Random.Range(0f, 360f);
            electronOffsets[electron] = Random.Range(0f, Mathf.PI * 2);
            AddTrailToElectron(electron); // 为新增电子添加拖尾
        }
    }

    public void RemoveElectron(GameObject electron)
    {
        electrons.Remove(electron);
        electronAngles.Remove(electron);
        electronOffsets.Remove(electron);
        if (electronTrails.ContainsKey(electron))
        {
            electronTrails.Remove(electron);
        }
    }

    // 为电子添加拖尾效果的方法
    private void AddTrailToElectron(GameObject electron)
    {
        // 如果电子上已经有TrailRenderer组件，则使用它；否则添加一个新的
        TrailRenderer trail = electron.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = electron.AddComponent<TrailRenderer>();
        }

        // 配置拖尾参数
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.material = trailMaterial;
        trail.colorGradient = trailColor;
        trail.autodestruct = false;

        // 存储引用以便后续管理
        electronTrails[electron] = trail;
    }
    #endregion

    #region Explode Effect
    [ContextMenu("Trigger Explode")]
    public void TriggerExplode()
    {
        if (isExploding || isReturning) return;

        isExploding = true;
        effectTimer = 0f;
    }

    private void UpdateEffectParameters()
    {
        if (isExploding)
        {
            effectTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(effectTimer / explodeDuration);
            float curveValue = explodeCurve.Evaluate(progress);

            // 平滑插值到爆炸参数
            currentRadiusMultiplier = Mathf.Lerp(1f, explodeRadiusMultiplier, curveValue);
            currentSpeedMultiplier = Mathf.Lerp(1f, explodeSpeedMultiplier, curveValue);
            currentChaosMultiplier = Mathf.Lerp(1f, explodeChaosMultiplier, curveValue);

            if (progress >= 1f)
            {
                isExploding = false;
                isReturning = true;
                effectTimer = 0f;
            }
        }
        else if (isReturning)
        {
            effectTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(effectTimer / returnDuration);
            float curveValue = returnCurve.Evaluate(progress);

            // 平滑插值回正常参数
            currentRadiusMultiplier = Mathf.Lerp(explodeRadiusMultiplier, 1f, curveValue);
            currentSpeedMultiplier = Mathf.Lerp(explodeSpeedMultiplier, 1f, curveValue);
            currentChaosMultiplier = Mathf.Lerp(explodeChaosMultiplier, 1f, curveValue);

            if (progress >= 1f)
            {
                isReturning = false;
                // 确保完全恢复
                currentRadiusMultiplier = 1f;
                currentSpeedMultiplier = 1f;
                currentChaosMultiplier = 1f;
            }
        }
        else
        {
            // 确保在正常状态
            currentRadiusMultiplier = 1f;
            currentSpeedMultiplier = 1f;
            currentChaosMultiplier = 1f;
        }
    }
    #endregion
}