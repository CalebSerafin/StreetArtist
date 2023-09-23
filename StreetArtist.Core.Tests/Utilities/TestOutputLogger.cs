namespace StreetArtist.Core.Tests;

class TestOutputLogger : ILoggerProvider, ILogger {
    public TestOutputLogger(ITestOutputHelper testOutput) {
        this.testOutput = testOutput;
    }
    #region Fields
    readonly ITestOutputHelper testOutput;
    #endregion

    #region ILoggerProvider
    public ILogger CreateLogger(string categoryName) => this;
    public void Dispose() { }
    #endregion

    #region ILogger
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => testOutput.WriteLine(formatter(state, exception));
    #endregion
}