using Bot.Telegram.Common.Model;
using Bot.Telegram.Common.Model.Session;
using TaskManager.Common;
using TaskManager.Trello;

namespace Bot.Telegram.Common.Commands
{
    public class AuthorizationCommand : ICommand
    {
        public bool IsPublicCommand => false;
        public string CommandTrigger => "/authorize";

        private readonly IAuthorizationProvider trelloAuthorizationProvider;

        private readonly AuthorizationStorage authorizationStorage;

        public AuthorizationCommand(AuthorizationStorage authorizationStorage, string appKey)
        {
            this.authorizationStorage = authorizationStorage;
            trelloAuthorizationProvider = new TrelloAuthorizationProvider(appKey);
        }

        public ICommandResponse StartCommand(ICommandInfo commandInfo)
        {
            return commandInfo.SessionMeta == null ? StartAuthorization() : ContinueAuthorization(commandInfo);
        }

        private ICommandResponse StartAuthorization()
        {
            var message = @"
Привет!
Для для начала давай познакомимся.
...
тут какой-нибудь описательный текст
";
            var response = ChainResponse.Create(SessionStatus.Expect)
                .AddResponse(TextResponse.ExpectedCommand(message))
                .AddResponse(GetHelpResponse());

            return new CommandResponse(response, (int) CommandStatus.AuthorizationRequest);
        }

        private ICommandResponse ContinueAuthorization(ICommandInfo commandInfo)
        {
            var token = commandInfo.Command;


            if (trelloAuthorizationProvider.IsValidAuthorizationToken(token).Result)
            {
                trelloAuthorizationProvider.CheckOrInitializeWorkspace(token).GetAwaiter().GetResult();
                authorizationStorage.SetUserToken(commandInfo.Author, token);
                return new CommandResponse(TextResponse.CloseCommand("Все круто"));
            }

            var response = ChainResponse.Create(SessionStatus.Expect)
                .AddResponse(TextResponse.ExpectedCommand("Что-то пошло не так, попробуйте еще раз"))
                .AddResponse(GetHelpResponse());

            return new CommandResponse(response, (int) CommandStatus.AuthorizationError);
        }

        private IResponse GetHelpResponse()
        {
            return TextResponse.ExpectedCommand(
                @$"
Тебе нужно перейте по указанной ссылке дать доступ к своим таблицам учтной записи нашего бота.
Для этого тебе нужно перейти по ссылке {trelloAuthorizationProvider.GetAuthorizationUrl()} и нажать кнопку <Разрешить>.
Затем нужно отправить в ответном сообщении полученный тобой токен.
Чуть позже этот процесс будет проще :(
");
        }

        private enum CommandStatus
        {
            AuthorizationRequest,
            AuthorizationError
        }
    }
}