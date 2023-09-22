namespace StreetArtist.UI;

internal class Program {
    static async Task Main(string[] args) {
        OpenGLWindow window = new();
        await window.RunOnNewThread();
    }
}
