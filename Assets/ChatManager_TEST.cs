using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;
using System.Diagnostics; // 🔹 say komutu için Process

public class ChatManager_TEST : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField inputField;
    public Transform chatContent;
    public GameObject messageBubblePrefab;
    public Button sendButton;
    public Button speakButton; // Mikrofon butonu
    public ScrollRect scrollRect;

    [Header("Character Animator")]
    public Animator characterAnimator;

    [Header("AI Voice")]
    public Button speakAIButton; // 🔹 Son AI cevabını seslendiren buton
    private string lastAIReply = ""; // 🔹 Son AI cevabı

    [Header("STT Settings")]
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;

    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClicked);
        speakButton.onClick.AddListener(OnSpeakButtonClicked);

        speakAIButton.onClick.AddListener(() => SpeakText(lastAIReply));
        speakAIButton.gameObject.SetActive(false); // Başlangıçta gizli
    }

    #region Text Chat
    void OnSendButtonClicked()
    {
        string userMessage = inputField.text;
        if (!string.IsNullOrEmpty(userMessage))
        {
            CreateBubble("You: " + userMessage, true);
            inputField.text = "";
            StartCoroutine(SendMessageToOpenRouter(userMessage));
        }
    }

    IEnumerator SendMessageToOpenRouter(string message)
    {
        string apiKey = ""; // OpenRouter API Key
        string json = "{\"model\": \"openai/gpt-3.5-turbo\",\"messages\": [{\"role\": \"user\",\"content\": \"" + message + "\"}]}";

        // AI yazıyor balonu
        GameObject typingBubble = Instantiate(messageBubblePrefab, chatContent);
        var typingText = typingBubble.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();
        typingText.text = "AI yazıyor...";
        typingText.color = new Color32(0xFD, 0xF0, 0xD5, 255);

        var typingImage = typingBubble.GetComponent<Image>();
        typingImage.color = new Color32(0x66, 0x9B, 0xBC, 255);

        LayoutRebuilder.ForceRebuildLayoutImmediate(typingBubble.GetComponent<RectTransform>());

        using (UnityWebRequest request = new UnityWebRequest("https://openrouter.ai/api/v1/chat/completions", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            string reply = "";
            if (request.result != UnityWebRequest.Result.Success)
                UnityEngine.Debug.LogError("Cevap alınamadı. Lütfen tekrar deneyin.");
            else
            {
                reply = ParseReply(request.downloadHandler.text);
                if (string.IsNullOrEmpty(reply))
                    reply = "Cevap alınamadı.";
            }

            Destroy(typingBubble);
            CreateBubble("AI: " + reply, false);

            lastAIReply = reply;
            speakAIButton.gameObject.SetActive(true);

            yield return new WaitForSeconds(3f);
        }
    }
    #endregion

    #region Voice Chat (Deepgram STT)
    void OnSpeakButtonClicked()
    {
        if (!isRecording)
        {
           

            microphoneDevice = Microphone.devices[0];
            recordedClip = Microphone.Start(microphoneDevice, false, 60, 16000);
            isRecording = true;
            speakButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Durdur";

            if (characterAnimator != null)
                characterAnimator.SetBool("isTalking", false);
        }
        else
        {
            Microphone.End(microphoneDevice);
            isRecording = false;
            speakButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Konuş";

            string path = Path.Combine(Application.persistentDataPath, "tempVoice.wav");
            SaveClipToWav(recordedClip, path);

            byte[] audioBytes = File.ReadAllBytes(path);
            StartCoroutine(SpeechToTextService.Instance.Transcribe(audioBytes, "voice.wav", (transcript) =>
            {
                inputField.text = transcript;
                inputField.caretPosition = transcript.Length;
            }));

            if (File.Exists(path))
                File.Delete(path);
        }
    }

    void SaveClipToWav(AudioClip clip, string filePath)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        using (var writer = new BinaryWriter(fileStream))
        {
            int sampleCount = samples.Length;
            int byteRate = 16000 * 2;

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(16000);
            writer.Write(byteRate);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(sampleCount * 2);

            foreach (var s in samples)
                writer.Write((short)(s * short.MaxValue));
        }
    }
    #endregion

    #region Bubble UI
    void CreateBubble(string msg, bool isUser)
    {
        GameObject bubble = Instantiate(messageBubblePrefab, chatContent);

        var text = bubble.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();
        text.text = msg;
        text.color = new Color32(0xFD, 0xF0, 0xD5, 255);

        var image = bubble.GetComponent<Image>();
        image.color = isUser ? new Color32(0x6F, 0x5E, 0x53, 255) : new Color32(0x66, 0x9B, 0xBC, 255);

        var timeTextObj = new GameObject("TimeText");
        timeTextObj.transform.SetParent(bubble.transform);
        var timeText = timeTextObj.AddComponent<TextMeshProUGUI>();
        timeText.text = System.DateTime.Now.ToString("HH:mm");
        timeText.fontSize = 27;
        timeText.color = new Color32(0xE8, 0xF1, 0xF2, 255);
        timeText.alignment = TextAlignmentOptions.BottomRight;

        var timeRect = timeText.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(1, 0);
        timeRect.anchorMax = new Vector2(1, 0);
        timeRect.pivot = new Vector2(1, 0);
        timeRect.anchoredPosition = new Vector2(-5, 5);

        RectTransform rect = bubble.GetComponent<RectTransform>();
        if (isUser)
        {
            rect.pivot = new Vector2(1, 1);
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
        }
        else
        {
            rect.pivot = new Vector2(0, 1);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        float padding = 20f;
        float maxWidth = 300f;
        float preferredWidth = Mathf.Min(text.preferredWidth + padding, maxWidth);
        float preferredHeight = text.preferredHeight + padding;
        rect.sizeDelta = new Vector2(preferredWidth, preferredHeight);

        float yOffset = 0;
        foreach (RectTransform child in chatContent)
            yOffset -= child.sizeDelta.y + 10;
        rect.anchoredPosition = new Vector2(0, yOffset);
    }
    #endregion

    #region AI Voice Function
    void SpeakText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if(characterAnimator != null)
            StartCoroutine(SpeakCoroutine(text));
    }

    IEnumerator SpeakCoroutine(string text)
    {
        characterAnimator.SetBool("isTalking", true);

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "/usr/bin/say";
        psi.Arguments = "\"" + text + "\"";
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        Process process = Process.Start(psi);
        float estimatedTime = Mathf.Max(1f, text.Split(' ').Length * 0.4f);
        yield return new WaitForSeconds(estimatedTime);

        characterAnimator.SetBool("isTalking", false);
    }
    #endregion

    string ParseReply(string json)
    {
        var wrapper = JsonUtility.FromJson<OpenAIResponseWrapper>(json);
        return wrapper.choices[0].message.content;
    }

    [System.Serializable] public class OpenAIResponseWrapper { public Choice[] choices; }
    [System.Serializable] public class Choice { public Message message; }
    [System.Serializable] public class Message { public string content; }
}
