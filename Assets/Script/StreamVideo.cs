using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
public class StreamVideo : MonoBehaviour
{
    //Raw Image to Show Video Images [Assign from the Editor]
    public RawImage image;
    //Video To Play [Assign from the Editor]
    private string videoToPlayUri;

    public Button StartPreviewButton;
    public Button StopPreviewButton;

    public InputField UriInput;

    private VideoPlayer videoPlayer;
    private VideoSource videoSource;

    //Audio
    private AudioSource audioSource;

    // Use this for initialization
    void Start()
    {
        Application.runInBackground = true;
        StartPreviewButton.interactable = false;
        StopPreviewButton.interactable = false;
        UriInput.onEndEdit.AddListener(delegate { SetVideoUri(UriInput); });
    }

    void SetVideoUri(InputField input)
    {
        if (input.text.Length > 0)
        {
            videoToPlayUri = input.text;
            StartPreviewButton.interactable = true;
        }
    }

    public void StartPrepareVideo()
    {
        StartCoroutine(playVideo());
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
        StopCoroutine(playVideo());
        image.texture = null;
    }

    IEnumerator playVideo()
    {
        StartPreviewButton.interactable = false;
        //Add VideoPlayer to the GameObject
        if (!gameObject.GetComponent<VideoPlayer>())
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }
        else
        {
            videoPlayer = gameObject.GetComponent<VideoPlayer>();
        }

        if (!gameObject.GetComponent<AudioSource>())
        { 
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            audioSource = gameObject.GetComponent<AudioSource>();
        }

        //Disable Play on Awake for both Video and Audio
        videoPlayer.playOnAwake = false;
        audioSource.playOnAwake = false;

        videoPlayer.url = videoToPlayUri;

        //Set Audio Output to AudioSource
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        //Assign the Audio from Video to AudioSource to be played
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);

        videoPlayer.Prepare();

        //Wait until video is prepared
        while (!videoPlayer.isPrepared)
        {
            Debug.Log("Preparing Video");
            yield return null;
        }

        Debug.Log("Done Preparing Video");

        //Assign the Texture from Video to RawImage to be displayed
        image.texture = videoPlayer.texture;

        //Play Video
        videoPlayer.Play();

        //Play Sound
        audioSource.Play();

        StopPreviewButton.interactable = true;

        Debug.Log("Playing Video");
        while (videoPlayer.isPlaying)
        {
            Debug.LogWarning("Video Time: " + Mathf.FloorToInt((float)videoPlayer.time));
            yield return null;
        }

        Debug.Log("Done Playing Video");
        StartPreviewButton.interactable = true;
        StopPreviewButton.interactable = false;
        StopVideo();
    }
}