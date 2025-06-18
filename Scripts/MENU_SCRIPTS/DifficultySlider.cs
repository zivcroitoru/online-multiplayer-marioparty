using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultySliderTMP : MonoBehaviour
{
    public Slider difficultySlider;
    public TextMeshProUGUI difficultyText;

    void Start()
    {
        // Set slider properties to go from 5 to 30
        difficultySlider.minValue = 5;
        difficultySlider.maxValue = 30;

        // Set slider to whole numbers and ensure it moves in increments of 5
        difficultySlider.wholeNumbers = true;  // Restrict to whole numbers

        // Initialize the slider with the minimum value of 5
        difficultySlider.value = 5;

        // Initialize the text with the initial slider value
        UpdateDifficultyText(difficultySlider.value);

        // Add listener to update text and enforce step size when the slider value changes
        difficultySlider.onValueChanged.AddListener(UpdateDifficultyText);
    }

    // Method to update the text and enforce step size of 5
    void UpdateDifficultyText(float value)
    {
        // Ensure that the value is snapped to the nearest multiple of 5
        float steppedValue = Mathf.Round(value / 5) * 5;

        // Update the slider value to match the stepped value
        difficultySlider.SetValueWithoutNotify(steppedValue);  // Prevents re-triggering the listener

        // Update the TextMeshPro text to show the stepped value as "Rounds"
        difficultyText.text = "Rounds \n" + steppedValue.ToString("0");
    }
}
