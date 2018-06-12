using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mit.Dci.Lit
{
    public class LitClient
    {
        private enum ListeningStatus {
            Unknown = 0,
            Listening = 1,
            NotListening = 2
        }

        private ClientWebSocket _websocket;
        private Uri _litUri;
        private int _requestNonce;
        private Dictionary<int, object> _pendingRequests;
        private CancellationTokenSource _receiveCancellation;
        private ListeningStatus _listeningStatus;

        /// <summary>
        /// Creates a new client talking to a LIT instance
        /// </summary>       
        /// <param name="host">The host to connect to (default: localhost)</param>
        /// <param name="port">The port number to connect to (default: 8001)</param>
        public LitClient(string host = "localhost", int port = 8001) {
            _litUri = new Uri(string.Format("ws://{0}:{1}/ws", host, port));
            _websocket = new ClientWebSocket();
            _websocket.Options.SetRequestHeader("Origin", "http://localhost/");
            _pendingRequests = new Dictionary<int, object>();
            _receiveCancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Opens the connection to LIT
        /// </summary>     
        public async Task Connect() {

            await _websocket.ConnectAsync(_litUri, CancellationToken.None);
            RestartReceiveLoop();
        }

        /// <summary>
        /// Closes the connection to LIT
        /// </summary>     
        public async Task Disconnect() {
            await _websocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
            if(_receiveCancellation != null) { 
                _receiveCancellation.Cancel();
            }
        }

        /// <summary>
        /// Instructs LIT to listen for incoming connections. By default, LIT will not
        /// listen. If LIT was already listening for incoming connections, this method 
        /// will just complete.
        /// </summary>       
        /// <param name="port">The port number to listen on (default: 2448)</param>
        public async Task Listen(int port = 2448) {
            var args = new ListenArgs() { 
                Port = string.Format(":{0}", port) 
            };
            try
            {
                await Call<ListenArgs,ListeningPortsReply>("LitRPC.Listen", args);
                _listeningStatus = ListeningStatus.Listening;
            } catch (Exception ex) {
                // check if it is "already listening" error, then ignore.
                throw ex;
            }
        }

        /// <summary>
        /// Checks if LIT is currently listening on any port.
        /// </summary>       
        /// <returns>true when listening, false when not</returns>
        public async Task<bool> IsListening() {
            if(_listeningStatus != ListeningStatus.Unknown) { 
                return _listeningStatus == ListeningStatus.Listening;
            }
            var reply = await Call<NoArgs,ListeningPortsReply>("LitRPC.GetListeningPorts", new NoArgs());
            _listeningStatus = (reply.LisIpPorts == null) ? ListeningStatus.NotListening : ListeningStatus.Listening;
            return _listeningStatus == ListeningStatus.Listening;
        }

        /// <summary>
        /// Returns the LN address for this node
        /// </summary>       
        /// <returns>LN address of the node</returns>
        public async Task<string> GetLNAddress() {
            var reply = await Call<NoArgs,ListeningPortsReply>("LitRPC.GetListeningPorts", new NoArgs());
            return reply.Adr;
        }

        /// <summary>
        /// Connects to another LIT node
        /// </summary>  
        /// <param name="address">LN address for the node to connect to</param>
        /// <param name="host">The host to connect to. If omitted, LIT will use a node tracker to find the correct host</param>
        /// <param name="port">The port to connect to. If omitted, LIT will use the default port (2448)</param>
        public async Task Connect(string address, string host = "", int port = 2448) {
            var args = new ConnectArgs() {
                LNAddr = address
            };
            if(!string.IsNullOrEmpty(host)) {
                args.LNAddr += "@" + host;
                if(port != 2448) {
                    args.LNAddr += ":" + port.ToString();
                }
            }
            var reply = await Call<ConnectArgs,StatusReply>("LitRPC.Connect", args);
            if(string.IsNullOrEmpty(reply.Status) || !reply.Status.Contains("connected to peer")) {
                throw new ApplicationException("Unexpected reply from server: " + reply.Status);
            }
        }

        /// <summary>
        /// Returns a list of currently connected nodes
        /// </summary>  
        /// <returns>List of connected nodes</returns>
        public async Task<PeerInfo[]> ListConnections() {
            var reply = await Call<NoArgs,ListConnectionsReply>("LitRPC.ListConnections", new NoArgs());
            if(reply.Connections == null) return new PeerInfo[]{};
            else return reply.Connections;
        }

        /// <summary>
        /// Assigns a nickname to a connected peer
        /// </summary>  
        /// <param name="peerIndex">Numeric index of the peer</param>
        /// <param name="nickName">Nickname to assign</param>
        public async Task AssignNickname(int peerIndex, string nickName) {
            var args = new AssignNicknameArgs() {
                Peer = peerIndex,
                Nickname = nickName
            };
            var reply = await Call<AssignNicknameArgs, StatusReply>("LitRPC.AssignNickname", args);
            if(string.IsNullOrEmpty(reply.Status) || !reply.Status.Contains("changed nickname")) {
                throw new ApplicationException("Unexpected reply from server: " + reply.Status);
            }
        }

        /// <summary>
        /// Stops the LIT node. This means you'll have to restart it manually.
        /// After stopping the node you can no longer connect to it via RPC.
        /// </summary>  
        /// <param name="peerIndex">Numeric index of the peer</param>
        /// <param name="nickName">Nickname to assign</param>
        public async Task Stop() {
            var reply = await Call<NoArgs, StatusReply>("LitRPC.Stop", new NoArgs());
            if(string.IsNullOrEmpty(reply.Status) || !reply.Status.Contains("Stopping lit node")) {
                throw new ApplicationException("Unexpected reply from server: " + reply.Status);
            }
        }   
           
        /// <summary>
        /// Returns a list of balances from the LIT node's wallet
        /// </summary>  
        /// <returns>List of connected nodes</returns>
        public async Task<CoinBalReply[]> ListBalances() {
            var reply = await Call<NoArgs,BalanceReply>("LitRPC.Balance", new NoArgs());
            if(reply.Balances == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Balances;
        }

        /// <summary>
        /// Returns a list of all unspent transaction outputs, that are not part of a channel
        /// </summary>  
        /// <returns>List of unspent outputs not part of a channel</returns>
        public async Task<TxoInfo[]> ListUtxos() {
            var reply = await Call<NoArgs,TxoListReply>("LitRPC.TxoList", new NoArgs());
            if(reply.Txos == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Txos;
        }

        /// <summary>
        /// Sends coins from LIT's wallet using a normal on-chain transaction
        /// </summary>  
        /// <param name="address">The address to send the coins to</param>
        /// <param name="amount">The amount (in satoshi) to send</param>
        /// <returns>The transaction ID for the on-chain transaction</returns>
        public async Task<string> Send(string address, long amount) {
            var args = new SendArgs() {
                DestAddrs = new string[] { address },
                Amts = new long[] { amount }
            };
            var reply = await Call<SendArgs,TxidsReply>("LitRPC.Send", args);
            if(reply.Txids == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Txids[0];
        }
        
        /// <summary>
        /// Allows you to configure the fee rate for a particular coin type
        /// </summary>  
        /// <param name="coinType">Numeric coin type</param>
        /// <param name="feePerByte">The amount of satoshi per byte to use as fee</param>
        public async Task SetFee(int coinType, long feePerByte) {
            var args = new SetFeeArgs() { 
                CoinType = coinType, 
                Fee = feePerByte 
            };
            var reply = await Call<SetFeeArgs, FeeReply>("LitRPC.SetFee", args);
            if(reply.CurrentFee != feePerByte) throw new ApplicationException("Fee was not set");
        }

        /// <summary>
        /// Allows you to retrieve the fee rate for a particular coin type
        /// </summary>  
        /// <param name="coinType">Numeric coin type</param>
        public async Task<long> GetFee(int coinType) {
            var args = new GetFeeArgs() { 
                CoinType = coinType
            };
            var reply = await Call<GetFeeArgs, FeeReply>("LitRPC.GetFee", args);
            return reply.CurrentFee;
        }

        /// <summary>
        /// Returns a list of (newly generated or existing) addresses. Returns bech32 by default.
        /// </summary>  
        /// <param name="coinType">Coin type the addresses should be returned for</param>
        /// <param name="numberToMake">The number of new addresses to make. Passing 0 will return all known addresses</param>
        /// <param name="legacy">Return legacy addresses (default: false)</param>
        /// <returns>A string array with the generated or retrieved addresses</returns>
        public async Task<string[]> GetAddresses(int coinType, int numberToMake = 0, bool legacy = false)
        {
            var args = new AddressArgs() { 
                CoinType = coinType,
                NumToMake = numberToMake
            };
            var reply = await Call<AddressArgs, AddressReply>("LitRPC.Addresses", args);
            if(reply.LegacyAddresses == null || reply.WitAddresses == null) {
                throw new ApplicationException("Unexpected reply from server");
            }
            return legacy ? reply.LegacyAddresses : reply.WitAddresses;
        }

        /// <summary>
        /// Returns a list of channels (both active and closed)
        /// </summary>  
        /// <returns>Array of @see ChannelInfo objects containing the known channels</returns>
        public async Task<ChannelInfo[]> ListChannels() {
            var reply = await Call<NoArgs, ChannelListReply>("LitRPC.ChannelList", new NoArgs());
            if(reply.Channels == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Channels;
        }

        /// <summary>
        /// Returns a list of channels (both active and closed)
        /// </summary>  
        /// <param name="peerIndex">The peer to create the channel with</param>
        /// <param name="coinType">Coin type of the channel</param>
        /// <param name="amount">Amount (in satoshi) to fund the channel with</param>
        /// <param name="initialSend">Send this amount over to the peer upon funding</param>
        /// <param name="data">Arbitrary data to attach to the initial channel state. Can be used for referencing payments</param>
        public async Task FundChannel(int peerIndex, int coinType, long amount, long initialSend, byte[] data)
        {
            var args = new FundArgs() {
                Peer = peerIndex,
                CoinType = coinType,
                Capacity = amount,
                InitialSend = initialSend,
                Data = data
            };
            var reply = await Call<FundArgs,StatusReply>("LitRPC.FundChannel", args);
            if(string.IsNullOrEmpty(reply.Status) || !reply.Status.Contains("funded channel")) {
                throw new ApplicationException("Unexpected reply from server: " + reply.Status);
            }
        }

        /// <summary>
        /// Dumps all the known (previous) states to channels. This can be useful when
        /// analyzing payment references periodically. The data of each individual state
        /// is returned in the array of @see JusticeTx objects.
        /// </summary>  
        public async Task<JusticeTx[]> StateDump() {
            var reply = await Call<NoArgs, StateDumpReply>("LitRPC.StateDump", new NoArgs());
            if(reply.Txs == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Txs;
        }
        

        /// <summary>
        /// Pushes funds through the channel to the other peer
        /// </summary>  
        /// <param name="channelIndex">Index of the channel to push funds through</param>
        /// <param name="amount">Amount (in satoshi) to push</param>
        /// <param name="data">Arbitrary data to attach to the push. Can be used for referencing payments</param>
        public async Task<long> Push(int channelIndex, long amount, byte[] data)
        {
            var args = new PushArgs() {
                ChanIdx = channelIndex,
                Amt = amount,
                Data = data
            };
            var reply = await Call<PushArgs,PushReply>("LitRPC.Push", args);
            if(reply.StateIndex == 0) throw new ApplicationException("Unexpected reply from server");
            return reply.StateIndex;
        }

        /// <summary>
        /// Collaboratively closes a channel and returns the funds to the wallet
        /// </summary>  
        /// <param name="channelIndex">The index of the channel to close</param>
        public async Task CloseChannel(int channelIndex) {
            var args = new ChanArgs() {
                ChanIdx = channelIndex
            };
            var reply = await Call<ChanArgs,StatusReply>("LitRPC.CloseChannel", args);
            if(string.IsNullOrEmpty(reply.Status) || !reply.Status.Contains("funded channel")) {
                throw new ApplicationException("Unexpected reply from server: " + reply.Status);
            }
        }

        /// <summary>
        /// Breaks a channel and claims the funds back to our wallet
        /// </summary>  
        /// <param name="channelIndex">The index of the channel to break</param>
        public async Task BreakChannel(int channelIndex) {
            var args = new ChanArgs() {
                ChanIdx = channelIndex
            };
            var reply = await Call<ChanArgs,StatusReply>("LitRPC.BreakChannel", args);
            if(string.IsNullOrEmpty(reply.Status) || !reply.Status.Contains("funded channel")) {
                throw new ApplicationException("Unexpected reply from server: " + reply.Status);
            }
        }
    

        /// <summary>
        /// Imports an oracle that exposes a REST API
        /// </summary>  
        /// <param name="url">The REST endpoint of the oracle</param>
        /// <param name="name">The display name to give the oracle</param>
        public async Task<DlcOracle> ImportOracle(string url, string name) {
            var args = new ImportOracleArgs() {
                Name = name,
                Url = url
            };
            var reply = await Call<ImportOracleArgs,AddOrImportOracleReply>("LitRPC.ImportOracle", args);
            if(reply.Oracle == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Oracle;
        }


        /// <summary>
        /// Adds a new oracle by specifying its public key
        /// </summary>  
        /// <param name="pubKeyHex">The public key of the oracle, 33 bytes hex</param>
        /// <param name="name">The display name to give the oracle</param>
        public async Task<DlcOracle> AddOracle(string pubKeyHex, string name) {
            var args = new AddOracleArgs() {
                Name = name,
                Key = pubKeyHex
            };
            var reply = await Call<AddOracleArgs,AddOrImportOracleReply>("LitRPC.AddOracle", args);
            if(reply.Oracle == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Oracle;
        }

        /// <summary>
        ///  Returns a list of known oracles
        /// </summary>  
        public async Task<DlcOracle[]> ListOracles() {
            var reply = await Call<NoArgs, ListOraclesReply>("LitRPC.ListOracles", new NoArgs());
            if(reply.Oracles == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Oracles;
        }

        /// <summary>
        /// Creates an offer for a new asset forward contract.
        /// </summary>  
        /// <param name="offer">The parameters to create a new forward offer</param>
        public async Task<DlcFwdOffer> NewForwardOffer(DlcFwdOffer offer) {
            var args = new NewForwardOfferArgs() {
                Offer = offer
            };
            var reply = await Call<NewForwardOfferArgs,NewForwardOfferReply>("LitRPC.NewForwardOffer", args);
            if(reply.Offer == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Offer;
        }

        /// <summary>
        /// Returns a list of current offers
        /// </summary>  
        public async Task<DlcFwdOffer[]> ListOffers() {
            var reply = await Call<NoArgs,ListOffersReply>("LitRPC.ListOffers", new NoArgs());
            if(reply.Offers == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Offers;
        }

        /// <summary>
        /// Accepts an offer that was sent to us
        /// </summary>  
        /// <param name="offerIndex">The index of the offer to accept</param>
        public async Task AcceptOffer(int offerIndex) {
            var args = new AcceptDeclineOfferArgs() {
                OIdx = offerIndex
            };
            var reply = await Call<AcceptDeclineOfferArgs,SuccessReply>("LitRPC.AcceptOffer", args);
            if(!reply.Success) throw new ApplicationException("Accepting offer failed");
        }

        /// <summary>
        /// Declines an offer that was sent to us
        /// </summary>  
        /// <param name="offerIndex">The index of the offer to decline</param>
        public async Task DeclineOffer(int offerIndex) {
            var args = new AcceptDeclineOfferArgs() {
                OIdx = offerIndex
            };
            var reply = await Call<AcceptDeclineOfferArgs,SuccessReply>("LitRPC.DeclineOffer", args);
            if(!reply.Success) throw new ApplicationException("Accepting offer failed");
        }

        /// <summary>
        /// Creates a new, empty draft contract
        /// </summary>
        public async Task<DlcContract> NewContract() {
            var reply = await Call<NoArgs,NewGetContractReply>("LitRPC.NewContract", new NoArgs());
            if(reply.Contract == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Contract;
        }

        /// <summary>
        /// Retrieves an existing contract
        /// </summary>
        /// <param name="contractIndex">The index of the contract to retrieve</param>
        public async Task<DlcContract> GetContract(int contractIndex) {
            var args = new GetContractArgs() {
                Idx = contractIndex
            };
            var reply = await Call<GetContractArgs,NewGetContractReply>("LitRPC.GetContract", args);
            if(reply.Contract == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Contract;
        }

        /// <summary>
        /// Returns all known contracts
        /// </summary>
        public async Task<DlcContract[]> ListContracts() {
            var reply = await Call<NoArgs,ListContractsReply>("LitRPC.ListContracts", new NoArgs());
            if(reply.Contracts == null) throw new ApplicationException("Unexpected reply from server");
            return reply.Contracts;
        }


        /// <summary>
        /// Offers a contract to another peer. Contract has to be in draft state
        /// </summary>
        /// <param name="contractIndex">The index of the contract to offer</param>
        /// <param name="peerIndex">The peer to offer the contract to</param>
        public async Task OfferContract(int contractIndex, int peerIndex) {
            var args = new OfferContractArgs() {
                CIdx = contractIndex,
                PeerIdx = peerIndex
            };
            var reply = await Call<OfferContractArgs,SuccessReply>("LitRPC.OfferContract", args);
            if(!reply.Success) throw new ApplicationException("Offering contract failed");
        }

        /// <summary>
        /// Accepts a contract
        /// </summary>
        /// <param name="contractIndex">The index of the contract to accept</param>
        public async Task AcceptContract(int contractIndex) {
            var args = new AcceptOrDeclineContractArgs() {
                CIdx = contractIndex
            };
            var reply = await Call<AcceptOrDeclineContractArgs,SuccessReply>("LitRPC.AcceptContract", args);
            if(!reply.Success) throw new ApplicationException("Accepting contract failed");
        }

        /// <summary>
        /// Declines a contract
        /// </summary>
        /// <param name="contractIndex">The index of the contract to decline</param>
        public async Task DeclineContract(int contractIndex) {
            var args = new AcceptOrDeclineContractArgs() {
                CIdx = contractIndex
            };
            var reply = await Call<AcceptOrDeclineContractArgs,SuccessReply>("LitRPC.DeclineContract", args);
            if(!reply.Success) throw new ApplicationException("Declining contract failed");
        }

        /// <summary>
        /// Settles the contract and claims the funds back to the wallet
        /// </summary>
        /// <param name="contractIndex">The index of the contract to settle</param>
        /// <param name="oracleValue">Oracle value to settle the contract on</param>
        /// <param name="oracleSignature">Signature from the oracle for the value</param>
        public async Task<SettleContractReply> SettleContract(int contractIndex, long oracleValue, byte[] oracleSignature) {
            var args = new SettleContractArgs() {
                CIdx = contractIndex,
                OracleSig = oracleSignature,
                OracleValue = oracleValue
            };
            var reply = await Call<SettleContractArgs,SettleContractReply>("LitRPC.SettleContract", args);
            if(!reply.Success) throw new ApplicationException("Settling contract failed");
            return reply;
        }


        /// <summary>
        /// Defines how the funds are divided based on the oracle's value, following a linear divison.
        /// </summary>
        /// <param name="contractIndex">The index of the contract to specify the division for</param>
        /// <param name="valueFullyOurs">The value (threshold) at which all the money in the contract is for us</param>
        /// <param name="valueFullyTheirs">The value (threshold) at which all the money in the contract is for our counter party</param>
        public async Task SetContractDivision(int contractIndex, long valueFullyOurs, long valueFullyTheirs) {
            var args = new SetContractDivisionArgs() {
                CIdx = contractIndex,
                ValueFullyOurs = valueFullyOurs,
                ValueFullyTheirs = valueFullyTheirs
            };
            var reply = await Call<SetContractDivisionArgs,SuccessReply>("LitRPC.SetContractDivision", args);
            if(!reply.Success) throw new ApplicationException("Setting contract divisionfailed");
        }


        /// <summary>
        /// Specifies which coin type to use for the contract. This cointype must be available or the server will return an error.
        /// </summary>
        /// <param name="contractIndex">The index of the contract to specify the cointype for</param>
        /// <param name="coinType">The coin type to use for the contract</param>
        public async Task SetContractCoinType(int contractIndex, int coinType) {
            var args = new SetContractCoinTypeArgs() {
                CIdx = contractIndex,
                CoinType = coinType
            };
            var reply = await Call<SetContractCoinTypeArgs,SuccessReply>("LitRPC.SetContractCoinType", args);
            if(!reply.Success) throw new ApplicationException("Setting contract division failed");
        }

        /// <summary>
        /// Describes how the funding of the contract is supposed to happen
        /// </summary>
        /// <param name="contractIndex">The index of the contract to define the funding for</param>
        /// <param name="ourAmount">The amount (in satoshi) we are going to fund</param>
        /// <param name="theirAmount">The amount (in satoshi) we expect our counter party to fund</param>
        public async Task SetContractFunding(int contractIndex, long ourAmount, long theirAmount) {
            var args = new SetContractFundingArgs() {
                CIdx = contractIndex,
                OurAmount = ourAmount,
                TheirAmount = theirAmount
            };
            var reply = await Call<SetContractFundingArgs,SuccessReply>("LitRPC.SetContractFunding", args);
            if(!reply.Success) throw new ApplicationException("Setting contract funding failed");
        }

        /// <summary>
        /// Sets the time the contract is supposed to settle
        /// </summary>
        /// <param name="contractIndex">The index of the contract to specify the settlement time for</param>
        /// <param name="settlementTime">The time (unix timestamp) when the contract is supposed to settle</param>
        public async Task SetContractSettlementTime(int contractIndex, int settlementTime) {
            var args = new SetContractSettlementTimeArgs() {
                CIdx = contractIndex,
                Time = settlementTime
            };
            var reply = await Call<SetContractSettlementTimeArgs,SuccessReply>("LitRPC.SetContractSettlementTime", args);
            if(!reply.Success) throw new ApplicationException("Setting contract settlement time failed");
        }

        /// <summary>
        /// Set the public key of the R-point the oracle will use to sign the message with that is used
        /// to divide the funds in this contract
        /// </summary>
        /// <param name="contractIndex">The index of the contract to specify the r-point for</param>
        /// <param name="rPoint">The public key of the R-Point</param>
        public async Task SetContractRPoint(int contractIndex, byte[] rPoint) {
            var args = new SetContractRPointArgs() {
                CIdx = contractIndex,
                RPoint = rPoint
            };
            var reply = await Call<SetContractRPointArgs,SuccessReply>("LitRPC.SetContractRPoint", args);
            if(!reply.Success) throw new ApplicationException("Setting contract r-point failed");
        }

        /// <summary>
        /// Configures a contract to use a specific oracle. You need to import the oracle first.
        /// </summary>
        /// <param name="contractIndex">The index of the contract to specify the oracle for</param>
        /// <param name="oracleIndex">The index of the oracle to use</param>
        public async Task SetContractOracle(int contractIndex, int oracleIndex) {
            var args = new SetContractOracleArgs() {
                CIdx = contractIndex,
                OIdx = oracleIndex
            };
            var reply = await Call<SetContractOracleArgs,SuccessReply>("LitRPC.SetContractOracle", args);
            if(!reply.Success) throw new ApplicationException("Setting contract oracle failed");
        }

        private void RestartReceiveLoop() {
            if(_receiveCancellation != null) { 
                _receiveCancellation.Cancel();
                _receiveCancellation = new CancellationTokenSource();
            }
            var cancellationToken = _receiveCancellation.Token;
            
            Task.Run(async () => {
                while(!cancellationToken.IsCancellationRequested) {
                    using(var memStream = new MemoryStream()) {
                        byte[] data = new byte[4096];
                        ArraySegment<byte> buffer = new ArraySegment<byte>(data);
                        WebSocketReceiveResult result;
                        do {
                            result = await _websocket.ReceiveAsync(buffer, cancellationToken);
                            if (result.Count > 0)
                            {
                                memStream.Write(buffer.Array, buffer.Offset, result.Count);
                            }
                            else
                            {
                                break;
                            }
                        } while (!result.EndOfMessage); 
                        memStream.Seek(0, SeekOrigin.Begin);

                        // Parse json here.
                        string json = string.Empty;
                        using(var sr = new StreamReader(memStream)) {
                            json = sr.ReadToEnd();
                        }

                        Console.WriteLine("Incoming JSON Response:\r\n==========\r\n{0}\r\n==========\r\n", json);
                        
                        JObject resultJson = JObject.Parse(json);
                        var id = resultJson["id"].Value<int>();
                        if(id == 0 || !_pendingRequests.ContainsKey(id)) {
                            // Ignore!
                        } else {
                            var tcs = _pendingRequests[id];
                            var t = tcs.GetType();
                            Type[] typeParameters = t.GetGenericArguments();
                            
                            Console.WriteLine("Type: {0}<{1}>", t.Name, typeParameters[0].Name);
                            
                            if(!resultJson["error"].IsNullOrEmpty()) {
                                MethodInfo methodInfo = t.GetMethod("SetException");
                                methodInfo.Invoke(tcs, new object[] { new ApplicationException(resultJson["error"].Value<string>()) });
                                
                            } else {
                                MethodInfo methodInfo = t.GetMethod("SetResult");
                                var resultObj = new object[] { JsonConvert.DeserializeObject(resultJson["result"].ToString(), typeParameters[0]) };
                                methodInfo.Invoke(tcs, resultObj);
                            }
                        }
                    }
                }
            }, cancellationToken);
        }

        private async Task<TReply> Call<TRequest,TReply>(string method, TRequest request)
        {
            int id = ++_requestNonce;
            var promise = new TaskCompletionSource<TReply>();
            
            _pendingRequests[id] = promise;

            JObject call = new JObject();
            call.Add(new JProperty("id", id));
            call.Add(new JProperty("method", method));
            var parameters = new JArray();
            parameters.Add(JObject.FromObject(request));
            call.Add(new JProperty("params", parameters));
            Console.WriteLine("Outgoing JSON Call:\r\n==========\r\n{0}\r\n==========\r\n", call.ToString());
            var msg = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(call.ToString()));
            await _websocket.SendAsync(msg, WebSocketMessageType.Binary, true, CancellationToken.None);

            return await promise.Task;
        }
    }
}
