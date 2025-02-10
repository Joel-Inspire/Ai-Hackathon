using Microsoft.Extensions.Logging;
using Nxnw.Adc.Acm.Core.Common;
using Nxnw.Adc.DataLayer.RelationClasses;
using Nxnw.Adc.Infrastructure.Exceptions;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Nxnw.Adc.Infrastructure
{
    public interface IOpenAIWrapper : IAIWrapper
    {
    }

    public class OpenAIWrapper : IOpenAIWrapper
    {
        private readonly ITtgApplicationSettings _TtgApplicationSettings;
        private readonly ILogger<OpenAIWrapper> _Logger;
        private string apiKey = "";
        private string uri = "";
        private string model = "";

        public OpenAIWrapper(ITtgApplicationSettings ttgApplicationSettings, ILogger<OpenAIWrapper> logger)
        {
            _TtgApplicationSettings = ttgApplicationSettings;
            _Logger = logger;
        }

        public async Task<string> CompleteChatAsync(IEnumerable<string> systemMessages, IEnumerable<string> userMessages)
        {
            OpenAIClient client = null;

            ApiKeyCredential apiKeyCredential = new ApiKeyCredential(apiKey);
            if (string.IsNullOrEmpty(uri))
            {
                client = new OpenAIClient(apiKeyCredential);
            }
            else
            {
                client = new OpenAIClient(apiKeyCredential, new OpenAIClientOptions() { Endpoint = new System.Uri(uri) });

            }

            List<ChatMessage> chatMessages = new List<ChatMessage>();
            foreach (string systemMessage in systemMessages)
            {
                _Logger.LogInformation("System message: {systemMessage}", systemMessage);
                chatMessages.Add(ChatMessage.CreateSystemMessage(systemMessage));
            }
            foreach (string userMessage in userMessages)
            {
                _Logger.LogInformation("User message: {userMessages}", userMessage);
                chatMessages.Add(ChatMessage.CreateUserMessage(userMessage));
            }


            ChatClient chatClient = client.GetChatClient(model);
            ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages);


            _Logger.LogDebug("ChatCompletion: {@chatCompletion}", chatCompletion);

            StringBuilder responseStringBuilder = new StringBuilder();
            foreach (ChatMessageContentPart contentParts in chatCompletion.Content)
            {
                _Logger.LogInformation("ChatMessageContentPart.Text: {text}", contentParts.Text);
                responseStringBuilder.Append(contentParts.Text);
            }

            string responseText = responseStringBuilder.ToString();
            _Logger.LogInformation("ResponseText: {responseText}", responseText);
            return responseText;

        }

        public string CleanResponse(string responseText)
        {
            return responseText;
        }

        void IAIWrapper.Initialize()
        {
            if (string.IsNullOrEmpty(_TtgApplicationSettings.AIApiKey))
            {
                throw new AIConfigurationException("AIApiKey is null");
            }
            else
            {
                apiKey = _TtgApplicationSettings.AIApiKey;
            }
            if (!string.IsNullOrEmpty(_TtgApplicationSettings.AIUri))
            {
                uri = _TtgApplicationSettings.AIUri;
            }
            if (string.IsNullOrEmpty(_TtgApplicationSettings.AIModel))
            {
                model = "llama3"; //gpt-4o-mini gpt-3.5-turbo
            }
            else
            {
                model = _TtgApplicationSettings.AIModel;
            }
        }
    }
}
