using Bot.Telegram.Common.Model.Domain;

namespace Bot.Telegram.Common.Model.Session
{
    public interface ISessionStorage
    {
        void HandleCommandSession(Author author, int commandIndex, SessionStatus sessionStatus, ISessionMeta sessionMeta);
        bool TryGetUserSession(Author author, out ISession session);
    }
}