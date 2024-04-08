using TeamSpeakBOT.Helper;
using TeamSpeakBOT.Interface;
using TeamSpeakBOT.Models;

namespace TeamSpeakBOT.Modules;
internal class SendMessages : IModule
{
    private static Queue<User> _messages = new Queue<User>();

    public async Task<bool> Run()
    {
        while (_messages.TryPeek(out User value))
        {
            Ts3Client.Client.SendMessage(value.Message,value.GetClientInfo);
        }

        return true;
    }
}
