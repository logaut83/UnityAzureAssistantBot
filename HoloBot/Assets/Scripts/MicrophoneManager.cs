using HoloToolkit;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using HoloToolkit.Unity;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System;

#if WINDOWS_UWP
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

/// <summary>
/// MicrophoneManager lets us capture audio from the user and feed into speech recognition
/// Make sure to enable the Microphone capability in the Windows 10 UWP Player Settings
/// </summary>
public class MicrophoneManager : MonoBehaviour
{

    // Use this string to cache the text currently displayed in the text box.
    public Animator animator;
    public AudioSource selectedSource;
    //public Text captions;
    public CaptionsManager captionsManager;
    public Billboard billboard;

    // Using an empty string specifies the default microphone. 
    private static string deviceName = string.Empty;
    private const int messageLength = 10;
    //private BotService tmsBot = new BotService();
    private AudioSource[] audioSources;
    private AudioSource ttsAudioSrc;

    public string requestText;
    public string result;
    private string endPoint;
    public GameObject cube;

    private int samplingRate;

    private string luisValue;

    string requestData;


    private string accessToken;

    private string requestUri = "https://speech.platform.bing.com/synthesize";
    private string sttUri = "https://speech.platform.bing.com/recognize";
    private string accessUri = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

    public string apiKey = "efef3982be3844b5ac6483c8d3a3683d";


    void Awake()
    {

        audioSources = this.GetComponents<AudioSource>();
        foreach (AudioSource a in audioSources)
        {
            if (a.clip == null)
            {
                ttsAudioSrc = a;
            }

            if ((a.clip != null) && (a.clip.name == "Ping"))
            {
                selectedSource = a;
            }
        }
        // Query the maximum frequency of the default microphone. Use 'unused' to ignore the minimum frequency.
        int unused;
        Microphone.GetDeviceCaps(deviceName, out unused, out samplingRate);

        captionsManager.SetCaptionsText("");

        //billboard.enabled = false;

    }

    void Start()
    {

        endPoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/e513bafa-d3af-47bc-8233-ff76814f3982?subscription-key=735f7ac9699444df82a31a21b7204fcd&timezoneOffset=0.0&verbose=true&q=";
        result = "";
        requestText = "";
        captionsManager.SetCaptionsText("Bienvenue");
        StartCoroutine(GetAccessToken());
    }

    void Update()
    {

        // Add condition to check if dictationRecognizer.Status is Running
        //if (!Microphone.IsRecording(deviceName) && dictationRecognizer.Status == SpeechSystemStatus.Running)
        //{
        //    // This acts like pressing the Stop button and sends the message to the Communicator.
        //    // If the microphone stops as a result of timing out, make sure to manually stop the dictation recognizer.
        //    // Look at the StopRecording function.
        //    SendMessage("RecordStop");
        //}
        if (ttsAudioSrc.isPlaying)
        {
            billboard.enabled = true;
        }
        else
        {
            billboard.enabled = false;
        }

    }

    /// <summary>
    /// Activate speech recognition only when the user looks straight at the bot
    /// </summary>
    void OnGazeEnter()
    {
        if (!Microphone.IsRecording(deviceName))
        {
            audioSources[0].clip = Microphone.Start(deviceName, true, 10, samplingRate);
            cube.GetComponent<Renderer>().material.color = Color.red;
        }

         if (!ttsAudioSrc.isPlaying)
         {
             //captionsManager.ToggleKeywordRecognizer(false);
             if (selectedSource != null)
             {
                selectedSource.Play();
             }
              //animator.Play("Idle");
             //StartCoroutine(CoStartRecording());
        
         }
    }

    void OnGazeLeave()
    {
        Microphone.End(deviceName);
        StartCoroutine(GetText());


    }


    public void OnClicDaButtonHello()
    {
        requestText = "hello";
        cube.GetComponent<Renderer>().material.color = Color.blue;
#if WINDOWS_UWP
        StartCoroutine(GetTextLuis());
#endif

    }

    public void OnClicDaButtonLanceDemo()
    {
        requestText = "lance la demo 1";
        cube.GetComponent<Renderer>().material.color = Color.magenta;
#if WINDOWS_UWP
        StartCoroutine(GetTextLuis());
#endif

    }

    public void OnClicDaButtonNone()
    {
        requestText = "oaizudapzpadup";
        cube.GetComponent<Renderer>().material.color = Color.yellow;
#if WINDOWS_UWP
        StartCoroutine(GetTextLuis());
#endif

    }

#if WINDOWS_UWP
    IEnumerator GetTextLuis()
    {

        cube.GetComponent<Renderer>().material.color = Color.magenta;
        string escUrl = WWW.UnEscapeURL(endPoint + requestText );
        

        UnityWebRequest www  = UnityWebRequest.Get(escUrl);
        string newUrl = escUrl.Replace(" ", "%20");
        www.url = newUrl;
        yield return www.Send();
        yield return new WaitForSeconds(1);
        luisValue = www.downloadHandler.text;
        JObject luisReturnQuery = JObject.Parse(luisValue);

        string luisIntent = luisReturnQuery.SelectToken("intents[0].intent").ToString(); //the accurate intent

        GetAccessTokenBut();

        switch (luisIntent) {

            case "gestionMenu":
                
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    // Display captions for the question
                    captionsManager.SetCaptionsText(result);
                }, false);
                //MyTTS.SpeakText(result);
                cube.GetComponent<Renderer>().material.color = Color.green;
               
                if (luisReturnQuery.SelectToken("entities[0]") != null)
                {
                    if(luisReturnQuery.SelectToken("entities[1].type").ToString() == "numDemo")
                    {
                        result = "tu es dans la gestion du menu, je lance la démo numéro " + luisReturnQuery.SelectToken("entities[1].entity").ToString();
                    }
                    if (luisReturnQuery.SelectToken("entities[0].type").ToString() == "numDemo")
                    {
                        result = "tu es dans la gestion du menu, je lance la démo numéro " + luisReturnQuery.SelectToken("entities[1].entity").ToString();
                    }

                    StartCoroutine(GetAudio());

                }
                break;
            case "None":
                result = "Je n'ai pas compris";
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    // Display captions for the question
                    captionsManager.SetCaptionsText(result);
                }, false);
                cube.GetComponent<Renderer>().material.color = Color.red;
                //MyTTS.SpeakText(result);
                //WindowsVoice.theVoice.speak("I don't understand what you are saying");
                break;


        }

    }
#endif


    IEnumerator GetAccessToken()
    {
        UnityWebRequest www = UnityWebRequest.Post(accessUri, apiKey);

        www.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);
        yield return www.Send();
        yield return new WaitForSeconds(1);
        byte[] results = www.downloadHandler.data;
        accessToken = Encoding.UTF8.GetString(results);
        yield return Encoding.UTF8.GetString(results);
        Debug.Log(www.responseCode);
    }

    IEnumerator GetAudio()
    {
        GetAccessTokenBut();
        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Content-type", "application/ssml+xml");
        headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        headers.Add("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");
        headers.Add("Authorization", "Bearer " + accessToken);
        headers.Add("X-Search-AppId", "07D3234E49CE426DAA29772419F436CA");
        headers.Add("X-Search-ClientID", "1ECFAE91408841A480F00935DC390960");
        headers.Add("User-Agent", "BingSpeechDemo");

        requestData = GenerateSsml("fr-FR", "Male", "Microsoft Server Speech Text to Speech Voice (fr-FR, Paul, Apollo)", result);

        Debug.Log("requestData : " + requestData);

        //StopCoroutine(GetText());
        //StopCoroutine(GetAccessToken());
        //StopCoroutine(GetTextLuis());

        Encoding encode = Encoding.UTF8;
        byte[] unicodeBytes = encode.GetBytes(requestData);
        WWW wwwReq = new WWW(requestUri, unicodeBytes, headers);
        Debug.Log(wwwReq.error);
        yield return wwwReq;
        cube.GetComponent<Renderer>().material.color = Color.white;
        Debug.Log("IICII");
        Debug.Log("erreur " + wwwReq.error);
        audioSources[0].clip = WWWAudioExtensions.GetAudioClip(wwwReq, false, false, AudioType.WAV);
        Debug.Log(audioSources[0].clip.loadState);
        audioSources[0].Play();
        yield return audioSources[0].clip;


    }

    IEnumerator GetText()
    {
        captionsManager.SetCaptionsText("Get text");
        string sttUriSend = sttUri;
        sttUriSend += @"?scenarios=smd";                                  // websearch is the other main option.
        sttUriSend += @"&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5";     // You must use this ID.
        sttUriSend += @"&locale=fr-FR";                                   // We support several other languages.  Refer to README file.
        sttUriSend += @"&device.os=wp7";
        sttUriSend += @"&version=3.0";
        sttUriSend += @"&format=json";
        sttUriSend += @"&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3";
        sttUriSend += @"&requestid=" + Guid.NewGuid().ToString();

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Content-type", @"audio/wav; codec=""audio/pcm""; samplerate=16000");
        headers.Add("Host", @"speech.platform.bing.com");
        headers.Add("Accept", @"application/json;text/xml");
        headers.Add("Authorization", "Bearer " + accessToken);


        cube.GetComponent<Renderer>().material.color = Color.blue;

        SavWav.Save("speechWav", audioSources[0].clip);

        string audioFile = Path.Combine(Application.persistentDataPath, "speechWav.wav");



        Encoding encode = Encoding.UTF8;
        byte[] fileData = null;
        using (FileStream fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
        {
            var binaryReader = new BinaryReader(fs);
            fileData = binaryReader.ReadBytes((int)fs.Length);
        }


        WWW wwwReq = new WWW(sttUriSend, fileData, headers);
        yield return wwwReq;

#if WINDOWS_UWP
        JObject ReturnQuery = JObject.Parse(wwwReq.text);
        string textDit = ReturnQuery.SelectToken("results[0].name").ToString();

        Debug.Log(ReturnQuery.SelectToken("results[0]"));
        Debug.Log(ReturnQuery.SelectToken("results[1]"));

        captionsManager.SetCaptionsText(textDit);
        requestText = textDit;
        StartCoroutine(GetTextLuis());
        if (textDit == "")
        {
            captionsManager.SetCaptionsText("Rien compris");
        }
#endif

        if (wwwReq.error != null)
            captionsManager.SetCaptionsText(wwwReq.error);
        

        yield return 0;
    }

    private string GenerateSsml(string locale, string gender, string name, string text)
    {
        var ssmlDoc = new XDocument(
                          new XElement("speak",
                              new XAttribute("version", "1.0"),
                              new XAttribute(XNamespace.Xml + "lang", "en-US"),
                              new XElement("voice",
                                  new XAttribute(XNamespace.Xml + "lang", locale),
                                  new XAttribute(XNamespace.Xml + "gender", gender),
                                  new XAttribute("name", name),
                                  text)));
        return ssmlDoc.ToString();
    }

    public void GetAccessTokenBut()
    {
        StartCoroutine(GetAccessToken());
    }

    public void GetAudioClip()
    {
        StartCoroutine(GetAudio());
    }

    public void GetTextSTT()
    {
        StartCoroutine(GetText());
    }

}