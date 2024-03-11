using UnityEngine;
using UnityEngine.UI;

public class Souls : MonoBehaviour
{
    #region References

    [Header("References")]

    [SerializeField] private Gradient gradient;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;

    private PlayerController player;

    #endregion

    //

    #region Variables

    [Header("Variables")]
    [SerializeField] private int maxSouls;
    [SerializeField] private int startingSouls;

    [HideInInspector] public int currentSouls;

    #endregion

    //

    #region General

    private void Awake()
    {
        // Get relevant components
        player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        // Initiliase the soul settings
        currentSouls = startingSouls;

        SetMaxSouls();
    }

    #endregion

    //

    #region UI

    private void SetMaxSouls()
    {
        slider.maxValue = maxSouls;
        slider.value = currentSouls;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    private void SetCurrentSouls()
    {
        slider.value = currentSouls;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }

    #endregion

    //

    #region Modifications

    public void GainSouls(int amount)
    {
        // Modify souls and handle out of bounds input
        currentSouls += amount;
        if (currentSouls > maxSouls) currentSouls = maxSouls;

        // Update the UI
        SetCurrentSouls();
    }

    public void SpendSouls(int amount)
    {
        // Modify souls and handle out of bounds input
        currentSouls -= amount;
        if (currentSouls <= 0) currentSouls = 0;

        // Update the UI
        SetCurrentSouls();
    }

    public int GetCharges()
    {
        // Get the increments of 20 from your souls and return it
        int charges = currentSouls / 20;

        return charges;
    }

    #endregion 

    //
}
