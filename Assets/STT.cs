using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SpeechToTextService : MonoBehaviour
{
    public static SpeechToTextService Instance { get; private set; }

    [Header("Deepgram API Settings (ÜCRETSİZ - 200 saat/ay)")]
    [Tooltip("Deepgram API Key - https://console.deepgram.com/signup adresinden ücretsiz alın")]
    public string deepgramApiKey = "6330e2081e4a785ea0e2a0fc895ec2ceeab150c1";

    [Header("Preferences")]
    [Tooltip("Tercih edilen tanıma dili. Deepgram 30+ dil destekler:\n" +
             "tr (Türkçe), en (İngilizce), de (Almanca), fr (Fransızca), es (İspanyolca),\n" +
             "it (İtalyanca), pt (Portekizce), ru (Rusça), ja (Japonca), ko (Korece),\n" +
             "zh (Çince), ar (Arapça), hi (Hintçe), nl (Felemenkçe), pl (Lehçe), vb.\n" +
             "Boş bırakırsanız otomatik dil tespiti yapar.")]
    public string preferredLanguage = "tr"; // Türkçe

    [Header("Settings")]
    public bool logSteps = false;
    public bool saveTranscriptTxt = false;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Ana transcribe fonksiyonu
    public IEnumerator Transcribe(byte[] wavBytes, string fileName, Action<string> onTranscript)
    {
        yield return StartCoroutine(TranscribeWithDeepgram(wavBytes, fileName, onTranscript));
    }

    // Deepgram API ile transcribe (ÜCRETSİZ - 200 saat/ay)
    private IEnumerator TranscribeWithDeepgram(byte[] wavBytes, string fileName, Action<string> onTranscript)
    {
        if (string.IsNullOrEmpty(deepgramApiKey))
        {
            Debug.LogError("[STT] Deepgram API Key boş! Inspector'da girin. Ücretsiz almak için: https://console.deepgram.com/signup");
            onTranscript?.Invoke("");
            yield break;
        }

        // API key'den başta ve sonda boşlukları temizle
        string cleanApiKey = deepgramApiKey.Trim();
        
        if (string.IsNullOrEmpty(cleanApiKey))
        {
            Debug.LogError("[STT] Deepgram API Key geçersiz! Lütfen doğru API key'i girin.");
            onTranscript?.Invoke("");
            yield break;
        }

        if (logSteps)
        {
            Debug.Log($"[STT] Deepgram API'ye gönderiliyor...");
            Debug.Log($"[STT] WAV boyutu: {wavBytes.Length} bytes");
            Debug.Log($"[STT] Dosya adı: {fileName}");
            Debug.Log($"[STT] Dil: {preferredLanguage}");
        }

        // Deepgram API endpoint
        string languageParam = string.IsNullOrEmpty(preferredLanguage) ? "" : $"&language={preferredLanguage}";
        string apiUrl = $"https://api.deepgram.com/v1/listen?model=nova-2&punctuate=true&diarize=false{languageParam}";

        using (UnityWebRequest req = new UnityWebRequest(apiUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(wavBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Token " + cleanApiKey);
            req.SetRequestHeader("Content-Type", "audio/wav");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[STT] Deepgram API hatası: {req.error}");
                Debug.LogError($"[STT] HTTP kodu: {req.responseCode}");
                Debug.LogError($"[STT] Yanıt: {req.downloadHandler.text}");
                onTranscript?.Invoke("");
                yield break;
            }

            string response = req.downloadHandler.text;
            if (logSteps)
            {
                Debug.Log($"[STT] Deepgram yanıtı alındı: {response}");
            }

            string transcript = ParseDeepgramResponse(response);
            if (logSteps)
            {
                Debug.Log($"[STT] Parse edilen transcript: '{transcript}'");
            }

            // Boş transcript kontrolü
            if (string.IsNullOrEmpty(transcript))
            {
                Debug.LogWarning("[STT] Boş transkript alındı");
                onTranscript?.Invoke("");
                yield break;
            }

            // Transcript'i dosyaya kaydet
            if (saveTranscriptTxt)
            {
                SaveTranscriptAlongsideWav(transcript, fileName);
            }

            onTranscript?.Invoke(transcript);
        }
    }


    // Deepgram response parsing
    private string ParseDeepgramResponse(string jsonResponse)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogWarning("[STT] Boş JSON yanıtı alındı");
                return "";
            }

            // Deepgram yanıt formatı: {"results":{"channels":[{"alternatives":[{"transcript":"..."}]}]}}
            if (jsonResponse.Contains("\"transcript\""))
            {
                int transcriptIndex = jsonResponse.IndexOf("\"transcript\"");
                int start = jsonResponse.IndexOf("\"", transcriptIndex + 12) + 1;
                int end = jsonResponse.IndexOf("\"", start);
                
                if (end > start)
                {
                    string text = jsonResponse.Substring(start, end - start);
                    
                    // Unicode escape karakterlerini düzelt
                    if (text.Contains("\\u"))
                    {
                        text = System.Text.RegularExpressions.Regex.Unescape(text);
                    }
                    
                    if (!string.IsNullOrEmpty(text.Trim()))
                    {
                        return text.Trim();
                    }
                }
            }

            Debug.LogWarning("[STT] Geçerli transcript field'ı bulunamadı");
            return "";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[STT] Parse hatası: {ex.Message}");
            return "";
        }
    }


    private void SaveTranscriptAlongsideWav(string transcript, string fileName)
    {
        try
        {
            string baseDir = Application.persistentDataPath;
            string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
            string txtPath = Path.Combine(baseDir, nameNoExt + ".txt");
            File.WriteAllText(txtPath, transcript, Encoding.UTF8);
            Debug.Log("Transcript kaydedildi: " + txtPath);
        }
        catch (Exception e)
        {
            Debug.LogError("Transcript kaydetme hatası: " + e.Message);
        }
    }

    // Test fonksiyonu
    [ContextMenu("Test STT Service")]
    public void TestSttService()
    {
        Debug.Log("[STT] Test başlatılıyor...");
        Debug.Log($"[STT] Dil: {preferredLanguage}");
        Debug.Log($"[STT] Deepgram API Key durumu: {(string.IsNullOrEmpty(deepgramApiKey) ? "BOŞ - Lütfen Inspector'da girin! (Ücretsiz: https://console.deepgram.com/signup)" : "Ayarlanmış")}");
    }

    // Test için basit bir ses dosyası gönder
    [ContextMenu("Test with Sample Audio")]
    public IEnumerator TestWithSampleAudio()
    {
        Debug.Log("[STT] Test ses dosyası ile STT test ediliyor...");
        
        // Basit bir test ses dosyası oluştur (1 saniye sessizlik)
        int sampleRate = 16000;
        int channels = 1;
        float[] samples = new float[sampleRate * channels];
        
        // WAV formatında encode et
        byte[] wavBytes = CreateWavFile(samples, sampleRate, channels);
        
        yield return StartCoroutine(Transcribe(wavBytes, "test_audio.wav", (text) =>
        {
            Debug.Log($"[STT] Test sonucu: '{text}'");
        }));
    }
    
    // Basit WAV dosyası oluştur
    private byte[] CreateWavFile(float[] samples, int sampleRate, int channels)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // WAV header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2); // File size
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // Audio format (PCM)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * 2); // Byte rate
            writer.Write((short)(channels * 2)); // Block align
            writer.Write((short)16); // Bits per sample
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(samples.Length * 2);
            
            // Audio data
            foreach (float sample in samples)
            {
                short value = (short)(sample * 32767f);
                writer.Write(value);
            }
            
            return stream.ToArray();
        }
    }
}

