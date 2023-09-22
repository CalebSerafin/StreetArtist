using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace StreetArtist.UI;
internal sealed class OpenGLWindow : IDisposable {
    public OpenGLWindow() {
        //Create a window.
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "LearnOpenGL with Silk.NET";

        window = Window.Create(options);

        //Assign events.
        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
    }

    #region Public Methods
    public Task RunOnNewThread() {
        Thread thread = new(RunBlocking);

        thread.Start();
        return windowClosedTaskSource.Task;
    }

    public void RunBlocking() {
        //Run the window.
        // window.Run() is a BLOCKING method - this means that it will halt execution of any code in the current
        // method until the window has finished running.
        window.Run();
        windowClosedTaskSource.SetResult();
    }
    #endregion

    #region Properties

    #endregion

    #region Fields
    readonly IWindow window;
    readonly TaskCompletionSource windowClosedTaskSource = new();
    #endregion

    #region Primitive Window Events
    private void OnLoad() {
        //Set-up input context.
        IInputContext input = window.CreateInput();
        for (int i = 0; i < input.Keyboards.Count; i++) {
            input.Keyboards[i].KeyDown += KeyDown;
        }
    }

    private void OnRender(double obj) {
        //Here all rendering should be done.
    }

    private void OnUpdate(double obj) {
        //Here all updates to the program should be done.
    }
    #endregion

    #region User Events
    private void KeyDown(IKeyboard arg1, Key arg2, int arg3) {
        //Check to close the window on escape.
        if (arg2 == Key.Escape) {
            window.Close();
        }
    }
    #endregion

    #region IDisposable
    public void Dispose() {
        window.Dispose();
    }
    #endregion
}
