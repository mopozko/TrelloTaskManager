using System;
using System.Linq;
using TaskManager.Bot.Model;
using TaskManager.Bot.Model.Domain;
using TaskManager.Bot.Model.Session;
using TaskManager.Common.Storage;
using TaskManager.Common.Tasks;

namespace TaskManager.Bot.Commands
{
    public class AddTask : ICommand
    {
        private const int MaxTaskNameLength = 500;
        private const int MaxTaskDescriptionLength = 1500;

        private readonly string[][] menuCommands =
        {
            new[]
            {
                "Добавить название",
                "Добавить описание"
            },
            new[]
            {
                "Сохранить",
                "Отмена"
            }
        };

        private readonly InMemoryStorage<MyTask, long> taskInitializationStorage;

        private readonly ITaskHandler taskProvider;

        public AddTask(ITaskHandler taskProvider)
        {
            this.taskProvider = taskProvider;
            taskInitializationStorage = new InMemoryStorage<MyTask, long>();
        }

        public bool IsPublicCommand => true;

        public string CommandTrigger => "добавить задачу";

        public ICommandResponse StartCommand(ICommandInfo commandInfo)
        {
            return commandInfo.SessionMeta == null
                ? GetMenu("мы начинаем")
                : StartCommand(commandInfo.Author, commandInfo.Command, commandInfo.SessionMeta);
        }

        private ICommandResponse StartCommand(Author author, string commandText, ISessionMeta meta)
        {
            var response = GetResponse(author, commandText, meta);

            return response ?? new CommandResponse(TextResponse.CloseCommand("Кажется, что-то пошло не так :("));
        }

        private ICommandResponse GetResponse(Author author, string commandText, ISessionMeta meta)
        {
            return (CommandStatus) meta.ContinueFrom switch
            {
                CommandStatus.Menu => ToMenuAction(author, commandText),
                CommandStatus.SetDescription => SetDescriptionAction(author, commandText),
                CommandStatus.SetName => SetNameAction(author, commandText),
                _ => throw new Exception() //add message
            };
        }

        private ICommandResponse ToMenuAction(Author author, string commandText)
        {
            return commandText switch
            {
                "Добавить название" => new CommandResponse(TextResponse.ExpectedCommand("Введите название"),
                    (int) CommandStatus.SetName),
                "Добавить описание" => new CommandResponse(TextResponse.ExpectedCommand("Введите описание"),
                    (int) CommandStatus.SetDescription),
                "Сохранить" => SaveAction(author),
                "Отмена" => AbortAction(author),
                _ => GetMenu("Попробуй еще")
            };
        }

        private ICommandResponse SetNameAction(Author author, string taskName)
        {
            if (taskName.Length > MaxTaskNameLength) //test1
                return GetMenu($"Максимальная длина названия - {MaxTaskNameLength}");

            if (menuCommands.Any(x => x.Contains(taskName)))
                return GetMenu($"Недопустимое название");

            var task = taskInitializationStorage.Get(author.TelegramId);
            task.Name = taskName;
            taskInitializationStorage.Update(task);
            return GetMenu("Название добавлено");
        }

        private ICommandResponse SetDescriptionAction(Author author, string taskDescription)
        {
            if (taskDescription.Length > MaxTaskDescriptionLength)
                return GetMenu($"Максимальная длина описания - {MaxTaskDescriptionLength}");

            if (menuCommands.Any(x => x.Contains(taskDescription)))
                return GetMenu($"Недопустимое описание");

            var task = taskInitializationStorage.Get(author.TelegramId);
            task.Description = taskDescription;
            taskInitializationStorage.Update(task);
            return GetMenu("Описание добавлено");
        }

        private ICommandResponse SaveAction(Author author)
        {
            var task = taskInitializationStorage.Get(author.TelegramId);
            if (task.Name == default)
                return new CommandResponse(TextResponse.ExpectedCommand("Задаче необходимо добавить имя"),
                    (int) CommandStatus.Menu);
            taskInitializationStorage.Delete(task.Key);

            task = taskProvider.AddNewTask(author.UserToken, task).Result;

            return new CommandResponse(TextResponse.CloseCommand(@$"
Задача успешно добавлена!

{task}
"));
        }

        private ICommandResponse AbortAction(Author author)
        {
            var task = taskInitializationStorage.Get(author.TelegramId);
            taskInitializationStorage.Delete(task.Key);

            return new CommandResponse(TextResponse.AbortCommand("Отменено"));
        }

        private ICommandResponse GetMenu(string message)
        {
            return new CommandResponse(
                new ButtonResponse(message, menuCommands, SessionStatus.Expect),
                (int) CommandStatus.Menu
            );
        }

        private enum CommandStatus
        {
            Menu = 1,
            SetName = 2,
            SetDescription = 3
        }
    }
}