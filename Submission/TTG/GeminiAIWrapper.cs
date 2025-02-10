using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI.Microsoft;
using Nxnw.Adc.Acm.Core.Common;
using Nxnw.Adc.Infrastructure.Exceptions;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nxnw.Adc.Infrastructure
{
    public interface IGeminiAIWrapper : IAIWrapper
    {
     
    }
    public class GeminiAIWrapper : IGeminiAIWrapper
    {
        private readonly ILogger<GeminiAIWrapper> _Logger;
        private readonly ITtgApplicationSettings _TtgApplicationSettings;

        private string apiKey = "";
        private string model = "";

        public GeminiAIWrapper(ILogger<GeminiAIWrapper> logger, ITtgApplicationSettings ttgApplicationSettings)
        { 
            _Logger = logger;
            _TtgApplicationSettings = ttgApplicationSettings;
        }

        public async Task<string> CompleteChatAsync(IEnumerable<string> systemMessages, IEnumerable<string> userMessages)
        {
            IChatClient chatClient = new GeminiChatClient(apiKey, model);

            List<ChatMessage> chatMessages = new List<ChatMessage>();
            foreach (string systemMessage in systemMessages)
            {
                _Logger.LogInformation("System message: {systemMessage}", systemMessage);
                ChatMessage chatMessage = new ChatMessage(ChatRole.System, systemMessage);
                chatMessages.Add(chatMessage);
            }
            foreach (string userMessage in userMessages)
            {
                _Logger.LogInformation("User message: {userMessages}", userMessage);
                ChatMessage chatMessage = new ChatMessage( ChatRole.User, userMessage);
                chatMessages.Add(chatMessage);
            }

            ChatCompletion chatCompletion = await chatClient.CompleteAsync(chatMessages);

            _Logger.LogDebug("ChatCompletion: {@chatCompletion}", chatCompletion);
            StringBuilder responseStringBuilder = new StringBuilder();
            foreach (ChatMessage message in chatCompletion.Choices)
            {
                _Logger.LogInformation("ChatMessage.Text: {text}", message.Text);
                responseStringBuilder.Append(message.Text);
            }

            string responseText = responseStringBuilder.ToString();
            _Logger.LogInformation("ResponseText: {responseText}", responseText);
            return responseText;
        }

        public string CleanResponse(string responseText)
        {
            if (responseText.StartsWith("```html"))
            {
                string formattedResponseText = responseText.Replace("```html", "").Replace($"```{System.Environment.NewLine}", "").Replace(System.Environment.NewLine, "");
                _Logger.LogInformation("responseText: {responseText} formattedResponseText: {formattedResponseText}", responseText, formattedResponseText);
                return formattedResponseText;
            }
            else if (responseText.StartsWith("```liquid"))
            {
                string formattedResponseText = responseText.Replace("```liquid", "").Replace($"```{System.Environment.NewLine}", "").Replace(System.Environment.NewLine, "");
                _Logger.LogInformation("responseText: {responseText} formattedResponseText: {formattedResponseText}", responseText, formattedResponseText);
                return formattedResponseText;
            }
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
            if (string.IsNullOrEmpty(_TtgApplicationSettings.AIModel))
            {
                model = "gemini-1.5-flash";
            }
            else
            {
                model = _TtgApplicationSettings.AIModel;
            }
        }
    }
}
