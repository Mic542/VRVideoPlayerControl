using System.Collections;
using System.Collections.Generic;
using TextureSendReceive;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridItemController : MonoBehaviour {

    public TextMeshProUGUI batteryLv;
    public Image image;
    public string unqIdf;
    public TMP_Text iP;
    public TMP_Text statusConn;

    TextureReceiver receiver;
    public RawImage img;
    Texture2D texture;
    bool reconnect = false;

    // Use this for initialization
    void Start ()
    {
        receiver = GetComponent<TextureReceiver>();
        receiver.IP = iP.text;
        texture = new Texture2D(1, 1);
        img.texture = texture;

        receiver.SetTargetTexture(texture);
    }

    private void ReSetEverything()
    {
        gameObject.AddComponent<TextureReceiver>();
        receiver = null;
        receiver = GetComponent<TextureReceiver>();
        receiver.IP = iP.text;
        texture = new Texture2D(1, 1);
        img.texture = texture;

        receiver.SetTargetTexture(texture);
    }
   
    // Update is called once per frame
    void Update () {
        if (manager.AllClients != null || unqIdf != "" || unqIdf.Length > 0 || unqIdf != null)
        {
            float b = manager.AllClients.Find(x => x.uniqueIdf == unqIdf).battery;
            batteryLv.text = manager.AllClients.Find(x => x.uniqueIdf == unqIdf).battery.ToString();

            if (b >= 60)
            {
                image.GetComponent<Image>().color = new Color32(91, 194, 54, 255);
            }
            else if (b < 60 && b > 30)
            {
                image.GetComponent<Image>().color = new Color32(255, 204, 0, 255);
            }
            else
            {
                image.GetComponent<Image>().color = new Color32(255, 0, 0, 255);
            }

            image.GetComponent<Image>().fillAmount = b / 100;

        }
        if(manager.AllClients.Find(x => x.uniqueIdf == unqIdf).dc)
        {
            image.GetComponent<Image>().color = new Color32(0, 0, 0, 50);
            statusConn.color = new Color32(255, 0, 0, 255);
            statusConn.text = "DC";
            if(gameObject.GetComponent<TextureReceiver>()) Destroy(gameObject.GetComponent<TextureReceiver>());
            reconnect = true;
        }
        else
        {
            statusConn.text = "Connected";
            statusConn.color = new Color32(91, 194, 54, 255);
            if(reconnect)
            {
               reconnect = false;
               ReSetEverything();
            }
        }
    }
}
