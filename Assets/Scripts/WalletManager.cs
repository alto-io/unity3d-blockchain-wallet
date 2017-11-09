using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class WalletManager : MonoBehaviour {

    // create class as singleton
    private static WalletManager instance;
    public static WalletManager Instance { get { return instance; } }
    public void Awake() { if (instance == null) instance = this; }
    public void OnDestroy() { if (instance == this) instance = null; }

    // UI Components
    public PasswordInputField passwordInputField;
    public LogText logText;
    public GameObject passwordPanel;
    public GameObject ButtonPanel;
    public GameObject loadingIndicatorPanel;

    private bool isPaused = false;
    private bool dataSaved = false;

    [System.Serializable]
    public class WalletData
    {
        public string name;
        public string address;

        // TODO: stored for convenience, may need to remove for security?
        public string cachedPassword;
        public string encryptedJson;
        public byte[] privateKey;
    }

    private static List<WalletData> walletList = new List<WalletData>();

    // used for saving 
    private BinaryFormatter bf;
    private FileStream file;
    private string filePath;
    private const string fileName = "walletcache.data";


    public void Start()
    {
        LoadWalletsFromFile();
    }

    void LoadWalletsFromFile()
    {
        filePath = (Application.persistentDataPath + "/" + fileName);

        if (File.Exists(filePath))
        {
            bf = new BinaryFormatter();
            file = File.Open(filePath, FileMode.Open);

            walletList = (List<WalletData>)bf.Deserialize(file);

            foreach (WalletData w in walletList)
            {
                logText.Log("Name: " +  w.name);
                logText.Log("Address: " + w.address);
            }

            file.Close();
        }

        logText.Log("Loaded " + walletList.Count + " Wallet/s");

    }

    void SaveDataToFile()
    {

        bf = new BinaryFormatter();
        file = File.Create(filePath);

        bf.Serialize(file, walletList);
        file.Close();

        dataSaved = true;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        isPaused = !hasFocus;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        isPaused = pauseStatus;

        if (isPaused)
            SaveDataToFile();
        else
            dataSaved = false;
    }

    void OnApplicationQuit()
    {
        if (!dataSaved)
            SaveDataToFile();
    }


    private void showLoadingIndicator(bool loading)
    {
        if (loading)
        {
            passwordPanel.SetActive(false);
            ButtonPanel.SetActive(false);

            loadingIndicatorPanel.SetActive(true);
        }
    }

    public void CreateWallet()
    {
        if (passwordInputField.passwordConfirmed())
        {

            showLoadingIndicator(true);

            // Here we call CreateAccount() and we send it a password to encrypt the new account
            StartCoroutine(CreateAccountCoroutine(passwordInputField.passwordString(),
                "Account " + (walletList.Count + 1))); 
        }

        else
        {
            logText.Log("passwords don't match");
        }
    }

   // We create the function which will check the balance of the address and return a callback with a decimal variable
    public static IEnumerator CreateAccountCoroutine(string password, string accountName)
    {
        yield return 0; // allow UI to updates

        CreateAccount(password, (address, encryptedJson, privateKey) =>
        {
            // We just print the address and the encrypted json we just created
            Debug.Log(address);
            Debug.Log(encryptedJson);

            WalletData w = new WalletData();
            w.name = accountName;
            w.address = address;
            w.cachedPassword = password;
            w.encryptedJson = encryptedJson;
            w.privateKey = privateKey;

            walletList.Add(w);
        });
    }

    // This function will just execute a callback after it creates and encrypt a new account
    public static void CreateAccount(string password, System.Action<string, string, byte[]> callback)
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
        callback(address, encryptedJson, privateKey);
    }





}
