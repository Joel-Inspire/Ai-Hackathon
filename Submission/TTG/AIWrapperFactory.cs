using Nxnw.Adc.Acm.Core.Common;
using Nxnw.Adc.Infrastructure.Exceptions;
using System;


namespace Nxnw.Adc.Infrastructure
{
    public class AIWrapperFactory : IAIWrapperFactory
    {
        private readonly ITtgApplicationSettings _TtgApplicationSettings;
        private readonly Func<IGeminiAIWrapper> _GeminiAIWrapperDelegate;
        private readonly Func<IOpenAIWrapper> _OpenAIWrapperDelegate;

        public AIWrapperFactory(ITtgApplicationSettings ttgApplicationSettings,
            Func<IGeminiAIWrapper> geminiAIWrapperDelegate,
            Func<IOpenAIWrapper> openAIWrapperDelegate)
        {
            _TtgApplicationSettings = ttgApplicationSettings;
            _GeminiAIWrapperDelegate = geminiAIWrapperDelegate;
            _OpenAIWrapperDelegate = openAIWrapperDelegate;
        }

        public IAIWrapper Create()
        {
            if (string.IsNullOrWhiteSpace(_TtgApplicationSettings.AIProvider)) throw new AIConfigurationException("AIProvider is null");
            if (_TtgApplicationSettings.AIProvider == "Gemini")
            {
                IAIWrapper  aIWrapper = _GeminiAIWrapperDelegate();
                aIWrapper.Initialize();
                return aIWrapper;
            }
            else if (_TtgApplicationSettings.AIProvider == "OpenAI")
            {
                IAIWrapper aIWrapper = _OpenAIWrapperDelegate();
                aIWrapper.Initialize();
                return aIWrapper;
            }
            throw new AIConfigurationException($"{_TtgApplicationSettings.AIProvider} not implemented");
        }
    }
}
