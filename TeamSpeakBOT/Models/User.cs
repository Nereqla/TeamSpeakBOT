using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamSpeakBOT.Models;
internal class User
{
    public string Message { get; set; }
    public GetClientInfo GetClientInfo { get; set; }
}
