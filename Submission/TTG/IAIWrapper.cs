using Nxnw.Adc.DataLayer.EntityClasses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nxnw.Adc.Infrastructure
{
    public interface IAIWrapper
    {
        Task<string> CompleteChatAsync(IEnumerable<string> systemMessages, IEnumerable<string> userMessages);
        internal void Initialize();
        string CleanResponse(string responseText);
    }

    public interface IAIWrapperFactory
    {
        IAIWrapper Create();
    }
}
