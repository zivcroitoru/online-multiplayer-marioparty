using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultySliderTMP : MonoBehaviour
{
    public Slider difficultySlider; // Reference to the Slider
    public TextMeshProUGUI difficultyText; // Reference to the TextMeshPro text

    void Start()
    {
        // Set the default value
        difficultySlider.value = 5;

        // Update the text with the default value
        difficultyText.text = "Difficulty \n" + difficultySlider.value.ToString("0");

        // Add a listener to call the method when the slider value changes
        difficultySlider.onValueChanged.AddListener(UpdateDifficultyText);
    }

    // Method to update the text next to the slider
    void UpdateDifficultyText(float value)
    {
        difficultyText.text = "Difficulty \n" + value.ToString("0");
    }
}
