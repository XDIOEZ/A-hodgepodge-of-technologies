using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atom : MonoBehaviour
{
    #region 字段定义
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

    // 电子精灵
    [Header("电子外观")]
    public Sprite electronSprite;               // 电子精灵图片

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

    #region Unity生命周期
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

    #region 电子管理
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
            if (electron == null) 
            {
                // 从字典中移除无效的电子引用
                if (electronAngles.ContainsKey(electron)) electronAngles.Remove(electron);
                if (electronOffsets.ContainsKey(electron)) electronOffsets.Remove(electron);
                if (electronTrails.ContainsKey(electron)) electronTrails.Remove(electron);
                continue;
            }

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
        
        // 如果设置了电子精灵，则应用到电子上
        if (electronSprite != null)
        {
            SpriteRenderer sr = electron.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = electronSprite;
            }
        }
    }
    #endregion

    #region 爆炸效果
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

    #region 电子精灵管理
    /// <summary>
    /// 更换所有电子的精灵
    /// </summary>
    /// <param name="newSprite">新的精灵</param>
    public void ChangeAllElectronsSprite(Sprite newSprite)
    {
        electronSprite = newSprite; // 更新默认精灵
        
        foreach (var electron in electrons)
        {
            if (electron != null)
            {
                SpriteRenderer sr = electron.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = newSprite;
                }
            }
        }
    }

    /// <summary>
    /// 更换指定电子的精灵
    /// </summary>
    /// <param name="electron">目标电子对象</param>
    /// <param name="newSprite">新的精灵</param>
    [Tooltip("更换指定电子的精灵")]
    public void ChangeSpecificElectronSprite(GameObject electron, Sprite newSprite)
    {
        if (electron != null && electrons.Contains(electron))
        {
            SpriteRenderer sr = electron.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = newSprite;
            }
        }
    }
    #endregion

#region 电子创建与添加
[ContextMenu("Create And Add Electron")]
[Tooltip("创建并添加一个新的电子到原子核周围")]
public void FastCreateElectron()
{
    CreateAndAddElectron();
}

/// <summary>
/// 创建并添加一个新的电子到原子核周围
/// </summary>
/// <param name="electronPrefab">电子预制体（可选），如果不提供则创建默认的电子对象</param>
/// <returns>新创建的电子对象</returns>
[Tooltip("自定义创建并添加一个新的电子到原子核周围")]
public GameObject CreateAndAddElectron(GameObject electronPrefab = null)
{
    GameObject newElectron;
    
    // 如果提供了预制体，则使用预制体创建电子
    if (electronPrefab != null)
    {
        newElectron = Instantiate(electronPrefab, transform.position, Quaternion.identity, transform);
    }
    else
    {
        // 创建默认的电子对象
        newElectron = new GameObject("Electron");
        newElectron.transform.SetParent(transform);
        newElectron.transform.position = transform.position;
        
        // 添加必要的组件
        SpriteRenderer sr = newElectron.AddComponent<SpriteRenderer>();
        if (electronSprite != null)
        {
            sr.sprite = electronSprite;
        }
        else
        {
            // 创建默认精灵
            Sprite defaultSprite = CreateDefaultElectronSprite();
            sr.sprite = defaultSprite;
        }
        
        // 设置默认的sortingOrder，确保电子显示在正确层级
        sr.sortingOrder = 1;
    }
    
    // 丝滑地添加电子到列表中并添加拖尾效果
    AddElectronWithSmoothIntegration(newElectron);
    
    return newElectron;
}

/// <summary>
/// 丝滑地添加电子并重新计算所有电子的位置
/// </summary>
/// <param name="electron">要添加的电子</param>
private void AddElectronWithSmoothIntegration(GameObject electron)
{
    // 先添加电子到列表
    if (!electrons.Contains(electron))
    {
        electrons.Add(electron);
    }
    
    // 为新添加的电子添加拖尾效果
    AddTrailToElectron(electron);
    
    // 重新计算所有电子的角度分布，保证均匀分布
    RedistributeElectrons();
}

/// <summary>
/// 重新分布所有电子的角度，使它们均匀分布
/// </summary>
private void RedistributeElectrons()
{
    int electronCount = electrons.Count;
    float angleStep = 360f / Mathf.Max(electronCount, 1);
    
    // 清空现有的角度和偏移量记录
    electronAngles.Clear();
    electronOffsets.Clear();
    
    // 为每个电子分配新的角度和偏移量
    for (int i = 0; i < electronCount; i++)
    {
        if (electrons[i] != null)
        {
            electronAngles[electrons[i]] = angleStep * i;
            electronOffsets[electrons[i]] = Random.Range(0f, Mathf.PI * 2);
        }
    }
}

/// <summary>
/// 创建默认的电子精灵
/// </summary>
/// <returns>默认电子精灵</returns>
private Sprite CreateDefaultElectronSprite()
{
    // 创建一个默认的圆形精灵
    Texture2D circleTexture = new Texture2D(32, 32);
    Color[] colors = new Color[32 * 32];
    for (int y = 0; y < 32; y++)
    {
        for (int x = 0; x < 32; x++)
        {
            Vector2 center = new Vector2(16, 16);
            float distance = Vector2.Distance(new Vector2(x, y), center);
            colors[y * 32 + x] = distance <= 16 ? Color.white : Color.clear;
        }
    }
    circleTexture.SetPixels(colors);
    circleTexture.Apply();
    
    Sprite defaultSprite = Sprite.Create(circleTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    return defaultSprite;
}
    #endregion


    #region 附属子原子的创建与添加
    /// <summary>
    /// 创建并添加一个子原子到当前原子周围
    /// </summary>
    /// <returns>新创建的子原子对象</returns>
    [Tooltip("创建并添加一个子原子到当前原子周围")]
    [ContextMenu("Create And Add SubAtom")]
    public GameObject CreateAndAddSubAtom()
    {
        // 创建子原子对象
        GameObject subAtom = new GameObject("SubAtom");
        subAtom.transform.SetParent(transform);
        subAtom.transform.position = transform.position;

        // 为子原子添加SpriteRenderer组件
        SpriteRenderer sr = subAtom.AddComponent<SpriteRenderer>();
        if (electronSprite != null)
        {
            sr.sprite = electronSprite;
        }
        else
        {
            // 如果没有指定电子精灵，则创建默认精灵
            Sprite defaultSprite = CreateDefaultElectronSprite();
            sr.sprite = defaultSprite;
        }

        // 设置渲染层级，确保正确显示
        sr.sortingOrder = 1;

        // 为子原子添加Atom组件
        Atom subAtomComponent = subAtom.AddComponent<Atom>();

        // 复制当前原子的一些设置给子原子
        subAtomComponent.orbitRadius = this.orbitRadius * 0.5f; // 子原子轨道半径稍小
        subAtomComponent.orbitSpeed = this.orbitSpeed * 0.8f;   // 子原子旋转速度稍慢
        subAtomComponent.chaosStrength = this.chaosStrength * 0.5f; // 子原子混沌运动较弱

        // 复制拖尾设置
        subAtomComponent.trailMaterial = this.trailMaterial;
        subAtomComponent.trailTime = this.trailTime;
        subAtomComponent.trailWidth = this.trailWidth * 0.7f; // 子原子拖尾稍细
        subAtomComponent.trailColor = this.trailColor;

        // 为子原子添加拖尾效果
        AddTrailToSubAtom(subAtom);

        // 将子原子添加到电子列表中，使其跟随当前原子旋转
        AddElectronWithSmoothIntegration(subAtom);

        return subAtom;
    }




    /// <summary>
    /// 为子原子添加拖尾效果
    /// </summary>
    /// <param name="subAtom">子原子对象</param>
    private void AddTrailToSubAtom(GameObject subAtom)
    {
        // 为子原子添加TrailRenderer组件
        TrailRenderer trail = subAtom.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = subAtom.AddComponent<TrailRenderer>();
        }

        // 配置拖尾参数
        trail.time = trailTime;
        trail.startWidth = trailWidth * 0.7f; // 子原子拖尾稍细
        trail.endWidth = 0f;
        trail.material = trailMaterial;
        trail.colorGradient = trailColor;
        trail.autodestruct = false;
    }
    #endregion
}