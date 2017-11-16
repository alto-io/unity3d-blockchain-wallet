using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.Encoders;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;


public class WalletManager : MonoBehaviour {

    // create class as singleton
    private static WalletManager instance;
    public static WalletManager Instance { get { return instance; } }
    public void Awake() { if (instance == null) instance = this; }
    public void OnDestroy() { if (instance == this) instance = null; }

    [Header("Config")]

    public string networkUrl;

    [Header("UI Components")]

    public PasswordInputField passwordInputField;
    public LogText logText;
    public InputField recepientAddressInputField;
    public GameObject createWalletPanel;
    public GameObject loadingIndicatorPanel;
    public GameObject operationsPanel;
    public GameObject currencyInfoPanel;
    public GameObject currencyInfoContentRoot;
    public GameObject QRPanel;
    public RawImage QRCodeImage;
    public Text QRCodeLoadingText;
    public Dropdown walletSelectionDropdown;
    public Dropdown recepientAddressDropdown;

    private bool isPaused = false;
    private bool dataSaved = false;

    // events
    static UnityEvent newAccountAdded;
    static UnityEvent loadingFinished;

    [System.Serializable]
    public class WalletData
    {
        public string name;
        public string address;

        // TODO: stored for convenience, may need to remove for security
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
        subscribeToEvents();    
        LoadWalletsFromFile();
        RefreshTopPanelView();
    }

    private void subscribeToEvents()
    {
        newAccountAdded = new UnityEvent();
        loadingFinished = new UnityEvent();

        newAccountAdded.AddListener(RefreshWalletAccountDropdown);
        newAccountAdded.AddListener(RefreshTopPanelView);

        loadingFinished.AddListener(hideLoadingIndicator);
    }

    void LoadWalletsFromFile()
    {
        filePath = (Application.persistentDataPath + "/" + fileName);

        if (File.Exists(filePath))
        {
            bf = new BinaryFormatter();
            file = File.Open(filePath, FileMode.Open);

            walletList = (List<WalletData>)bf.Deserialize(file);

            file.Close();
        }

        RefreshWalletAccountDropdown();

        logText.Log("Loaded " + walletList.Count + " Wallet/s");
    }

    public void RecepientAddressDropdownSelected()
    {
        if (recepientAddressDropdown.value > 0)
            recepientAddressInputField.text = recepientAddressDropdown.options[recepientAddressDropdown.value].text;

        // reset to none when a value is selected
        recepientAddressDropdown.value = 0;
    }

    public void RefreshRecepientAddressDropdown()
    {
        recepientAddressDropdown.ClearOptions();

        // add default none option
        recepientAddressDropdown.AddOptions(new List<string> { "-" });

        int index = 0;

        foreach (WalletData w in walletList)
        {
            if (index != walletSelectionDropdown.value)
                recepientAddressDropdown.AddOptions(new List<string> { w.address });

            index++;
        }

        recepientAddressDropdown.gameObject.SetActive(recepientAddressDropdown.options.Count > 0);
    }


    public void RefreshWalletAccountDropdown()
    {
        walletSelectionDropdown.ClearOptions();

        foreach (WalletData w in walletList)
        {
            walletSelectionDropdown.AddOptions(new List<string> { w.address });            
        }

        // add wallet create option
        walletSelectionDropdown.AddOptions(new List<string> { "New Wallet" });
    }

    public void RefreshTopPanelView()
    {
        passwordInputField.resetFields();
        recepientAddressInputField.text = "";

        RefreshRecepientAddressDropdown();

        int index = walletSelectionDropdown.value;

        if (index >= walletSelectionDropdown.options.Count - 1)
        {
            createWalletPanel.SetActive(true);
            operationsPanel.SetActive(false);
        }

        else
        {
            createWalletPanel.SetActive(false);
            operationsPanel.SetActive(true);
        }
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


    private void disableOperationPanels()
    {
        createWalletPanel.SetActive(false);
    }

    private void hideLoadingIndicator()
    {
        loadingIndicatorPanel.SetActive(false);
    }

    private void showLoadingIndicator()
    {
        loadingIndicatorPanel.SetActive(true);
    }

    public void CreateWallet()
    {
        if (passwordInputField.passwordConfirmed())
        {
            disableOperationPanels();
            showLoadingIndicator();

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
    public IEnumerator CreateAccountCoroutine(string password, string accountName)
    {
        yield return 0; // allow UI to update

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

            newAccountAdded.Invoke();
            loadingFinished.Invoke();
        });
    }

    // This function will just execute a callback after it creates and encrypt a new account
    public void CreateAccount(string password, System.Action<string, string, byte[]> callback)
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

    // We create the function which will check the balance of the address and return a callback with a decimal variable
    public IEnumerator getAccountBalance(string address, System.Action<decimal> callback)
    {
        // Now we define a new EthGetBalanceUnityRequest and send it the testnet url where we are going to
        // check the address, defined by networkUrl
        // (we get EthGetBalanceUnityRequest from the Netherum lib imported at the start)

        var getBalanceRequest = new EthGetBalanceUnityRequest(networkUrl);
        // Then we call the method SendRequest() from the getBalanceRequest we created
        // with the address and the newest created block.
        yield return getBalanceRequest.SendRequest(address, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

        // Now we check if the request has an exception
        if (getBalanceRequest.Exception == null)
        {
            // We define balance and assign the value that the getBalanceRequest gave us.
            var balance = getBalanceRequest.Result.Value;
            // Finally we execute the callback and we use the Netherum.Util.UnitConversion
            // to convert the balance from WEI to ETHER (that has 18 decimal places)
            callback(Nethereum.Util.UnitConversion.Convert.FromWei(balance, 18));
        }
        else
        {
            // If there was an error we just throw an exception.
            throw new System.InvalidOperationException("Get balance request failed");
        }

    }





}
