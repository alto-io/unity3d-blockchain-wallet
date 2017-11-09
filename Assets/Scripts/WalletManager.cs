using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WalletManager : MonoBehaviour {

    public PasswordInputField passwordInputField;

    public Text logText;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    
    public void CreateWallet()
    {
        logText.text += "\nCreate Wallet";
        Debug.Log("Create Wallet");
    }
}
