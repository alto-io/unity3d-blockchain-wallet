using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Runtime.InteropServices;
using Nethereum.ABI.Encoders;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Util;
using Nethereum.Signer;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TokenContractService : MonoBehaviour
{
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

    public Text CurrencyInfoText;
    public GameObject LoadingIndicator;


    // We define a new contract (Netherum.Contracts)
    private Contract contract;

    private string _url;

    public void AddInfoText(string text, bool clear = false)
    {
        if (clear)
            CurrencyInfoText.text = "";
        else
            CurrencyInfoText.text += "\n";

        CurrencyInfoText.text += text;

    }

    void Awake()
    {

        // retrieve the url from the Wallet Manager
        _url = WalletManager.Instance.networkUrl;

        // Here we assign the contract as a new contract and we send it the ABI and contact address
        this.contract = new Contract(null, ABI, TokenContractAddress);

        AddInfoText("Token Address: " + TokenContractAddress, true);

        LoadingIndicator.SetActive(true);

        StartCoroutine(GetTokenInfo());

    }
    public IEnumerator GetTokenInfo()
    {
        var wait = 0;
        while (true)
        {
            yield return new WaitForSeconds(wait);
            wait = 20;

            //Create a unity call request (we have a request for each type of rpc operation)
            var topScoreRequest = new EthCallUnityRequest(_url);

            // get token name
            var nameCallInput = CreateNameCallInput();
            yield return topScoreRequest.SendRequest(nameCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

            AddInfoText("Name: " + DecodeName(topScoreRequest.Result));

            /*
            //Use the service to create a call input which includes the encoded  
            var countTopScoresCallInput = CreateCountTopScoresCallInput();
            //Call request sends and yield for response	
            yield return topScoreRequest.SendRequest(countTopScoresCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

            //decode the top score using the service
            var scores = new List<TokenDTO>();

            var count = DecodeTopScoreCount(topScoreRequest.Result);
            for (int i = 0; i < count; i++)
            {
                topScoreRequest = new EthCallUnityRequest(_url);
                var topScoreCallInput = CreateTopScoresCallInput(i);
                yield return topScoreRequest.SendRequest(topScoreCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

                scores.Add(DecodeTopScoreDTO(topScoreRequest.Result));
            }

            var orderedScores = scores.OrderByDescending(x => x.Score).ToList();

            var topScores = "Top Scores" + Environment.NewLine;

            foreach (var score in orderedScores)
            {
                topScores = topScores + score.Score + "-" + score.Addr.Substring(0, 15) + "..." + Environment.NewLine;

            }
           // topScoresAllTimeText.text = topScores;
           */
        }

    }

    public Function GetFunctionTokenName()
    {
        return contract.GetFunction("name");
    }

    public CallInput CreateNameCallInput()
    {
        var
        function = GetFunctionTokenName();
        return function.CreateCallInput();
    }

    public Function GetUserTopScoresFunction()
    {
        return contract.GetFunction("userTopScores");
    }

    public Function GetFunctionTopScores()
    {
        return contract.GetFunction("topScores");
    }

    public Function GetFunctionGetCountTopScores()
    {
        return contract.GetFunction("getCountTopScores");
    }

    public CallInput CreateUserTopScoreCallInput(string userAddress)
    {
        var
        function = GetUserTopScoresFunction();
        return function.CreateCallInput(userAddress);
    }

    public CallInput CreateTopScoresCallInput(BigInteger index)
    {
        var
        function = GetFunctionTopScores();
        return function.CreateCallInput(index);
    }

    public CallInput CreateCountTopScoresCallInput()
    {
        var
        function = GetFunctionGetCountTopScores();
        return function.CreateCallInput();
    }

    public Function GetFunctionSetTopScore()
    {
        return contract.GetFunction("setTopScore");
    }

    public TransactionInput CreateSetTopScoreTransactionInput(string addressFrom, string addressOwner, string privateKey, BigInteger score, HexBigInteger gas = null, HexBigInteger valueAmount = null)
    {
        var numberBytes = new IntTypeEncoder().Encode(score);
        var sha3 = new Nethereum.Util.Sha3Keccack();
        var hash = sha3.CalculateHashFromHex(addressFrom, addressOwner, numberBytes.ToHex());
        var signer = new MessageSigner();
        var signature = signer.Sign(hash.HexToByteArray(), privateKey);
        var ethEcdsa = MessageSigner.ExtractEcdsaSignature(signature);

        var
        function = GetFunctionSetTopScore();
        return function.CreateTransactionInput(addressFrom, gas, valueAmount, score, ethEcdsa.V, ethEcdsa.R, ethEcdsa.S);
    }

    public string DecodeName(string result)
    {
        var
        function = GetFunctionTokenName();
        return function.DecodeSimpleTypeOutput<string>(result);
    }


    public int DecodeUserTopScoreOutput(string result)
    {
        var
        function = GetUserTopScoresFunction();
        return function.DecodeSimpleTypeOutput<int>(result);
    }

    public int DecodeTopScoreCount(string result)
    {
        var
        function = GetFunctionGetCountTopScores();
        return function.DecodeSimpleTypeOutput<int>(result);
    }

    public TokenDTO DecodeTopScoreDTO(string result)
    {
        var
        function = GetFunctionTopScores();
        return function.DecodeDTOTypeOutput<TokenDTO>(result);
    }
}

[FunctionOutput]
public class TokenDTO
{
    [Parameter("address", "addr", 1)]
    public string Addr { get; set; }

    [Parameter("int256", "score", 2)]
    public BigInteger Score { get; set; }

}