using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace isRock.Template
{
    public class LineBotChatGPTWebHookController : isRock.LineBot.LineWebHookControllerBase
    {
        [Route("api/LineBotChatGPTWebHook")]
        [HttpPost]
        public IActionResult POST()
        {
            const string AdminUserId = "_______U5e60294b8c__AdminUserId__02d6295b621a_____"; //👉repleace it with your Admin User Id

            try
            {
                //設定ChannelAccessToken
                this.ChannelAccessToken = "_____________ChannelAccessToken___________________"; //👉repleace it with your Channel Access Token
                //配合Line Verify
                if (ReceivedMessage.events == null || ReceivedMessage.events.Count() <= 0 ||
                    ReceivedMessage.events.FirstOrDefault().replyToken == "00000000000000000000000000000000") return Ok();
                //取得Line Event
                var LineEvent = this.ReceivedMessage.events.FirstOrDefault();
                var responseMsg = "";
                //準備回覆訊息
                if (LineEvent.type.ToLower() == "message" && LineEvent.message.type == "text")
                {
                    //先判斷intent
                    var intents = ChatGPT.getIntentsFromGPT(LineEvent.message.text);
                    if (intents.Contains("購票"))
                    {
                        //處理購票
                        responseMsg = ChatGPT.getSellTicketResponseFromGPT(LineEvent.message.text);
                }
                    else if (intents.Contains("客訴"))
                    {
                        //處理客訴
                        responseMsg = ChatGPT.getCustomServiceResponseFromGPT(LineEvent.message.text);
                    }
                    else
                    {
                        //閒聊
                        responseMsg = ChatGPT.getChatResponseFromGPT(LineEvent.message.text);
                    }
                }
                else if (LineEvent.type.ToLower() == "message")
                    responseMsg = $"收到 event : {LineEvent.type} type: {LineEvent.message.type} ";
                else
                    responseMsg = $"收到 event : {LineEvent.type} ";
                //回覆訊息
                this.ReplyMessage(LineEvent.replyToken, responseMsg);
                //response OK
                return Ok();
            }
            catch (Exception ex)
            {
                //回覆訊息
                this.PushMessage(AdminUserId, "發生錯誤:\n" + ex.Message);
                //response OK
                return Ok();
            }
        }
    }

    public class ChatGPT
    {
        const string AzureOpenAIEndpoint = "https://_________.openai.azure.com";  //👉replace it with your Azure OpenAI Endpoint
        const string AzureOpenAIModelName = "gpt35"; //👉repleace it with your Azure OpenAI Model Name
        const string AzureOpenAIToken = "_________API_KEY___________"; //👉repleace it with your Azure OpenAI Token
        const string AzureOpenAIVersion = "2023-03-15-preview";  //👉replace  it with your Azure OpenAI Model Version

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum role
        {
            assistant, user, system
        }

        public static string CallAzureOpenAIChatAPI(
            string endpoint, string modelName, string apiKey, string apiVersion, object requestData)
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl = $"{endpoint}/openai/deployments/{modelName}/chat/completions?api-version={apiVersion}";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT heade
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }
    public static string getChatResponseFromGPT(string Message)
        {
            return ChatGPT.CallAzureOpenAIChatAPI(
               AzureOpenAIEndpoint, AzureOpenAIModelName, AzureOpenAIToken, AzureOpenAIVersion,
                new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new {
                            role = ChatGPT.role.system ,
                            content = @"
                               你是一個和善的客服機器人，客戶會向你提出各種問題，請盡可能溫柔的回答，設法讓客戶開心。

                                回應時，請注意以下幾點:
                                * 不要說 '感謝你的來信' 之類的話，因為客戶是從對談視窗輸入訊息的，不是寫信來的
                                * 不能過度承諾
                                * 要同理客戶的情緒
                                * 要能夠盡量解決客戶的問題
                                * 不要以回覆信件的格式書寫，請直接提供對談機器人可以直接給客戶的回覆
                                ----------------------
"
                        },
                        new {
                             role = ChatGPT.role.user,
                              content = Message
                             },
                    }
                });
        }
        public static string getSellTicketResponseFromGPT(string Message)
        {
            return ChatGPT.CallAzureOpenAIChatAPI(
               AzureOpenAIEndpoint, AzureOpenAIModelName, AzureOpenAIToken, AzureOpenAIVersion,
                new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new {
                            role = ChatGPT.role.system ,
                            content = @"
                               你是一個售票克服機器人，客戶會向你購票，你必須從客戶的購票敘述中找到底下這些購票資訊。
                               找到的資訊必須覆述一次，如果有缺少的資訊，必須提醒客戶缺少的部分，並且請客戶再說一次。
                               購票資訊包含:
                                * 乘車起始站
                                * 乘車目的站     
                                * 乘車預計出發時間
                                * 乘車張數
                                * 乘車票種

                                回應時，請注意以下幾點:
                                * 不要說 '感謝你的來信' 之類的話，因為客戶是從對談視窗輸入訊息的，不是寫信來的
                                * 不能過度承諾
                                * 要同理客戶的情緒
                                * 要能夠盡量解決客戶的問題
                                * 不要以回覆信件的格式書寫，請直接提供對談機器人可以直接給客戶的回覆
                                ----------------------
"
                        },
                        new {
                             role = ChatGPT.role.user,
                              content = Message
                             },
                    }
                });
        }


        public static string getCustomServiceResponseFromGPT(string Message)
        {
            return ChatGPT.CallAzureOpenAIChatAPI(
               AzureOpenAIEndpoint, AzureOpenAIModelName, AzureOpenAIToken, AzureOpenAIVersion,
                new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new {
                            role = ChatGPT.role.system ,
                            content = @"
                                假設你是一個專業客戶服務人員，對於客戶非常有禮貌、也能夠安撫客戶的抱怨情緒。
                                請檢視底下的客戶訊息，以最親切有禮的方式回應。

                                但回應時，請注意以下幾點:
                                * 不要說 '感謝你的來信' 之類的話，因為客戶是從對談視窗輸入訊息的，不是寫信來的
                                * 不能過度承諾
                                * 要同理客戶的情緒
                                * 要能夠盡量解決客戶的問題
                                * 不要以回覆信件的格式書寫，請直接提供對談機器人可以直接給客戶的回覆
                                ----------------------
"
                        },
                        new {
                             role = ChatGPT.role.user,
                              content = Message
                             },
                    }
                });
        }

        public static string[] getIntentsFromGPT(string Message)
        {
            var ret = ChatGPT.CallAzureOpenAIChatAPI(
               AzureOpenAIEndpoint, AzureOpenAIModelName, AzureOpenAIToken, AzureOpenAIVersion,
                new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new {
                            role = ChatGPT.role.system ,
                            content = @"
                                你是一個判斷 Intent 的AI，會將傳入的文字敘述判斷出該敘述的意圖
                                意圖只有 購票/客訴/閒聊 三種，請判斷出最接近的，並將結果以JSON回傳。

                                請判斷下面的敘述，這句話的 intent 是什麼?
                                
                                請組一個JSON給我， 不需要任何文字解釋，只要回傳JSON即可。同時請注意以下規則:
                                * JSON中的 intent 屬性是陣列格式，如果發現有多種意圖，或intent 有多種可能性，請在JSON中以陣列方式回傳。
                                * JSON中的 intent 屬性的值，只能是 購票/客訴/閒聊 三種之一
                                * 原始語句用 text 屬性回傳。
                                * 不需要任何文字解釋，只要回傳JSON即可
"
                        },
                         new {
                             role = ChatGPT.role.user,
                              content = "我想要一張從台北到高雄的自由座"
                             },
                         new {
                             role = ChatGPT.role.assistant,
                              content = @"{intents : [""購票""]}"
                             },
                         new {
                             role = ChatGPT.role.user,
                              content = "地上有蟑螂"
                             },
                         new {
                             role = ChatGPT.role.assistant,
                              content = @"{intents : [""客訴""]}"
                             },
                        new {
                             role = ChatGPT.role.user,
                              content = Message
                             },
                    }
                });

            var Object = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(ret);
            //return intent array
            return Object.intents.ToObject<string[]>();
        }
    }
}