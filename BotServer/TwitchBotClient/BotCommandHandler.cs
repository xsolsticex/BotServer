using BotServer.Commands.Interface;
using System.Windows.Input;

namespace BotServer.TwitchBotClient
{
    //TODO
    public class BotCommandHandler
    {
        public List<ITwitchCommand> Commands { get; set; } = new();


        public void AddCommand(ITwitchCommand command)
        {
            Commands.Add(command);
        }


        public async Task ExecuteCommand(string commandName,string[] args)
        {
            //await Commands.Where(c => c.Name == commandName).FirstOrDefault().ExecuteAsync(args);
        }

        public static void HandleComand(string command)
        {
            switch (command)
            {
                case "hola":
                    break;

                default: break;
            }

        }
    }
}
