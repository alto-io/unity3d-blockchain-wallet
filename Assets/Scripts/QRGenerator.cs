using ZXing;
using ZXing.QrCode;
using UnityEngine;
using System;

public class QRGenerator : MonoBehaviour
{

    private WebCamTexture camTexture;
    private Rect screenRect;
    void Start()
    {
        /*
        screenRect = new Rect(0, 0, Screen.width, Screen.height);
        camTexture = new WebCamTexture();
        camTexture.requestedHeight = Screen.height;
        camTexture.requestedWidth = Screen.width;
        if (camTexture != null)
        {
            camTexture.Play();
        }
        */
    }

    private static Color32[] Encode(string textForEncoding,
      int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width
            }
        };
        return writer.Write(textForEncoding);
    }


    public Texture2D generateQR(string text)
    {
        var encoded = new Texture2D(256, 256);
        var color32 = Encode(text, encoded.width, encoded.height);
        encoded.SetPixels32(color32);
        encoded.Apply();
        return encoded;
    }

    void OnGUI()
    {

        Texture2D myQR = generateQR("test");
        if (GUI.Button(new Rect(0,  0, 256, 256), myQR, GUIStyle.none)) { }

        /*
        // drawing the camera on screen
        GUI.DrawTexture(screenRect, camTexture, ScaleMode.ScaleToFit);
        // do the reading — you might want to attempt to read less often than you draw on the screen for performance sake
        try
        {
            IBarcodeReader barcodeReader = new BarcodeReader();
            // decode the current frame
            var result = barcodeReader.Decode(camTexture.GetPixels32(),
              camTexture.width, camTexture.height);
            if (result != null)
            {
                Debug.Log("DECODED TEXT FROM QR: " + result.Text);
            }
        }
        catch (Exception ex) { Debug.LogWarning(ex.Message); }
        */
    }
}