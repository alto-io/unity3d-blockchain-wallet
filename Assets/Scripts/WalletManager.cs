using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WalletManager : MonoBehaviour {

    public PasswordInputField passwordInputField;

    public LogText logText;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    
    public void CreateWallet()
    {
        if (passwordInputField.passwordConfirmed())
        {
            // Here we call CreateAccount() and we send it a password to encrypt the new account
            // (for now are going to use "strong_password") and a callback
            CreateAccount(passwordInputField.passwordString(), (address, encryptedJson) =>
            {
                // We just print the address and the encrypted json we just created
                Debug.Log(address);
                Debug.Log(encryptedJson);
                logText.Log("Wallet Created:" , address);
            });
        }

        else
        {
            logText.Log("passwords don't match");
        }
    }

    // This function will just execute a callback after it creates and encrypt a new account
    public void CreateAccount(string password, System.Action<string, string> callback)
    {
        // We use the Nethereum.Signer to generate a new secret key
        var ecKey = Nethereum.Signer.EthECKey.GenerateKey();

        // After creating the secret key, we can get the public address and the private key with
        // ecKey.GetPublicAddress() and ecKey.GetPrivateKeyAsBytes()
        // (so it return it as bytes to be encrypted)
        var address = ecKey.GetPublicAddress();
        var privateKey = ecKey.GetPrivateKeyAsBytes();

        // Then we define a new KeyStore service
        var keystoreservice = new Nethereum.KeyStore.KeyStoreService();

        // And we can proceed to define encryptedJson with EncryptAndGenerateDefaultKeyStoreAsJson(),
        // and send it the password, the private key and the address to be encrypted.
        var encryptedJson = keystoreservice.EncryptAndGenerateDefaultKeyStoreAsJson(password, privateKey, address);
        // Finally we execute the callback and return our public address and the encrypted json.
        // (you will only be able to decrypt the json with the password used to encrypt it)
        callback(address, encryptedJson);
    }
}
