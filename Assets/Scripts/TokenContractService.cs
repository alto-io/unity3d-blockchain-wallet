using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.Util;
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

    // We define a new contract (Netherum.Contracts)
    private Contract contract;

    private string _url;
    //Service to generate, encode and decode CallInput and TransactionInpput
    //This includes the contract address, abi,, etc, similar to a generic Nethereum services
    private ScoreContractService _scoreContractService;

    void Awake()
    {
        // Here we assign the contract as a new contract and we send it the ABI and contact address
        this.contract = new Contract(null, ABI, TokenContractAddress);

        // retrieve the url from the Wallet Manager
        _url = WalletManager.Instance.networkUrl;


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

            //Use the service to create a call input which includes the encoded  
            var countTopScoresCallInput = _scoreContractService.CreateCountTopScoresCallInput();
            //Call request sends and yield for response	
            yield return topScoreRequest.SendRequest(countTopScoresCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

            //decode the top score using the service
            var scores = new List<TopScoresDTO>();

            var count = _scoreContractService.DecodeTopScoreCount(topScoreRequest.Result);
            for (int i = 0; i < count; i++)
            {
                topScoreRequest = new EthCallUnityRequest(_url);
                var topScoreCallInput = _scoreContractService.CreateTopScoresCallInput(i);
                yield return topScoreRequest.SendRequest(topScoreCallInput, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

                scores.Add(_scoreContractService.DecodeTopScoreDTO(topScoreRequest.Result));
            }

            var orderedScores = scores.OrderByDescending(x => x.Score).ToList();

            var topScores = "Top Scores" + Environment.NewLine;

            foreach (var score in orderedScores)
            {
                topScores = topScores + score.Score + "-" + score.Addr.Substring(0, 15) + "..." + Environment.NewLine;

            }
           // topScoresAllTimeText.text = topScores;
        }

    }


}