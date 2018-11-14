using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Video;
using System.Linq;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System;
using TextureSendReceive;

public class manager : MonoBehaviour
{
    public bool isAtStartup = true;
    private NetworkClient myClient;
 
    [Header("Client Video Player Variable")]
    public VideoPlayer vp;
    public RenderTexture renderTextureSkyBox;
    public Material mat;
    public Button StopPlay;

    [Header("Server Port and Scroll View")]
    public int port = 4444;
    public GameObject ContentPanel;
    public GameObject GridItemPrefab;
    public Button startServerBtn;
    public TMP_InputField vidNameTxt;
    public Image serverUpIndicator;
    public Button SkipToBtn;

    public static List<ClientInfo> AllClients;
    private ClientInfo c;
    private bool inProccessingNewConnection;

    private Texture2D sendImage2DMain;

    void Awake()
    {
        renderTextureSkyBox.Release();
    }
    void Update()
    {
        if(Input.GetKeyDown("c"))
        {
            ipAddressText.text = "192.168.1.89";
            SetupClient();
        }

        if (myClient != null && vp.isPlaying)
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                if (seekbar.activeSelf)
                {
                    seekbar.SetActive(false);
                }
                else if (!seekbar.activeSelf)
                {
                    seekbar.SetActive(true);
                }
            }
        }

        if (myClient != null && vp.isPlaying)
        {
            setCurrentTimeUI();
            headMover.MovePlayhead(CalculatePlayedFraction());
        }


        if(NetworkServer.active &&  NetworkServer.connections.Count > 0)
        {
            if (AllClients != null)
                {
                    foreach (ClientInfo c in AllClients)
                    {
                    c.dc = true;
                        foreach (NetworkConnection con in NetworkServer.connections)
                        {
                            if (con == null) continue;

                            string conIP = con.address;
                            string cIP = c.ipAddress;

                            if (!conIP.Contains("::ffff:")) conIP = "::ffff:" + conIP;
                            if (!cIP.Contains("::ffff:")) cIP = "::ffff:" + cIP;

                            if (cIP == conIP)
                            {
                                c.dc = false;
                                break;
                            }
                        }
                    }
                }
        }
    }

    private void Start()
    {
        if (disconnectMessage != null) disconnectMessage.SetActive(false);
        if (StopPlay != null) StopPlay.interactable = false;
        AllClients = new List<ClientInfo>();
        if (seekbar != null) seekbar.SetActive(false);
        if (SkipToBtn != null) SkipToBtn.interactable = false;

        if(ipAddressText != null)
        {
            if (PlayerPrefs.GetString("last connected ip") == null)
            {
                ipAddressText.text = "IP Address";
            }
            else
            {
                ipAddressText.text = PlayerPrefs.GetString("last connected ip");
            }
        }
    }

    public class MyMsgType
    {
        public static short Cmd = MsgType.Highest + 1;
        public static short DisconectInf = MsgType.Highest + 2;
        public static short ClientInf = MsgType.Highest + 3;
    };

    public TMP_Text serverTxt;
    public void SetupServer()
    {
        NetworkServer.Listen(port);
        NetworkServer.RegisterHandler(MyMsgType.ClientInf, OnClientInfoReceived);
        isAtStartup = false;
        startServerBtn.interactable = false;
        serverUpIndicator.GetComponent<Image>().color = new Color32(91, 194, 54, 255);
        serverTxt.text = LocalIPAddress();
    }

    public void OnClientInfoReceived(NetworkMessage netMsg)
    {
        c = new ClientInfo();
        c = netMsg.ReadMessage<ClientInfo>();
        if(!AllClients.Any(item => item.uniqueIdf == c.uniqueIdf))
        {
            AllClients.Add(c);

            GameObject newClientJoin = Instantiate(GridItemPrefab) as GameObject;
            GridItemController controller = newClientJoin.GetComponent<GridItemController>();
            controller.batteryLv.text = c.battery.ToString();
            controller.unqIdf = c.uniqueIdf;
            controller.iP.text = c.ipAddress;
            controller.statusConn.text = "Connected";
            newClientJoin.transform.SetParent(ContentPanel.transform);
            newClientJoin.transform.localScale = Vector3.one;
        }
        else
        {
            AllClients.Find(x => x.uniqueIdf == c.uniqueIdf).battery = c.battery;
            AllClients.Find(x => x.uniqueIdf == c.uniqueIdf).dc = false;
        }
    }

    public class Cmd : MessageBase
    {
        public string c;
        public string VideoName;
        public string min;
        public string sec;
    }

    private void SendCommand(string Command, string videoName)
    {
        Cmd command = new Cmd
        {
            c = Command,
            VideoName = videoName
        };
        NetworkServer.SendToAll(MyMsgType.Cmd, command);
    }

    private void SendCommand(string Command, string m, string s)
    {
        Cmd command = new Cmd
        {
            c = Command,
            min = m,
            sec = s
        };
        NetworkServer.SendToAll(MyMsgType.Cmd, command);
    }

    public void PlayAll()
    {
        SendCommand("PLAY", vidNameTxt.text);
        StopPlay.interactable = true;
        SkipToBtn.interactable = true;
    }

    public void StopAll()
    {
        SendCommand("STOP", "");
        StopPlay.interactable = false;
        SkipToBtn.interactable = false;
    }

    public TMP_InputField minInput;
    public TMP_InputField secInput;

    public void SkipTo()
    {
        SendCommand("SKIP", minInput.text, secInput.text);
        StopPlay.interactable = true;
    }

    public void Quit()
    {
        Application.Quit();
    }


    //---------------------------------------------------------------------------------------------------------------------------
    // Create a client and connect to the server port

    private ClientInfo ci;

    [Header("Client IP to Connect")]
    public Text ipAddressText;
    public GameObject numpad;

    public void SetupClient()
    {
        myClient = new NetworkClient();
        myClient.RegisterHandler(MsgType.Connect, OnConnected);
        myClient.RegisterHandler(MsgType.Disconnect, OnDisconnect);
        myClient.RegisterHandler(MyMsgType.Cmd, OnReceivedCommand);
        myClient.Connect(ipAddressText.text, 4444);
        isAtStartup = false;
    }

    public GameObject disconnectMessage;
    public Camera RenderCam;

    private void OnDisconnect(NetworkMessage netMsg)
    {
        StopCoroutine(SendBatteryStat());
        myClient.UnregisterHandler(MyMsgType.Cmd);
        myClient.UnregisterHandler(MsgType.Connect);
        Destroy(RenderCam.GetComponent<CameraSender>());
        Destroy(RenderCam.GetComponent<TextureSender>());

        StartCoroutine(retryConnect());
    }

    public string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }

    bool reconnect = false;

    private void OnConnected(NetworkMessage netMsg)
    {
        Debug.Log("Connected to server");
        Debug.Log(myClient.serverIp);
        PlayerPrefs.SetString("last connected ip", ipAddressText.text);
        if (reconnect)
        {
            RenderCam.gameObject.AddComponent<CameraSender>();
            reconnect = false;
        }

        ClientInfo cinf = new ClientInfo
        {
            uniqueIdf = SystemInfo.deviceUniqueIdentifier,
            battery = SystemInfo.batteryLevel * 100,
            dc = false,
            ipAddress = LocalIPAddress()
        };
        myClient.Send(MyMsgType.ClientInf, cinf);

        Debug.Log("SEND " + cinf.uniqueIdf);

        numpad.SetActive(false);

        StopAllCoroutines();

        disconnectMessage.SetActive(false);

        StartCoroutine(SendBatteryStat());
    }

    private string header = "file:///sdcard/Movies/";

    public void OnReceivedCommand(NetworkMessage netMsg)
    {
        string path;
        Cmd receiveCommand = netMsg.ReadMessage<Cmd>();
        Debug.Log("AY Captain, " + receiveCommand.c);
        switch (receiveCommand.c)
        {
            case "PLAY":
                if (receiveCommand.VideoName.Length > 0 && receiveCommand.VideoName != null)
                {
                    path = header + receiveCommand.VideoName;
                }
                else
                {
                    path = header + "video.mp4";
                }

                Debug.Log(path);
                vp.url = path;
                vp.prepareCompleted += PrepareCompleted;
                vp.loopPointReached += EndReached;
                vp.Prepare();
                break;

            case "STOP":
                vp.Stop();
                renderTextureSkyBox.Release();
                seekbar.SetActive(true);
                break;

            case "SKIP":
                double min, sec;
                if (receiveCommand.min == null || receiveCommand.min == "")
                {
                    min = 0f;
                }
                else
                {
                    min = double.Parse(receiveCommand.min) * 60;
                }

                if (receiveCommand.sec == null || receiveCommand.sec == "")
                {
                    sec = 0f;
                }
                else
                {
                    sec = double.Parse(receiveCommand.sec);
                }

                if (!vp.isPlaying)
                {
                    vp.Play();
                }

                vp.time = min + sec;
                break;
        }
    }

    void EndReached(VideoPlayer v)
    {
        seekbar.SetActive(true);
    }

    void PrepareCompleted(VideoPlayer v)
    {
        seekbar.SetActive(true);
        setTotalTimeUI();
        v.Play();
    }


    public class BatteryStat : MessageBase
    {
        public float battery;
    }

    public class ClientInfo : MessageBase
    {
        public string uniqueIdf;
        public float battery;
        public string ipAddress;
        public bool dc;
    }

    IEnumerator retryConnect()
    {
        yield return new WaitUntil(() => !vp.isPlaying);
        reconnect = true;
        SetupClient();
        disconnectMessage.SetActive(true);
        yield return new WaitForSeconds(10f);

    }

    IEnumerator SendBatteryStat()
    {
        float currbattery = SystemInfo.batteryLevel;
        ci = new ClientInfo();
        while (true)
        {
            yield return new WaitForSeconds(10.0f);
            Debug.Log(SystemInfo.batteryLevel);
            if (currbattery != SystemInfo.batteryLevel) //send only if the battery value changed
            {
                ci.battery = SystemInfo.batteryLevel * 100;
                ci.uniqueIdf = SystemInfo.deviceUniqueIdentifier;
                ci.dc = false;
                myClient.Send(MyMsgType.ClientInf, ci);
                Debug.Log("SEND");
                currbattery = SystemInfo.batteryLevel;
            }
        }
    }

    [Header("Seekbar")]
    public TMP_Text curMin;
    public TMP_Text curSec;
    public TMP_Text totMin;
    public TMP_Text totSec;
    public GameObject seekbar;
    public HeadMover headMover;

    void setCurrentTimeUI()
    {
        string min = Mathf.Floor((int)vp.time / 60).ToString("00");
        string sec = ((int)vp.time % 60).ToString("00");

        curMin.text = min;
        curSec.text = sec;
    }

    void setTotalTimeUI()
    {
        double time = vp.frameCount / vp.frameRate;
        string min = Mathf.Floor((int)time / 60).ToString("00");
        string sec = ((int)time % 60).ToString("00");

        totMin.text = min;
        totSec.text = sec;
    }

    double CalculatePlayedFraction()
    {
        double fraction = (double)vp.frame / (double)vp.frameCount;
        return fraction;
    }
}
