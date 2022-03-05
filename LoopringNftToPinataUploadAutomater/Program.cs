﻿using CsvHelper;
using LoopringNftToPinataUploadAutomater;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

string apiKey = Environment.GetEnvironmentVariable("PINATAAPIKEY", EnvironmentVariableTarget.Machine);//you can either set an environmental variable or input it here directly.
string apiKeySecret = Environment.GetEnvironmentVariable("PINATAAPIKEYSECRET", EnvironmentVariableTarget.Machine); //you can either set an environmental variable or input it here directly.
string nftImageDirectoryFilePath = "C:\\NFT\\FrankenLoops"; //this should point to a directory that only contains your images in the naming format: 1.jpg, 2.jpg, 3.jpg and etc

FileInfo[] nftImageDirectoryFileNames = Directory.GetFiles(nftImageDirectoryFilePath).Select(fn => new FileInfo(fn)).ToArray();
IPinataService pinataService = new PinataService();
List<NftCidPair> metadataCIDPairs = new List<NftCidPair>();

foreach (FileInfo nftImageFileInfo in nftImageDirectoryFileNames)
{
    string nftId = nftImageFileInfo.Name.Split('.')[0]; //the source file directory has the nfts named as follows: 1.jpg, 2.jpg, 3.jpg, 4.jpg and etc
    string nftName = $"FrankenLoop #{nftId}"; //change this to the name of your nft
    string nftDescription = "It is a mistake to fancy that horror is associated inextricably with darkness, silence, and solitude."; //change this to the description of your nft

    //Submit image to pinata section
    Console.WriteLine($"Uploading {nftName} image to Pinata");
    PinataResponseData? pinataImageResponseData = await pinataService.SubmitPin(apiKey, apiKeySecret, File.ReadAllBytes(nftImageFileInfo.FullName), nftName);
    Console.WriteLine($"{nftName} image uploaded to Pinata successfully");

    //Submit metadata.json to pinata section
    NftMetadata nftMetadata = new NftMetadata
    {
        name = nftName,
        description = nftDescription,
        image = "ipfs://" + pinataImageResponseData.IpfsHash
    };
    MetadataGuid metadataGuid = new MetadataGuid
    {
        name = nftName + " - metadata.json"
    };
    string metadataGuidJsonString = JsonConvert.SerializeObject(metadataGuid);
    string metaDataJsonString = JsonConvert.SerializeObject(nftMetadata);
    byte[] metaDataByteArray = Encoding.ASCII.GetBytes(metaDataJsonString);
    Console.WriteLine($"Uploading {nftName} metadata to Pinata");
    PinataResponseData? pinataMetadataResponseData = await pinataService.SubmitPin(apiKey, apiKeySecret, metaDataByteArray, "metadata.json", true, metadataGuidJsonString);
    Console.WriteLine($"{nftName} metadata uploaded to Pinata successfully");
    Console.WriteLine($"Generated CID {pinataMetadataResponseData.IpfsHash}");

    //Add nft cid pair to list for later csv generation
    NftCidPair nftCidPair = new NftCidPair
    {
        Id = nftName,
        MetadataCid = pinataMetadataResponseData.IpfsHash
    };
    metadataCIDPairs.Add(nftCidPair);
}

//Generate nft cid pair csv here
if(metadataCIDPairs.Count > 0)
{
    string csvName = $"{DateTime.Now.ToString("yyyy-mm-dd hh-mm-ss")}.csv";
    using (var writer = new StreamWriter(csvName))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        csv.WriteRecords(metadataCIDPairs);
        Console.WriteLine($"Generated NFT ID/Metadata CID Pairs csv: {csvName} in current directory");
    }
}
else
{
    Console.WriteLine("Did not generate any Metadata CIDS");
}

Console.WriteLine("Enter any key to end:");
Console.ReadKey();