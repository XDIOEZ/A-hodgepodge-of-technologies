using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atom : MonoBehaviour
{
    #region �ֶζ���
    //����ҽ�����һ�ȡ�����е��� 
    public List<GameObject> electrons = new List<GameObject>();

    //���ӹ������
    [Header("�������")]
    public float orbitRadius = 2f;        // ����뾶
    public float orbitSpeed = 50f;        // ����ٶ�

    //�����˶�����
    [Header("�����˶�")]
    public float chaosStrength = 0.2f;    // ����ǿ��
    public float chaosSpeed = 3f;         // ����Ƶ��

    //��ը����
    [Header("��ըЧ��")]
    public float explodeRadiusMultiplier = 3f;  // ��ըʱ�뾶����
    public float explodeSpeedMultiplier = 5f;   // ��ըʱ�ٶȱ���
    public float explodeChaosMultiplier = 8f;   // ��ըʱ���籶��
    public float explodeDuration = 0.4f;        // ��ը����ʱ��
    public float returnDuration = 1.5f;         // ���س���ʱ��
    public AnimationCurve explodeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ��β����
    [Header("��β����")]
    public Material trailMaterial;              // ��β����
    public float trailTime = 0.5f;              // ��β����ʱ��
    public float trailWidth = 0.1f;             // ��β���
    public Gradient trailColor = new Gradient(); // ��β��ɫ����

    // ���Ӿ���
    [Header("�������")]
    public Sprite electronSprite;               // ���Ӿ���ͼƬ

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

    #region Unity��������
    private void Start()
    {
        InitializeElectrons();
    }

    public void Update()
    {
        UpdateEffectParameters();
        UpdateElectronPositions();
        
        // ��ӿո������Դ�����ըЧ��
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerExplode();
        }
    }
    #endregion

    #region ���ӹ���
    private void InitializeElectrons()
    {
        float angleStep = 360f / Mathf.Max(electrons.Count, 1);

        for (int i = 0; i < electrons.Count; i++)
        {
            if (electrons[i] != null)
            {
                electronAngles[electrons[i]] = angleStep * i;
                electronOffsets[electrons[i]] = Random.Range(0f, Mathf.PI * 2);
                AddTrailToElectron(electrons[i]); // Ϊ���������β
            }
        }
    }

    private void UpdateElectronPositions()
    {
        // Ӧ�õ�ǰ�Ĳ�������
        float activeRadius = orbitRadius * currentRadiusMultiplier;
        float activeSpeed = orbitSpeed * currentSpeedMultiplier;
        float activeChaos = chaosStrength * currentChaosMultiplier;

        for (int i = 0; i < electrons.Count; i++)
        {
            var electron = electrons[i];
            if (electron == null) 
            {
                // ���ֵ����Ƴ���Ч�ĵ�������
                if (electronAngles.ContainsKey(electron)) electronAngles.Remove(electron);
                if (electronOffsets.ContainsKey(electron)) electronOffsets.Remove(electron);
                if (electronTrails.ContainsKey(electron)) electronTrails.Remove(electron);
                continue;
            }

            // ��ʼ������ӵĵ���
            if (!electronAngles.ContainsKey(electron))
            {
                electronAngles[electron] = Random.Range(0f, 360f);
                electronOffsets[electron] = Random.Range(0f, Mathf.PI * 2);
                AddTrailToElectron(electron); // Ϊ�µ��������β
            }

            // ���½Ƕ�(ʹ�ö�̬�ٶ�)
            electronAngles[electron] += activeSpeed * Time.deltaTime;

            // ����������λ��(ʹ�ö�̬�뾶)
            float angle = electronAngles[electron] * Mathf.Deg2Rad;
            Vector2 orbitPos = new Vector2(
                Mathf.Cos(angle) * activeRadius,
                Mathf.Sin(angle) * activeRadius
            );

            // ��ӻ����˶�(ʹ�ö�̬����ǿ��)
            float chaosTime = Time.time * chaosSpeed + electronOffsets[electron];
            Vector2 chaos = new Vector2(
                Mathf.PerlinNoise(chaosTime, 0) - 0.5f,
                Mathf.PerlinNoise(0, chaosTime) - 0.5f
            ) * activeChaos;

            // Ӧ������λ��
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
            AddTrailToElectron(electron); // Ϊ�������������β
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

    // Ϊ���������βЧ���ķ���
    private void AddTrailToElectron(GameObject electron)
    {
        // ����������Ѿ���TrailRenderer�������ʹ�������������һ���µ�
        TrailRenderer trail = electron.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = electron.AddComponent<TrailRenderer>();
        }

        // ������β����
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.material = trailMaterial;
        trail.colorGradient = trailColor;
        trail.autodestruct = false;

        // �洢�����Ա��������
        electronTrails[electron] = trail;
        
        // ��������˵��Ӿ��飬��Ӧ�õ�������
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

    #region ��ըЧ��
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

            // ƽ����ֵ����ը����
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

            // ƽ����ֵ����������
            currentRadiusMultiplier = Mathf.Lerp(explodeRadiusMultiplier, 1f, curveValue);
            currentSpeedMultiplier = Mathf.Lerp(explodeSpeedMultiplier, 1f, curveValue);
            currentChaosMultiplier = Mathf.Lerp(explodeChaosMultiplier, 1f, curveValue);

            if (progress >= 1f)
            {
                isReturning = false;
                // ȷ����ȫ�ָ�
                currentRadiusMultiplier = 1f;
                currentSpeedMultiplier = 1f;
                currentChaosMultiplier = 1f;
            }
        }
        else
        {
            // ȷ��������״̬
            currentRadiusMultiplier = 1f;
            currentSpeedMultiplier = 1f;
            currentChaosMultiplier = 1f;
        }
    }
    #endregion

    #region ���Ӿ������
    /// <summary>
    /// �������е��ӵľ���
    /// </summary>
    /// <param name="newSprite">�µľ���</param>
    public void ChangeAllElectronsSprite(Sprite newSprite)
    {
        electronSprite = newSprite; // ����Ĭ�Ͼ���
        
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
    /// ����ָ�����ӵľ���
    /// </summary>
    /// <param name="electron">Ŀ����Ӷ���</param>
    /// <param name="newSprite">�µľ���</param>
    [Tooltip("����ָ�����ӵľ���")]
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

#region ���Ӵ��������
[ContextMenu("Create And Add Electron")]
[Tooltip("���������һ���µĵ��ӵ�ԭ�Ӻ���Χ")]
public void FastCreateElectron()
{
    CreateAndAddElectron();
}

/// <summary>
/// ���������һ���µĵ��ӵ�ԭ�Ӻ���Χ
/// </summary>
/// <param name="electronPrefab">����Ԥ���壨��ѡ����������ṩ�򴴽�Ĭ�ϵĵ��Ӷ���</param>
/// <returns>�´����ĵ��Ӷ���</returns>
[Tooltip("�Զ��崴�������һ���µĵ��ӵ�ԭ�Ӻ���Χ")]
public GameObject CreateAndAddElectron(GameObject electronPrefab = null)
{
    GameObject newElectron;
    
    // ����ṩ��Ԥ���壬��ʹ��Ԥ���崴������
    if (electronPrefab != null)
    {
        newElectron = Instantiate(electronPrefab, transform.position, Quaternion.identity, transform);
    }
    else
    {
        // ����Ĭ�ϵĵ��Ӷ���
        newElectron = new GameObject("Electron");
        newElectron.transform.SetParent(transform);
        newElectron.transform.position = transform.position;
        
        // ��ӱ�Ҫ�����
        SpriteRenderer sr = newElectron.AddComponent<SpriteRenderer>();
        if (electronSprite != null)
        {
            sr.sprite = electronSprite;
        }
        else
        {
            // ����Ĭ�Ͼ���
            Sprite defaultSprite = CreateDefaultElectronSprite();
            sr.sprite = defaultSprite;
        }
        
        // ����Ĭ�ϵ�sortingOrder��ȷ��������ʾ����ȷ�㼶
        sr.sortingOrder = 1;
    }
    
    // ˿������ӵ��ӵ��б��в������βЧ��
    AddElectronWithSmoothIntegration(newElectron);
    
    return newElectron;
}

/// <summary>
/// ˿������ӵ��Ӳ����¼������е��ӵ�λ��
/// </summary>
/// <param name="electron">Ҫ��ӵĵ���</param>
private void AddElectronWithSmoothIntegration(GameObject electron)
{
    // ����ӵ��ӵ��б�
    if (!electrons.Contains(electron))
    {
        electrons.Add(electron);
    }
    
    // Ϊ����ӵĵ��������βЧ��
    AddTrailToElectron(electron);
    
    // ���¼������е��ӵĽǶȷֲ�����֤���ȷֲ�
    RedistributeElectrons();
}

/// <summary>
/// ���·ֲ����е��ӵĽǶȣ�ʹ���Ǿ��ȷֲ�
/// </summary>
private void RedistributeElectrons()
{
    int electronCount = electrons.Count;
    float angleStep = 360f / Mathf.Max(electronCount, 1);
    
    // ������еĽǶȺ�ƫ������¼
    electronAngles.Clear();
    electronOffsets.Clear();
    
    // Ϊÿ�����ӷ����µĽǶȺ�ƫ����
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
/// ����Ĭ�ϵĵ��Ӿ���
/// </summary>
/// <returns>Ĭ�ϵ��Ӿ���</returns>
private Sprite CreateDefaultElectronSprite()
{
    // ����һ��Ĭ�ϵ�Բ�ξ���
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


    #region ������ԭ�ӵĴ��������
    /// <summary>
    /// ���������һ����ԭ�ӵ���ǰԭ����Χ
    /// </summary>
    /// <returns>�´�������ԭ�Ӷ���</returns>
    [Tooltip("���������һ����ԭ�ӵ���ǰԭ����Χ")]
    [ContextMenu("Create And Add SubAtom")]
    public GameObject CreateAndAddSubAtom()
    {
        // ������ԭ�Ӷ���
        GameObject subAtom = new GameObject("SubAtom");
        subAtom.transform.SetParent(transform);
        subAtom.transform.position = transform.position;

        // Ϊ��ԭ�����SpriteRenderer���
        SpriteRenderer sr = subAtom.AddComponent<SpriteRenderer>();
        if (electronSprite != null)
        {
            sr.sprite = electronSprite;
        }
        else
        {
            // ���û��ָ�����Ӿ��飬�򴴽�Ĭ�Ͼ���
            Sprite defaultSprite = CreateDefaultElectronSprite();
            sr.sprite = defaultSprite;
        }

        // ������Ⱦ�㼶��ȷ����ȷ��ʾ
        sr.sortingOrder = 1;

        // Ϊ��ԭ�����Atom���
        Atom subAtomComponent = subAtom.AddComponent<Atom>();

        // ���Ƶ�ǰԭ�ӵ�һЩ���ø���ԭ��
        subAtomComponent.orbitRadius = this.orbitRadius * 0.5f; // ��ԭ�ӹ���뾶��С
        subAtomComponent.orbitSpeed = this.orbitSpeed * 0.8f;   // ��ԭ����ת�ٶ�����
        subAtomComponent.chaosStrength = this.chaosStrength * 0.5f; // ��ԭ�ӻ����˶�����

        // ������β����
        subAtomComponent.trailMaterial = this.trailMaterial;
        subAtomComponent.trailTime = this.trailTime;
        subAtomComponent.trailWidth = this.trailWidth * 0.7f; // ��ԭ����β��ϸ
        subAtomComponent.trailColor = this.trailColor;

        // Ϊ��ԭ�������βЧ��
        AddTrailToSubAtom(subAtom);

        // ����ԭ����ӵ������б��У�ʹ����浱ǰԭ����ת
        AddElectronWithSmoothIntegration(subAtom);

        return subAtom;
    }




    /// <summary>
    /// Ϊ��ԭ�������βЧ��
    /// </summary>
    /// <param name="subAtom">��ԭ�Ӷ���</param>
    private void AddTrailToSubAtom(GameObject subAtom)
    {
        // Ϊ��ԭ�����TrailRenderer���
        TrailRenderer trail = subAtom.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = subAtom.AddComponent<TrailRenderer>();
        }

        // ������β����
        trail.time = trailTime;
        trail.startWidth = trailWidth * 0.7f; // ��ԭ����β��ϸ
        trail.endWidth = 0f;
        trail.material = trailMaterial;
        trail.colorGradient = trailColor;
        trail.autodestruct = false;
    }
    #endregion
}