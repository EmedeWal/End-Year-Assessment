using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    #region References

    [Header("Health Bar References")]
    [SerializeField] private Gradient gradient;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;

    [Header("Status References")]
    [SerializeField] private GameObject[] statusIcons;

    #endregion

    //

    #region Default

    private void Awake()
    {
        // Initially disable all status icons
        foreach (GameObject icon in statusIcons) icon.SetActive(false);
    }

    #endregion

    //

    #region Health Bar

    public void SetMaxHealth(float currentHealth, float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = currentHealth;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }


    public void SetCurrentHealth(float currentHealth)
    {
        slider.value = currentHealth;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    #endregion

    //

    #region Status Effect UI

    public void SetStatusIconActive(int position, bool active)
    {
        // Handle status icon logic
        statusIcons[position].SetActive(active);
    }

    #endregion

    //
}
