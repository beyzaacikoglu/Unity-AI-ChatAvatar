
using System.Collections;
 using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatManager_TEST : MonoBehaviour
{
    public TMP_InputField inputField;
    public Transform chatContent;
    public GameObject messageBubblePrefab;
    public Button sendButton;
    public ScrollRect scrollRect;   

    // 🔹 Animator referansı eklendi
    public Animator characterAnimator;

    void Start()
    {
        sendButton.onClick.AddListener(OnSendButtonClicked);
    }

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
    string apiKey = "";  
    string json = "{\"model\": \"openai/gpt-3.5-turbo\",\"messages\": [{\"role\": \"user\",\"content\": \"" + message + "\"}]}";

    if (characterAnimator != null)
        characterAnimator.SetBool("isTalking", false);

    // 🔹 "AI yazıyor..." sahte bubble
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
        request.SetRequestHeader("Content-Type","application/json");
        request.SetRequestHeader("Authorization","Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (characterAnimator != null)
            characterAnimator.SetBool("isTalking", true);

        string reply = "";
        if (request.result != UnityWebRequest.Result.Success)
        {
            reply = "Cevap alınamadı. Lütfen tekrar deneyin.";
        }
        else
        {
            reply = ParseReply(request.downloadHandler.text);
            if (string.IsNullOrEmpty(reply))
                reply = "Cevap alınamadı.";
        }

        // 🔹 Önce "AI yazıyor..." balonunu sil
        Destroy(typingBubble);

        // 🔹 Gerçek cevabı ekle
        CreateBubble("AI: " + reply, false);

        yield return new WaitForSeconds(3f);
        if (characterAnimator != null)
            characterAnimator.SetBool("isTalking", false);
    }
}


  

  void CreateBubble(string msg, bool isUser)
{
    GameObject bubble = Instantiate(messageBubblePrefab, chatContent);

    // MessageText component’ini bul
    var text = bubble.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();
    text.text = msg;

    // Yazı rengi
text.color = new Color32(0xFD, 0xF0, 0xD5, 255); // FDF0D5

// Balon arka plan ve çerçeve
var image = bubble.GetComponent<Image>();
        if (isUser)
        {
            image.color = new Color32(0x6F, 0x5E, 0x53, 255); // User arka plan
            var outline = bubble.GetComponent<Outline>() ?? bubble.AddComponent<Outline>();

            


        }
        else
        {
            image.color = new Color32(0x66, 0x9B, 0xBC, 255); // AI arka plan
            var outline = bubble.GetComponent<Outline>() ?? bubble.AddComponent<Outline>();
         

        }

    // 🔹 Saat ekleme
    var timeTextObj = new GameObject("TimeText");
    timeTextObj.transform.SetParent(bubble.transform);
    var timeText = timeTextObj.AddComponent<TextMeshProUGUI>();
    timeText.text = System.DateTime.Now.ToString("HH:mm"); // Saat: Dakika
    timeText.fontSize = 27;
    timeText.color = new Color32(0xE8, 0xF1, 0xF2, 255); // Açık renk
    timeText.alignment = TextAlignmentOptions.BottomRight;

    // RectTransform ayarları
    var timeRect = timeText.GetComponent<RectTransform>();
    timeRect.anchorMin = new Vector2(1, 0);
    timeRect.anchorMax = new Vector2(1, 0);
    timeRect.pivot = new Vector2(1, 0);
    timeRect.anchoredPosition = new Vector2(-5, 5); // sağ alt köşeye hafif boşluk

   
    // Sağ veya sol hizalama
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

    // Balon genişliğini ve yüksekliğini dinamik olarak ayarla
    LayoutRebuilder.ForceRebuildLayoutImmediate(rect); // Layout'u yeniden oluştur
    float padding = 20f; // Balonun içindeki metin için boşluk
    float maxWidth = 300f; // Maksimum genişlik sınırı
    float preferredWidth = Mathf.Min(text.preferredWidth + padding, maxWidth);
    float preferredHeight = text.preferredHeight + padding;

    rect.sizeDelta = new Vector2(preferredWidth, preferredHeight);

    // Yeni balonun pozisyonunu ayarla
    float yOffset = 0;
    foreach (RectTransform child in chatContent)
    {
        yOffset -= child.sizeDelta.y + 10; // 10 birim boşluk ekleyin
    }
    rect.anchoredPosition = new Vector2(0, yOffset);
}


    string ParseReply(string json)
    {
        var wrapper = JsonUtility.FromJson<OpenAIResponseWrapper>(json);
        return wrapper.choices[0].message.content;
    }

    [System.Serializable] public class OpenAIResponseWrapper { public Choice[] choices; }
    [System.Serializable] public class Choice { public Message message; }
    [System.Serializable] public class Message { public string content; }
} 