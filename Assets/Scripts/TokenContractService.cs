using System.Collections;
using System.Numerics;
using Nethereum.Contracts;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using UnityEngine;
using UnityEngine.UI;

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

    public IEnumerator GetTokenInfo()
    {
        var wait = 0;

        yield return new WaitForSeconds(wait);
        wait = 20;

        //Create a unity call request (we have a request for each type of rpc operation)
        var currencyInfoRequest = new EthCallUnityRequest(_url);


        // get token name (string)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("name"), BlockParameter.CreateLatest());
        TokenInfo.name = DecodeVariable<string>("name", currencyInfoRequest.Result);

        // get token symbol (string)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("symbol"), BlockParameter.CreateLatest());
        TokenInfo.symbol = DecodeVariable<string>("symbol", currencyInfoRequest.Result);

        // get token totalSupply (uint 256)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("totalSupply"), BlockParameter.CreateLatest());
        TokenInfo.totalSupply = DecodeVariable<BigInteger>("totalSupply", currencyInfoRequest.Result);

        // get token decimal places (uint 8)
        yield return currencyInfoRequest.SendRequest(CreateCallInput("decimals"), BlockParameter.CreateLatest());
        TokenInfo.decimals = DecodeVariable<int>("decimals", currencyInfoRequest.Result);


        WalletManager.Instance.AddInfoText("Token Address: \n" + TokenContractAddress, true);
        WalletManager.Instance.AddInfoText("Name: " + TokenInfo.name + " (" + TokenInfo.symbol + ")");
        WalletManager.Instance.AddInfoText("Decimals: " + TokenInfo.decimals);
        WalletManager.Instance.AddInfoText("Total Supply: " + UnitConversion.Convert.FromWei(TokenInfo.totalSupply, TokenInfo.decimals) + " " + TokenInfo.symbol);

        WalletManager.Instance.LoadingIndicator.SetActive(false);
    }

}