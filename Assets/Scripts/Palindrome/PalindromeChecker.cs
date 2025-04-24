using UnityEngine;
using TMPro;

public class PalindromeChecker : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text resultText;

    public void CheckPalindrome()
    {
        string input = inputField.text;

        if (string.IsNullOrWhiteSpace(input))
        {
            resultText.text = "Please enter a word or phrase.";
            return;
        }

        // remove spaces then convert to lowercase
        string cleaned = input.Replace(" ", "").ToLower();

        // Reverse the cleaned string
        char[] chars = cleaned.ToCharArray();
        System.Array.Reverse(chars);
        string reversed = new string(chars);

        // Check if it's a palindrome , if reversed and obtained string without spaces matches then it is palindrome
        bool isPalindrome = cleaned == reversed;
        resultText.text = isPalindrome ? "It's a palindrome!" : "Not a palindrome.";
    }
}
