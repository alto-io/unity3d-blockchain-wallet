using System.Collections;
using System.Numerics;
using System.Text;
using Nethereum.Contracts;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using UnityEngine;
using Nethereum.ABI.Encoders;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;

[System.Serializable]
public struct TokenInfo
{
    public string name;
    public string symbol;
    public int decimals;
    public BigInteger totalSupply;
};

public class TokenContractService : MonoBehaviour
{

    // create class as singleton
    private static TokenContractService instance;
    public static TokenContractService Instance { get { return instance; } }
    public void OnDestroy() { if (instance == this) instance = null; }

    // this class uses the token contract source found at https://www.ethereum.org/token

    // We define the ABI of the contract we are going to use. One way to get this is by
    // going to remix IDE and view contract details
    public string ABI;

    // example ABI:
    // @"[{""constant"":true, ""inputs"":[],""name"":""name"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""totalSupply"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""decimals"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_value"",""type"":""uint256""}],""name"":""burn"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":"""",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""burnFrom"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""},{""name"":""_extraData"",""type"":""bytes""}],""name"":""approveAndCall"",""outputs"":[{""name"":""success"",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":"""",""type"":""address""},{""name"":"""",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""inputs"":[{""name"":""initialSupply"",""type"":""uint256""},{""name"":""tokenName"",""type"":""string""},{""name"":""tokenSymbol"",""type"":""string""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""constructor""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""from"",""type"":""address""},{""indexed"":true,""name"":""to"",""type"":""address""},{""indexed"":false,""name"":""value"",""type"":""uint256""}],""name"":""Transfer"",""type"":""event""},{""anonymous"":false,""inputs"":[{""indexed"":true,""name"":""from"",""type"":""address""},{""indexed"":false,""name"":""value"",""type"":""uint256""}],""name"":""Burn"",""type"":""event""}]";

    public string TokenContractAddress;

    // example address:
    // "0x1d8338c008fede66018b71b70956766e41da34ea";
    // Etherscan info: https://ropsten.etherscan.io/address/0x1d8338c008fede66018b71b70956766e41da34ea


    // We define a new contract (Netherum.Contracts)
    private Contract contract;

    private string _url;


    public TokenInfo TokenInfo = new TokenInfo();



    void Awake()
    {
        if (instance == null) instance = this;

        // retrieve the url from the Wallet Manager
        _url = WalletManager.Instance.networkUrl;

        TokenInfo.symbol = "???";
        TokenInfo.decimals = 18; // add default values. TODO: make sure these are populated before checking balance

        // Here we assign the contract as a new contract and we send it the ABI and contact address
        this.contract = new Contract(null, ABI, TokenContractAddress);

        WalletManager.Instance.AddInfoText("[Loading Token Info...]", true);
        WalletManager.Instance.LoadingIndicator.SetActive(true);

        StartCoroutine(GetTokenInfo());

    }

    public void SendFundsButtonPressed()
    {
        WalletData wd = WalletManager.Instance.GetSelectedWalletData();

        if (wd != null)
        {
            StartCoroutine(SendFunds(wd.address, WalletManager.Instance.recepientAddressInputField.text, wd.privateKey,
                WalletManager.Instance.fundTransferAmountInputField.text));
        }

        else
        {
            WalletManager.Instance.logText.Log("No Wallet Account Found");
        }
    }

    public CallInput CreateCallInput(string variableName, params object[] p)
    {
        var function = contract.GetFunction(variableName);
        return function.CreateCallInput(p);
    }

    public T DecodeVariable<T>(string variableName, string result)
    {
        var function = contract.GetFunction(variableName);
        try
        {
            return function.DecodeSimpleTypeOutput<T>(result); // this results in an error if BigInteger is 0
        }
        catch
        {
            return default(T);
        }
    }

    public TransactionInput CreateTransferFundsTransactionInput(
         // For this transaction to the contract we are going to use
         // the address which is excecuting the transaction (addressFrom), 
         // the address which to receive the transfer (addressTo), 
         // the private key of that address (privateKey),
         // the ping value we are going to send to this contract (pingValue),
         // the maximum amount of gas to consume,
         // the price you are willing to pay per each unit of gas consumed, (higher the price, faster the tx will be included)
         // and the valueAmount in ETH to send to this contract.
         // IMPORTANT: the contract doesn't accept eth transfers so this must be 0 or it will throw an error.
         string addressFrom,
         string addressTo,
         string privateKey,
         BigInteger transferAmount,
         HexBigInteger gas = null,
         HexBigInteger gasPrice = null,
         HexBigInteger valueAmount = null)
    {

        var function = contract.GetFunction("transfer");
        return function.CreateTransactionInput(addressFrom, gas, gasPrice, valueAmount, addressTo, transferAmount);
    }

    public IEnumerator SendFunds(string addressFrom, string addressTo, string accountPrivateKey, string transferAmount)
    {
        // Create the transaction input with encoded values for the function
        // We will need, the public key of sender and receiver (addressFrom, addressTo),the private key (accountPrivateKey),
        // the amount we are going to send to our contract,
        // the gas amount (500000 in this case),
        // the gas price (10), (you can send a gas price of null to get the default value)
        // and the ammount of ethers you want to transfer, this contract doesn't receive ethereum transfers,
        // so we set it to 0, you can modify it and see how it fails.
        var transactionInput = CreateTransferFundsTransactionInput(
            addressFrom,
            addressTo,
            accountPrivateKey,
            BigInteger.Parse(transferAmount),
            new HexBigInteger(500000),
            new HexBigInteger(10),
            null
        );

        // Here we create a new signed transaction Unity Request with the url, private key, and the user address we get before
        // (this will sign the transaction automatically :D )
        var transactionSignedRequest = new TransactionSignedUnityRequest(WalletManager.Instance.networkUrl, 
            accountPrivateKey, addressFrom);

        // Then we send it and wait
        WalletManager.Instance.logText.Log("Sending fund transfer transaction...");
        yield return transactionSignedRequest.SignAndSendTransaction(transactionInput);
        if (transactionSignedRequest.Exception == null)
        {
            // If we don't have exceptions we just display the result, congrats!
            WalletManager.Instance.logText.Log("transfer tx submitted: " + transactionSignedRequest.Result);
            WalletManager.Instance.CopyToClipboard(transactionSignedRequest.Result);

            // TODO: replace prefix of url derived from network url
            Application.OpenURL("https://ropsten.etherscan.io/tx/" + transactionSignedRequest.Result);
        }
        else
        {
            // if we had an error in the UnityRequest we just display the Exception error
            Debug.Log("Error submitting transfer tx: " + transactionSignedRequest.Exception.Message);
        }
    }



    public IEnumerator GetTokenInfo()
    {
        //Create a unity call request (we have a request for each type of rpc operation)
        var currencyInfoRequest = new EthCallUnityRequest(_url);

        // get token symbol (string)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("symbol"), BlockParameter.CreateLatest());
        TokenInfo.symbol = DecodeVariable<string>("symbol", currencyInfoRequest.Result);

        // get token decimal places (uint 8)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("decimals"), BlockParameter.CreateLatest());
        TokenInfo.decimals = DecodeVariable<int>("decimals", currencyInfoRequest.Result);

        // get token name (string)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("name"), BlockParameter.CreateLatest());
        TokenInfo.name = DecodeVariable<string>("name", currencyInfoRequest.Result);

        // get token totalSupply (uint 256)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("totalSupply"), BlockParameter.CreateLatest());
        TokenInfo.totalSupply = DecodeVariable<BigInteger>("totalSupply", currencyInfoRequest.Result);    

        WalletManager.Instance.AddInfoText("Token Address: \n" + TokenContractAddress, true);
        WalletManager.Instance.AddInfoText("Name: " + TokenInfo.name + " (" + TokenInfo.symbol + ")");
        WalletManager.Instance.AddInfoText("Decimals: " + TokenInfo.decimals);
        WalletManager.Instance.AddInfoText("Total Supply: " + UnitConversion.Convert.FromWei(TokenInfo.totalSupply, TokenInfo.decimals) + " " + TokenInfo.symbol);

        WalletManager.Instance.LoadingIndicator.SetActive(false);
    }

}