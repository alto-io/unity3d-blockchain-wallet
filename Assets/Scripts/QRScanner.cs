using ZXing;
using ZXing.QrCode;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QRScanner : MonoBehaviour {

    private WebCamTexture camTexture;
    private Rect screenRect;
    public InputField addressToField;

    // Use this for initialization
    void Start () {
        screenRect = new Rect(0, 0, Screen.width, Screen.height);
        camTexture = new WebCamTexture();
        camTexture.requestedHeight = Screen.height;
        camTexture.requestedWidth = Screen.width;
        if (camTexture != null)
        {
            camTexture.Play();
        }

    }

    // TODO: Optimize, also has an assertion failure
    void OnGUI()
    {
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
                addressToField.text = result.Text;
                WalletManager.Instance.ToggleQRScannerDisplay(true);
            }
        }
        catch (Exception ex) { Debug.LogWarning(ex.Message); }
    }

}
