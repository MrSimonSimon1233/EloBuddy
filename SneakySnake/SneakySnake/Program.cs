using EloBuddy;
using EloBuddy.SDK.Events;
namespace SneakySnake
{
    class Program
    {
        static void Main(string[] args)
        { 
            Loading.OnLoadingComplete += eventArgs => Cassiopeia.OnLoad();
            Chat.Print("SneakySnake addon by MrSimonSimon1233 loaded");
        }
    }
}
