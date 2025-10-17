using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // ����ʵ��
    private static UIManager _instance;
    public static UIManager Instance 
    { 
        get
        {
            // ���ʵ�������ڣ����Բ���
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                
                // ��������Ҳ���������һ���µ�
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("UIManager");
                    _instance = singletonObject.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }
    
    // �洢���������ֵ�
    private Dictionary<string, BasePanel> panels = new Dictionary<string, BasePanel>();
    
    // ���ĸ�����
    public Transform panelRoot;
    
    // Ԥ�������ã���ѡ��
    public GameObject[] panelPrefabs;

    private void Awake()
    {
        // ȷ��ֻ��һ��UIManagerʵ��
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // ��ʼ������ֵ�
        InitializePanels();
    }

    /// <summary>
    /// ��ʼ���������
    /// </summary>
    private void InitializePanels()
    {
        panels.Clear();
        
        // ���ҳ��������е�BasePanel���
        BasePanel[] allPanels = FindObjectsOfType<BasePanel>(true);
        foreach (BasePanel panel in allPanels)
        {
            if (!panels.ContainsKey(panel.name))
            {
                panels[panel.name] = panel;
            }
            else
            {
                // �������ͬ����壬��Ӿ���
                Debug.LogWarning($"Duplicate panel name found: {panel.name}");
            }
        }
    }

    /// <summary>
    /// ��ȡָ�����Ƶ����
    /// </summary>
    /// <param name="panelName">�������</param>
    /// <returns>BasePanel�������������ڷ���null</returns>
    public BasePanel GetPanel(string panelName)
    {
        if (panels.TryGetValue(panelName, out BasePanel panel))
        {
            return panel;
        }
        
        Debug.LogWarning($"Panel '{panelName}' not found!");
        return null;
    }

    /// <summary>
    /// ��ʾָ�����
    /// </summary>
    /// <param name="panelName">�������</param>
    public void ShowPanel(string panelName)
    {
        BasePanel panel = GetPanel(panelName);
        if (panel != null)
        {
            panel.Open();
        }
    }

    /// <summary>
    /// ����ָ�����
    /// </summary>
    /// <param name="panelName">�������</param>
    public void HidePanel(string panelName)
    {
        BasePanel panel = GetPanel(panelName);
        if (panel != null)
        {
            panel.Close();
        }
    }

    /// <summary>
    /// �л������ʾ״̬
    /// </summary>
    /// <param name="panelName">�������</param>
    public void TogglePanel(string panelName)
    {
        BasePanel panel = GetPanel(panelName);
        if (panel != null)
        {
            panel.Toggle();
        }
    }

    /// <summary>
    /// �������Ƿ��
    /// </summary>
    /// <param name="panelName">�������</param>
    /// <returns>����Ƿ��</returns>
    public bool IsPanelOpen(string panelName)
    {
        BasePanel panel = GetPanel(panelName);
        if (panel != null)
        {
            return panel.IsOpen();
        }
        return false;
    }

    /// <summary>
    /// �����������
    /// </summary>
    public void HideAllPanels()
    {
        foreach (var panel in panels.Values)
        {
            panel.Close();
        }
    }

    /// <summary>
    /// ��ʾ�������
    /// </summary>
    public void ShowAllPanels()
    {
        foreach (var panel in panels.Values)
        {
            panel.Open();
        }
    }

    /// <summary>
    /// ͨ��Ԥ���崴�������
    /// </summary>
    /// <param name="panelPrefabName">���Ԥ��������</param>
    /// <param name="parent">������</param>
    /// <returns>���������</returns>
    public BasePanel CreatePanel(string panelPrefabName, Transform parent = null)
    {
        // ����Ԥ����
        GameObject panelPrefab = null;
        foreach (GameObject prefab in panelPrefabs)
        {
            if (prefab != null && prefab.name == panelPrefabName)
            {
                panelPrefab = prefab;
                break;
            }
        }
        
        if (panelPrefab == null)
        {
            Debug.LogWarning($"Panel prefab '{panelPrefabName}' not found!");
            return null;
        }
        
        // �������ʵ��
        Transform parentTransform = parent != null ? parent : (panelRoot != null ? panelRoot : transform);
        GameObject panelInstance = Instantiate(panelPrefab, parentTransform);
        
        // ��ȡBasePanel���
        BasePanel panel = panelInstance.GetComponent<BasePanel>();
        if (panel != null)
        {
            // ��ӵ��ֵ���
            if (!panels.ContainsKey(panelInstance.name))
            {
                panels[panelInstance.name] = panel;
            }
            return panel;
        }
        else
        {
            Debug.LogWarning($"Panel prefab '{panelPrefabName}' does not have a BasePanel component!");
            Destroy(panelInstance);
            return null;
        }
    }

    /// <summary>
    /// ����ָ�����
    /// </summary>
    /// <param name="panelName">�������</param>
    public void DestroyPanel(string panelName)
    {
        if (panels.TryGetValue(panelName, out BasePanel panel))
        {
            panels.Remove(panelName);
            if (panel != null && panel.gameObject != null)
            {
                Destroy(panel.gameObject);
            }
        }
    }

    /// <summary>
    /// ˢ������б�����̬������ʱ���ã�
    /// </summary>
    public void RefreshPanels()
    {
        InitializePanels();
    }

    /// <summary>
    /// ��ȡ�����������
    /// </summary>
    /// <returns>��������б�</returns>
    public List<string> GetAllPanelNames()
    {
        return new List<string>(panels.Keys);
    }

    /// <summary>
    /// �������Ŀɼ���
    /// </summary>
    /// <param name="panelName">�������</param>
    /// <param name="isVisible">�Ƿ�ɼ�</param>
    public void SetPanelVisible(string panelName, bool isVisible)
    {
        BasePanel panel = GetPanel(panelName);
        if (panel != null)
        {
            if (isVisible)
            {
                panel.Open();
            }
            else
            {
                panel.Close();
            }
        }
    }
    
    /// <summary>
    /// ��ȡָ����ǩ������б�
    /// </summary>
    /// <param name="tag">��ǩ����</param>
    /// <returns>ƥ���ǩ������б�</returns>
    public List<BasePanel> GetPanelsByTag(string tag)
    {
        List<BasePanel> taggedPanels = new List<BasePanel>();
        
        foreach (var panel in panels.Values)
        {
            if (panel != null && panel.gameObject.CompareTag(tag))
            {
                taggedPanels.Add(panel);
            }
        }
        
        return taggedPanels;
    }
    
    /// <summary>
    /// ��ʾָ����ǩ���������
    /// </summary>
    /// <param name="tag">��ǩ����</param>
    public void ShowPanelsByTag(string tag)
    {
        List<BasePanel> taggedPanels = GetPanelsByTag(tag);
        foreach (BasePanel panel in taggedPanels)
        {
            panel.Open();
        }
    }
    
    /// <summary>
    /// ����ָ����ǩ���������
    /// </summary>
    /// <param name="tag">��ǩ����</param>
    public void HidePanelsByTag(string tag)
    {
        List<BasePanel> taggedPanels = GetPanelsByTag(tag);
        foreach (BasePanel panel in taggedPanels)
        {
            panel.Close();
        }
    }
    
    /// <summary>
    /// ע����嵽UIManager
    /// </summary>
    /// <param name="panel">Ҫע������</param>
    public void RegisterPanel(BasePanel panel)
    {
        if (panel != null && !panels.ContainsKey(panel.name))
        {
            panels[panel.name] = panel;
        }
        else if (panel != null && panels.ContainsKey(panel.name))
        {
            Debug.LogWarning($"Panel '{panel.name}' is already registered!");
        }
    }
}