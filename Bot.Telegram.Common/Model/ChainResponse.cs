using System.Collections.Generic;
using Bot.Telegram.Common.Model.Session;

namespace Bot.Telegram.Common.Model
{
    public class ChainResponse : IResponse
    {
        private readonly List<IResponse> responses;
        public IEnumerable<IResponse> Responses => responses;

        private ChainResponse(SessionStatus sessionStatus)
        {
            SessionStatus = sessionStatus;
            responses = new List<IResponse>();
        }

        public SessionStatus SessionStatus { get; }

        public static ChainResponse Create(SessionStatus sessionStatus)
        {
            return new ChainResponse(sessionStatus);
        }

        public ChainResponse AddResponse(IResponse response)
        {
            responses.Add(response);
            return this;
        }
    }
}