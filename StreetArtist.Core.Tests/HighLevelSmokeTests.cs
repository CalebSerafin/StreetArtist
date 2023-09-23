namespace StreetArtist.Core.Tests;

public class HighLevelSmokeTests {
    public HighLevelSmokeTests(ITestOutputHelper testOutput) {
        this.testOutput = testOutput;
    }

    readonly ITestOutputHelper testOutput;

    [Fact]
    public async Task ProjectStartsUp() {
        CancellationToken timeoutCt = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;
        IHost host = await new HostBuilder()
            .ConfigureLogging(builder => builder.AddProvider(new TestOutputLogger(testOutput)))
            .ConfigureServices(services => services.AddStreetArtistCore())
            .StartAsync(timeoutCt);
        // Not all background are finished yet.
        await Task.Delay(TimeSpan.FromMilliseconds(50));
        // Set stopping token and call for gracious exit.
        await host.StopAsync(timeoutCt);
        // Background services have graciously finished.
    }
}