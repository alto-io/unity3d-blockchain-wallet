using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.Encoders;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using UnityEngine;


public class PingContractService
{
    // We define the ABI of the contract we are going to use.
    public static string ABI = @"[{""constant"":true,""inputs"":[],""name"":""pings"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""value"",""type"":""uint256""}],""name"":""ping"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""},{""anonymous"":false,""inputs"":[{""indexed"":false,""name"":""pong"",""type"":""uint256""}],""name"":""Pong"",""type"":""event""}]";

    // And we define the contract address here, in this case is a simple ping contract
    // (Remember this contract is deployed on the kovan network)
    // https://kovan.etherscan.io/address/0xc4b054A90676fea7E8CBb8e311fd1ed086A296e1#code
    private static string contractAddress = "0xc4b054A90676fea7E8CBb8e311fd1ed086A296e1";

    // We define a new contract (Netherum.Contracts)
    private Contract contract;

    public PingContractService()
    {
        // Here we assign the contract as a new contract and we send it the ABI and contact address
        this.contract = new Contract(null, ABI, contractAddress);
        // Basically, this contract will add 1 each time you call the ping function,
        // and then emit a pong event with the sum of (total pings) and (sent value).
        // i.e. (first time ever to call the contract) ping 10 => pings = 1; Pong(11);
        // ping 55 => pings = 2; Pong(57); ...
    }

    public Function GetPingFunction()
    {
        return contract.GetFunction("ping");
    }

    public Function GetPingsFunction()
    {
        return contract.GetFunction("pings");
    }

    public TransactionInput CreatePingTransactionInput(
        // For this transaction to the contract we are going to use
        // the address which is excecuting the transaction (addressFrom), 
        // the private key of that address (privateKey),
        // the ping value we are going to send to this contract (pingValue),
        // the maximum amount of gas to consume,
        // the price you are willing to pay per each unit of gas consumed, (higher the price, faster the tx will be included)
        // and the valueAmount in ETH to send to this contract.
        // IMPORTANT: the PingContract doesn't accept eth transfers so this must be 0 or it will throw an error.
        string addressFrom,
        string privateKey,
        BigInteger pingValue,
        HexBigInteger gas = null,
        HexBigInteger gasPrice = null,
        HexBigInteger valueAmount = null)
    {

        var function = GetPingFunction();
        return function.CreateTransactionInput(addressFrom, gas, gasPrice, valueAmount, pingValue);
    }

    public CallInput CreatePingsCallInput()
    {
        // For this transaction to the contract we dont need inputs,
        // its only to retreive the quantity of Ping transactions we did. (the pings variable on the contract)
        var function = GetPingsFunction();
        return function.CreateCallInput();
    }

    public int DecodePings(string pings)
    {
        // We use this function later to parse the result of encoded pings (Hexadecimal 0x0f)
        // into a decoded output for easier readability (Integer 15)
        var function = GetPingsFunction();
        return function.DecodeSimpleTypeOutput<int>(pings);
    }
}