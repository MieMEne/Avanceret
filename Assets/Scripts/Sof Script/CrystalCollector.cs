using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrystalCollector : MonoBehaviour
{
    public static CrystalCollector Instance { get; private set; }

    [Header("Crystal Count")]
    public int current = 0;
    public int target = 10;

    [Header("UI Elements")]
    public Slider progressBar;
    public TextMeshProUGUI counterText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = target;
            progressBar.value = current;
        }
        UpdateUI();
    }

    public void Add(int amount)
    {
        current = Mathf.Clamp(current + amount, 0, target);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (progressBar != null) progressBar.value = current;
        if (counterText != null) counterText.text = $"{current} / {target}";
    }
}
