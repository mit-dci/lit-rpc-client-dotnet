using System;
using Newtonsoft.Json;

namespace Mit.Dci.Lit
{
    public class NoArgs {

    }

    public class ListenArgs {
        public string Port { get; set;}
    }

    public class ListeningPortsReply {
        public string[] LisIpPorts { get; set;}
        public string Adr { get; set;}
    }

    public class TxidsReply {
        public string[] Txids { get; set;}
    }
    public class StatusReply {
        public string Status { get; set;}
    }

    public class CoinArgs {
        public UInt32 CoinType { get; set;}
    }

    public class ConnectArgs {
        public string LNAddr { get; set;}
    }

    /**
    * Describes the information of a connected peer
    */
    public class PeerInfo {
        /**
        * The (unique) peer number for this peer
        */
        public UInt32 PeerNumber { get; set;}
        /**
        * The remote endpoint we connected to
        */
        public string RemoteHost { get; set;}
        /**
        * The nickname for this peer
        */
        public string Nickname { get; set;}
    }

    public class ListConnectionsReply {
        public PeerInfo[] Connections { get; set;}
        public string MyPKH { get; set;}
    }

    public class AssignNicknameArgs {
        public UInt32 Peer { get; set;}
        public string Nickname { get; set;}
    }

    public class CoinBalReply {
        public UInt32 CoinType { get; set;}
        public long SyncHeight { get; set;}
        public long ChanTotal { get; set;}
        public long TxoTotal { get; set;}
        public long MatureWitty { get; set;}
        public long FeeRate { get; set;}
    }

    public class BalanceReply {
        public CoinBalReply[] Balances { get; set;} 
    }

    public class TxoInfo  {
        public string OutPoint { get; set; }
        public long Am { get; set; } 
        public long Height { get; set; } 
        public long Delay { get; set; } 
        public string CoinType { get; set; } 
        public bool Witty { get; set; } 
        public string KeyPath { get; set; } 
    }
    public class TxoListReply  {
        public TxoInfo[] Txos { get; set; } 
    }

    public class SendArgs {
        public string[] DestAddrs { get; set; } 
        public long[] Amts { get; set; }
    }

    public class  SetFeeArgs {
        public long Fee { get; set; } 
        public UInt32 CoinType { get; set; } 
    }

    public class GetFeeArgs {
        public UInt32 CoinType { get; set; } 
    }
    public class FeeReply {
        public int CurrentFee { get; set; } 
    }

    public class AddressArgs {
        public int NumToMake { get; set; } 
        public UInt32 CoinType { get; set; } 
    }

    public class AddressReply {
        public string[] WitAddresses { get; set; } 
        public string[] LegacyAddresses { get; set; }
    }

    public class ChannelInfo {
        public string OutPoint { get; set; }   
        public UInt32 CoinType { get; set; }   
        public bool Closed { get; set; }   
        public long Capacity { get; set; }   
        public long MyBalance { get; set; }   
        public long Height { get; set; }   
        public long StateNum { get; set; }    
        public int PeerIdx { get; set; }   
        public int CIdx { get; set; }   
        public string PeerID { get; set; } 
        [JsonConverter(typeof(ByteArrayConverter))] 
        public byte[] Data { get; set; }  
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Pkh { get; set; }   
    }

    public class ChannelListReply {
        public ChannelInfo[] Channels { get; set; } 
    }

    // ------------------------- fund
    public class FundArgs {
        public int Peer { get; set; } 
        public UInt32 CoinType { get; set; } 
        public long Capacity { get; set; } 
        public long Roundup { get; set; } 
        public long InitialSend { get; set; } 
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; } 
    }

    public class JusticeTx {
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Sig { get; set; }   
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Txid { get; set; }   
        public long Amt { get; set; }   
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; } 
        [JsonConverter(typeof(ByteArrayConverter))] 
        public byte[] Pkh { get; set; }  
        public int Idx { get; set; }   
    }

    public class StateDumpReply {
        public JusticeTx[] Txs { get; set; } 
    }

    public class PushArgs {
        public int ChanIdx { get; set; }
        public long Amt { get; set; } 
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data  { get; set; }
    }
    public class PushReply {
        public long StateIndex { get; set; }
    }

    public class ChanArgs {
        public int ChanIdx { get; set; }
    }

    public class DlcOracle {
        /**
        * Index of the oracle for refencing in commands
        */
        public int Idx { get; set; } 
        
        /**
        * Public key of the oracle
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] A { get; set; }  
        
        /**
        * Name of the oracle for display purposes
        */
        public string Name { get; set; }  
        
        /**
        * Base URL of the oracle, if its REST based (optional)
        */
        public string Url { get; set; }    
    }

    public class ImportOracleArgs {
        public string Url { get; set; }  
        public string Name { get; set; } 
    }

    public class AddOrImportOracleReply {
        public DlcOracle Oracle { get; set; } 
    }

    public class AddOracleArgs {
        public string Key { get; set; }  
        public string Name { get; set; } 
    }

    public class ListOraclesReply {
        public DlcOracle[] Oracles { get; set; } 
    }

    public class DlcContractDivision {
        public long OracleValue { get; set; } 
        public long ValueOurs { get; set; } 
    }

    /**
    * DlcFwdOffer is an offer for a specific contract template: it is 
    * a bitcoin (or other coin) settled forward, which is symmetrically 
    * funded
    */
    public class DlcFwdOffer {
        /**
        * Convenience definition for serialization from RPC
        */
        public int OType { get; set; } 
        /**
        * Index of the offer
        */
        public int OIdx { get; set; } 
        /**
        * Index of the offer on the other peer
        */
        public int TheirOIdx { get; set; } 
        /**
        * Index of the peer offering to / from
        */
        public int PeerIdx { get; set; } 
        /**
        * Coin type
        */
        public UInt32 CoinType { get; set; } 
        /**
        * Pub key of the oracle used in the contract
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OracleA { get; set; } 
        /**
        * Pub key of the R point (one-time signing key) used in the contract
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OracleR { get; set; }
        /**
        * Time of expected settlement
        */
        public int SettlementTime { get; set; } 
        /**
        * Amount of funding (in satoshi) each party contributes
        */
        public long FundAmt { get; set; } 
        /**
        * Slice of my payouts for given oracle values
        */
        public DlcContractDivision[] Payouts { get; set; } 
        /**
        * If true, I'm the 'buyer' of the foward asset (and I'm short bitcoin)
        */
        public bool ImBuyer { get; set; } 
        /**
        * Amount of asset to be delivered at settlement time.
        * Note that initial price is FundAmt / AssetQuantity
        */
        public long AssetQuantity { get; set; } 

        /**
        * Stores if the offer was accepted. When receiving a matching
        * Contract draft, it will be automatically accepted
        */
        public bool Accepted { get; set; } 
    }

    public enum DlcContractStatus
    {
        ContractStatusDraft         = 0,
        ContractStatusOfferedByMe   = 1,
        ContractStatusOfferedToMe   = 2,
        ContractStatusDeclined      = 3,
        ContractStatusAccepted      = 4,
        ContractStatusAcknowledged  = 5,
        ContractStatusActive        = 6,
        ContractStatusSettling      = 7,
        ContractStatusClosed        = 8
    }

    /**
    * DlcContract is a struct containing all elements to work with a Discreet 
    * Log Contract. This struct is stored in the database of LIT
    */
    public class DlcContract {
        /**
        * Index of the contract for referencing in commands
        */
        public int Idx { get; set; } 
        /**
        * Index of the contract on the other peer (so we can reference it in
        * messages)
        */
        public int TheirIdx { get; set; } 
        /**
        * Index of the peer we've offered the contract to or received the contract 
        * from
        */
        public int PeerIdx { get; set; } 
        
        /**
        * Coin type
        */
        public UInt32 CoinType { get; set; } 
        /**
        * Pub key of the oracle used in the contract
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OracleA { get; set; } 
        /**
        * Pub key of the R point (one-time signing key) used in the contract
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OracleR { get; set; } 
        /** 
        * The time we expect the oracle to publish
        */
        public long OracleTimestamp { get; set; } 
        /** 
        * The payout specification
        */
        public DlcContractDivision[] Division { get; set; } 
        /**
        * The amount (in satoshi) we are funding
        */
        public long OurFundingAmount { get; set; } 
        /**
        * The amount (in satoshi) our counter party is funding
        */
        public long TheirFundingAmount { get; set; } 
        /**
        * PKH to which our part of the contracts funding change should go
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OurChangePKH { get; set; } 
        /**
        * PKH to which the counter party's part of the contracts funding change should go
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] TheirChangePKH { get; set; } 
        /**
        * Our Pubkey used in the funding multisig output
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[]  OurFundMultisigPub { get; set; } 
        /**
        * Counter party's pubkey used in the funding multisig output
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] TheirFundMultisigPub { get; set; } 
        /**
        * Our pubkey to be used in the commit script (combined with oracle pubkey or CSV timeout)
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OurPayoutBase { get; set; } 
        /**
        * Our pubkey to be used in the commit script (combined with oracle pubkey or CSV timeout)
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] TheirPayoutBase { get; set; } 
        /**
        * Our Pubkeyhash to which the contract pays out (directly)
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OurPayoutPKH { get; set; } 
        /**
        * Counterparty's Pubkeyhash to which the contract pays out (directly)
        */  
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] TheirPayoutPKH { get; set; } 
        /**
        * Status of the contract
        */
        public DlcContractStatus Status { get; set; } 
        /** 
        * Our outpoints used to fund the contract
        */
        public DlcContractFundingInput[] OurFundingInputs { get; set; } 
        /** 
        * Counter party's outpoints used to fund the contract
        */
        public DlcContractFundingInput[] TheirFundingInputs { get; set; } 
        /**
        * Signatures for the settlement transactions
        */
        public DlcContractSettlementSignature[] TheirSettlementSignatures { get; set; } 
        /**
        * The outpoint of the funding TX we want to spend in the settlement
        * for easier monitoring
        */
        public OutPoint FundingOutpoint { get; set; } 
    }

    /**
    * DlcContractFundingInput describes a UTXO that is offered to fund the
    * contract with
    */
    public class DlcContractFundingInput {
        /**
        * The outpoint used for funding
        */
        public OutPoint Outpoint { get; set; } 
        /**
        * The value of the outpoint (in satoshi)
        */
        public long Value { get; set; } 
    }

    public class OutPoint {
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Hash { get; set; } 	
        public long Index { get; set; } 
    }

    public class DlcContractSettlementSignature {
        /**
        * The oracle value for which transaction these are the signatures
        */
        public long Outcome { get; set; } 
        /**
        * The signature for the transaction
        */
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Signature { get; set; } 
    }


    public class NewForwardOfferArgs {
        public DlcFwdOffer Offer { get; set; } 
    }

    public class NewForwardOfferReply {
        public DlcFwdOffer Offer { get; set; } 
    }

    public class ListOffersReply {
        public DlcFwdOffer[] Offers { get; set; } 
    }

    public class AcceptDeclineOfferArgs {
        public int OIdx { get; set; } 
    }

    public class SuccessReply {
        public bool Success { get; set; } 
    }

    public class OfferContractArgs {
        public int CIdx { get; set; } 
        public int PeerIdx { get; set; } 
    }

    public class NewGetContractReply {
        public DlcContract Contract { get; set; } 
    }

    public class ListContractsReply {
        public DlcContract[] Contracts { get; set; } 
    }

    public class GetContractArgs {
        public int Idx { get; set; } 
    }

    public class AcceptOrDeclineContractArgs {
        public int CIdx { get; set; } 
    }

    public class SettleContractArgs {
        public int CIdx { get; set; } 
        public long OracleValue { get; set; } 
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] OracleSig { get; set; } 
    }

    public class SettleContractReply {
        public bool Success { get; set; }   
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] SettleTxHash { get; set; } 
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] ClaimTxHash { get; set; } 
    }

    public class SetContractFundingArgs {
        public int CIdx { get; set; } 
        public long OurAmount { get; set; } 
        public long TheirAmount { get; set; } 
    }

    public class SetContractDivisionArgs {
        public int CIdx { get; set; } 
        public long ValueFullyOurs { get; set; } 
        public long ValueFullyTheirs { get; set; } 
    }

    public class SetContractCoinTypeArgs {
        public int CIdx { get; set; } 
        public UInt32 CoinType { get; set; }
    }

    public class SetContractSettlementTimeArgs {
        public int CIdx { get; set; } 
        public long Time { get; set; } 
    }

    public class SetContractRPointArgs {
        public int CIdx { get; set; } 
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] RPoint { get; set; } 
    }

    public class SetContractOracleArgs {
        public int CIdx { get; set; } 
        public int OIdx { get; set; } 
    }
}