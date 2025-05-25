using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GeoMap.UI
{
    public class ConsoleSimulator : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private TMP_InputField inputField;

        [Header("Settings")] 
        [TextArea]
        [SerializeField] private string text;

        [SerializeField] private float timeForCharacter = 0.05f;
        [SerializeField] private float timeForSpace = 0.1f;
        [SerializeField] private float timeForNewline = 0.25f;

        private void Start()
        {
            // Setup the input field for console simulation
            SetupInputField();
            StartCoroutine(Typescript());
        }

        private void SetupInputField()
        {
            // Configure input field appearance
            inputField.caretWidth = 5;
            inputField.caretBlinkRate = 0.85f;
            
            // CRITICAL: Set to read-only to prevent user input but keep caret
            inputField.readOnly = true;
            inputField.interactable = true;
            
            // Enable multi-line if not already
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        }

        private void LateUpdate()
        {
            // Keep the input field selected to maintain caret visibility
            if (inputField != null && !inputField.isFocused)
            {
                inputField.ActivateInputField();
            }
        }

        private IEnumerator Typescript()
        {
            while (true)
            {
                // Clear and reset
                inputField.text = "";
                
                // Activate input field to show caret
                inputField.ActivateInputField();
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);

                // Type each character
                for (int i = 0; i < text.Length; i++)
                {
                    char currentChar = text[i];
                    inputField.text += currentChar;

                    // CRITICAL: Force caret to end and scroll
                    inputField.caretPosition = inputField.text.Length;
                    inputField.stringPosition = inputField.text.Length;
                    
                    // Force the input field to handle the caret position change
                    inputField.ForceLabelUpdate();
                    
                    // Wait one frame for UI to update
                    yield return null;
                    
                    // Force scroll to caret position - this is the key fix
                    if (inputField.textComponent != null)
                    {
                        // Calculate if we need to scroll
                        var textInfo = inputField.textComponent.textInfo;
                        if (textInfo.lineCount > 0)
                        {
                            int currentLine = textInfo.characterInfo[Mathf.Max(0, inputField.caretPosition - 1)].lineNumber;
                            
                            // Get the input field's viewport
                            RectTransform viewport = inputField.textViewport;
                            RectTransform textRect = inputField.textComponent.rectTransform;
                            
                            if (viewport != null && textRect != null)
                            {
                                // Calculate line height
                                float lineHeight = inputField.textComponent.fontSize * inputField.textComponent.lineSpacing;
                                
                                // Calculate how much we need to scroll
                                float viewportHeight = viewport.rect.height;
                                float totalTextHeight = textInfo.lineCount * lineHeight;
                                
                                if (totalTextHeight > viewportHeight)
                                {
                                    // Calculate the Y position to show the current line at the bottom
                                    float targetY = (currentLine + 1) * lineHeight - viewportHeight;
                                    
                                    // Apply the scroll by moving the text component
                                    Vector2 anchoredPos = textRect.anchoredPosition;
                                    anchoredPos.y = targetY;
                                    textRect.anchoredPosition = anchoredPos;
                                }
                            }
                        }
                    }

                    // Wait based on character type
                    float waitTime = currentChar switch
                    {
                        ' ' => timeForSpace,
                        '\n' => timeForNewline,
                        _ => timeForCharacter
                    };
                    
                    yield return new WaitForSeconds(waitTime);
                }

                // Brief pause before restarting
                yield return new WaitForSeconds(timeForNewline * 2);
            }
        }

        // Alternative method using TextMeshPro's built-in scrolling
        private void ScrollToBottom()
        {
            if (inputField.textComponent is TextMeshProUGUI tmpText)
            {
                // Force the text to update
                tmpText.ForceMeshUpdate();
                
                // Use TMP's scroll to line functionality
                var textInfo = tmpText.textInfo;
                if (textInfo.lineCount > 0)
                {
                    int lastLine = textInfo.lineCount - 1;
                    
                    // Calculate viewport
                    RectTransform viewport = inputField.textViewport;
                    if (viewport != null)
                    {
                        float viewportHeight = viewport.rect.height;
                        float lineHeight = tmpText.fontSize * tmpText.lineSpacing;
                        int visibleLines = Mathf.FloorToInt(viewportHeight / lineHeight);
                        
                        // Scroll to show the last line
                        if (lastLine >= visibleLines)
                        {
                            Vector2 anchoredPos = tmpText.rectTransform.anchoredPosition;
                            anchoredPos.y = (lastLine - visibleLines + 1) * lineHeight;
                            tmpText.rectTransform.anchoredPosition = anchoredPos;
                        }
                    }
                }
            }
        }

        // Public methods for external control
        public void SetConsoleText(string newText)
        {
            text = newText;
        }

        public void StopTyping()
        {
            StopAllCoroutines();
        }

        public void StartTyping()
        {
            StopAllCoroutines();
            StartCoroutine(Typescript());
        }
        
        // Force focus for caret visibility
        [ContextMenu("Force Focus")]
        public void ForceFocus()
        {
            inputField.ActivateInputField();
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
    }
}