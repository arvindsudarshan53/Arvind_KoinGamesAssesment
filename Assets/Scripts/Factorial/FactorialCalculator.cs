using UnityEngine;
using TMPro;

public class FactorialCalculator : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text resultText;

    const int minInputVal = 1;
    const int maxInputVal = 18;

    public void CalculateFactorial()
    {
        string input = inputField.text;

        if (!int.TryParse(input, out int num))
        {
            resultText.text = "Please enter a valid number.";
            return;
        }

        if (num < minInputVal || num > maxInputVal)
        {
            resultText.text = $"Input must be between {minInputVal} and {maxInputVal}.";
            return;
        }

        long result = 1;
        for (int i = 1; i <= num; i++)
        {
            result *= i;
        }

        resultText.text = $"Factorial of {num} is {result}";
    }
}
