﻿using Microsoft.AspNetCore.Mvc;
using receiptParser.Domain;
using receiptParser.Repository.mappers;
using receiptParser.Service.inter;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using HttpMultipartParser;
using Newtonsoft.Json;

namespace receiptParser.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ParseReceiptController : Controller
    {
        private readonly IUserReceiptService _userReceiptService;
        string endpoint = "https://muonreceiptparser.cognitiveservices.azure.com/";
        string apiKey = "de3591828f28412fa6c0ed24499a6c8e";
        private readonly ILogger _logger;


        [HttpPost(Name = "ParseReceipt")]
        public async Task<ReceiptResponse> ParseReceipt(HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var credential = new AzureKeyCredential(apiKey);
            var client = new DocumentAnalysisClient(new Uri(endpoint), credential);

            // Parse the form data
            var parsedFormBody = MultipartFormDataParser.ParseAsync(req.Body).Result;
            var usersStr = parsedFormBody.GetParameterValue("users");
            var usersObj = JsonConvert.DeserializeObject<List<string>>(usersStr);
            List<string> users = usersObj != null ? usersObj : new List<string>();



            FilePart file = parsedFormBody.Files[0];
            var stream = file.Data;


            AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-receipt", stream);


            AnalyzeResult receipts = operation.Value;

            string merchantName = null;
            Nullable<DateTimeOffset> transactionDate = null;
            double total = 0;
            double tip = 0;
            List<ItemDto> items = new List<ItemDto>();

            foreach (AnalyzedDocument receipt in receipts.Documents)
            {

                if (receipt.Fields.TryGetValue("MerchantName", out DocumentField merchantNameField))
                {
                    if (merchantNameField.FieldType == DocumentFieldType.String)
                    {
                        merchantName = merchantNameField.Value.AsString();

                    }
                }

                if (receipt.Fields.TryGetValue("TransactionDate", out DocumentField transactionDateField))
                {
                    if (transactionDateField.FieldType == DocumentFieldType.Date)
                    {
                        transactionDate = transactionDateField.Value.AsDate();

                    }
                }
                if (receipt.Fields.TryGetValue("Total", out DocumentField totalField))
                {
                    if (totalField.FieldType == DocumentFieldType.Double)
                    {
                        total = totalField.Value.AsDouble();

                    }
                }

                if (receipt.Fields.TryGetValue("Tip", out DocumentField tipField))
                {
                    if (tipField.FieldType == DocumentFieldType.Double)
                    {
                        tip = tipField.Value.AsDouble();

                    }
                }

                if (receipt.Fields.TryGetValue("Items", out DocumentField itemsField))
                {
                    if (itemsField.FieldType == DocumentFieldType.List)
                    {
                        foreach (DocumentField itemField in itemsField.Value.AsList())
                        {

                            if (itemField.FieldType == DocumentFieldType.Dictionary)
                            {
                                IReadOnlyDictionary<string, DocumentField> itemFields = itemField.Value.AsDictionary();
                                string itemDescription = null;
                                double itemTotalPrice = 0;
                                double itemQuantity = 0;
                                if (itemFields.TryGetValue("Description", out DocumentField itemDescriptionField))
                                {
                                    if (itemDescriptionField.FieldType == DocumentFieldType.String)
                                    {
                                        itemDescription = itemDescriptionField.Value.AsString();

                                    }
                                }

                                if (itemFields.TryGetValue("TotalPrice", out DocumentField itemTotalPriceField))
                                {
                                    if (itemTotalPriceField.FieldType == DocumentFieldType.Double)
                                    {
                                        itemTotalPrice = itemTotalPriceField.Value.AsDouble();

                                    }
                                }
                                if (itemFields.TryGetValue("Quantity", out DocumentField itemQuantityField))
                                {
                                    if (itemQuantityField.FieldType == DocumentFieldType.Double)
                                    {
                                        itemQuantity = itemQuantityField.Value.AsDouble();

                                    }
                                }

                                if (itemQuantity > 0)
                                {
                                    for (int i = 0; i < itemQuantity; i++)
                                    {
                                        ItemDto item = new ItemDto();
                                        if (itemDescription != null && itemTotalPrice != null)
                                        {
                                            item.description = itemDescription;
                                            item.price = itemTotalPrice;
                                            item.quantity = 1;

                                        }
                                        items.Add(item);
                                    }
                                }
                                else
                                {
                                    ItemDto item = new ItemDto();
                                    if (itemDescription != null && itemTotalPrice != null)
                                    {
                                        item.description = itemDescription;
                                        item.price = itemTotalPrice;
                                        item.quantity = 1;

                                    }
                                    items.Add(item);
                                }

                            }
                        }
                    }
                }

            }

            for (int i = 0; i < items.Count; i++)
            {
                items[i].itemId = i;
            }
            List<UserDto> userDtos = ReceiptMapper.MapUserNamesToUserDtos(users);
            Guid receiptId = Guid.NewGuid();
            ReceiptDto receiptDto = new ReceiptDto();
            receiptDto.items = items;
            receiptDto.total = total;
            receiptDto.tip = tip;
            receiptDto.merchantName = merchantName;
            receiptDto.users = userDtos;
            receiptDto.receiptId = receiptId.ToString();
            if (transactionDate != null)
            {
                receiptDto.transactionDate = transactionDate.Value;
            }

            //ReceiptDto resultReceiptDto = await _userReceiptService.CreateReceipt(receiptDto);

            ReceiptResponse responseReceipt = new ReceiptResponse();
            responseReceipt.isSuccess = true;
            responseReceipt.receipt = receiptDto;

            //string jsonResult = JsonConvert.SerializeObject(responseReceipt);

            //// Create an HTTP response with the JSON data
            //var response = req.CreateResponse(HttpStatusCode.OK);
            //response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            //response.WriteString(jsonResult);

            return responseReceipt;
        }
    }
}
