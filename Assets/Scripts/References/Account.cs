using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.Hex.HexTypes;
using System;

public class Account : MonoBehaviour
{

    // Here we define accountAddress (the public key; We are going to extract it later from the private key)
    private string accountAddress;
    // This is the secret key of the address, you should put it yours.
    private string accountPrivateKey = "0xe11e11e11e11e11e11e11e11e11e11e11e11e11e11e11e11e11e11e11e11e11e";
    // This is the testnet we are going to use for our contract, in this case kovan
    private string _url = "https://kovan.infura.io";

    // We define a new PingContractService (this is the file we are going to create for our contract)
    private PingContractService pingContractService = new PingContractService();

    // Use this for initialization
    void Start()
    {
        // First we'll call this function which will extract and assign the public key from the accountPrivateKey defined above.
        importAccountFromPrivateKey();

        // Then we call getPings to display how many pings we did to the PingFunction. 
        // This will only read from the blockchain thus consuming 0 gas. (it's free!)
        StartCoroutine(getPings());

        // After this we call the PingTransaction function to actually interact with the contract.
        // This function will create a new transaction to our contract, consuming gas to pay for it's computational costs.
        StartCoroutine(PingTransaction());

        StartCoroutine(getAccountBalance(accountAddress, (balance) => {
            Debug.Log("Account balance: " + balance);
        }));

        // Disables new account creation to speed up and simplify the demo.
        // Feel free to remove the if or just change false to true to create a new account.
        // Code Comments for account creation and balance checks are available here:
        // https://gist.github.com/e11io/88f0ae5831f3aa31651f735278b5b463
        var createNewAccount = false;
        if (createNewAccount == true)
        {
            CreateAccount("strong_password", (address, encryptedJson) => {
                Debug.Log(address);
                Debug.Log(encryptedJson);
                StartCoroutine(getAccountBalance(address, (balance) => {
                    Debug.Log(balance); // This will always give you 0, except if you are imposibly lucky.
                }));
            });
        }

    }

    public IEnumerator getPings()
    {
        // We create a new pingsRequest as a new Eth Call Unity Request
        var pingsRequest = new EthCallUnityRequest(_url);

        var pingsCallInput = pingContractService.CreatePingsCallInput();
        Debug.Log("Getting pings...");
        // Then we send the request with the pingsCallInput and the most recent block mined to check.
        // And we wait for the response...
        yield return pingsRequest.SendRequest(pingsCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

        if (pingsRequest.Exception == null)
        {
            // If we don't have exceptions we just display the raw result and the
            // result decode it with our function (decodePings) from the service, congrats!
            Debug.Log("Pings (HEX): " + pingsRequest.Result);
            Debug.Log("Pings (INT):" + pingContractService.DecodePings(pingsRequest.Result));
        }
        else
        {
            // if we had an error in the UnityRequest we just display the Exception error
            Debug.Log("Error submitting getPings tx: " + pingsRequest.Exception.Message);
        }
    }

    public IEnumerator PingTransaction()
    {
        // Create the transaction input with encoded values for the function
        // We will need, the public key (accountAddress), the private key (accountPrivateKey),
        // the pingValue we are going to send to our contract (10000),
        // the gas amount (50000 in this case),
        // the gas price (25), (you can send a gas price of null to get the default value)
        // and the ammount of ethers you want to transfer, this contract doesn't receive ethereum transfers,
        // so we set it to 0, you can modify it and see how it fails.
        var transactionInput = pingContractService.CreatePingTransactionInput(
            accountAddress,
            accountPrivateKey,
            new HexBigInteger(10000),
            new HexBigInteger(50000),
            new HexBigInteger(25),
            new HexBigInteger(0)
        );

        // Here we create a new signed transaction Unity Request with the url, private key, and the user address we get before
        // (this will sign the transaction automatically :D )
        var transactionSignedRequest = new TransactionSignedUnityRequest(_url, accountPrivateKey, accountAddress);

        // Then we send it and wait
        Debug.Log("Sending Ping transaction...");
        yield return transactionSignedRequest.SignAndSendTransaction(transactionInput);
        if (transactionSignedRequest.Exception == null)
        {
            // If we don't have exceptions we just display the result, congrats!
            Debug.Log("Ping tx submitted: " + transactionSignedRequest.Result);
        }
        else
        {
            // if we had an error in the UnityRequest we just display the Exception error
            Debug.Log("Error submitting Ping tx: " + transactionSignedRequest.Exception.Message);
        }
    }


    public void importAccountFromPrivateKey()
    {
        // Here we try to get the public address from the secretKey we defined
        try
        {
            var address = Nethereum.Signer.EthECKey.GetPublicAddress(accountPrivateKey);
            // Then we define the accountAdress private variable with the public key
            accountAddress = address;
        }
        catch (Exception e)
        {
            // If we catch some error when getting the public address we just display the exception in the console
            Debug.Log("Error importing account from PrivateKey: " + e);
        }
    }



    // Code Comments for account creation and balance checks are available here:
    // https://gist.github.com/e11io/88f0ae5831f3aa31651f735278b5b463
    public void CreateAccount(string password, System.Action<string, string> callback)
    {
        var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
        var address = ecKey.GetPublicAddress();
        var privateKey = ecKey.GetPrivateKeyAsBytes();

        var addr = Nethereum.Signer.EthECKey.GetPublicAddress(privateKey.ToString());
        var keystoreservice = new Nethereum.KeyStore.KeyStoreService();
        var encryptedJson = keystoreservice.EncryptAndGenerateDefaultKeyStoreAsJson(password, privateKey, address);

        callback(address, encryptedJson);
    }

    public IEnumerator getAccountBalance(string address, System.Action<decimal> callback)
    {
        var getBalanceRequest = new EthGetBalanceUnityRequest(_url);
        yield return getBalanceRequest.SendRequest(address, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());
        if (getBalanceRequest.Exception == null)
        {
            var balance = getBalanceRequest.Result.Value;
            callback(Nethereum.Util.UnitConversion.Convert.FromWei(balance, 18));
        }
        else
        {
            throw new System.InvalidOperationException("Get balance request failed");
        }

    }

}