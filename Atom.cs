using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atom : MonoBehaviour
{
    #region Fields
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
        
        // ��ӿո������Դ�����ըЧ��
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
            if (electron == null) continue;

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
}