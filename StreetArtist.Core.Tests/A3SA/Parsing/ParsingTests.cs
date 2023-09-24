using System.Text;

using OneOf.Types;

using StreetArtist.Core.A3SA.Parsing;
using StreetArtist.Core.A3SA.Storage.Models;

namespace StreetArtist.Core.Tests.A3SA.Parsing;

public class ParsingTests {
    public ParsingTests(ITestOutputHelper testOutput) {
        this.testOutput = testOutput;
        cancellationToken = CancellationToken.None;
    }

    readonly ITestOutputHelper testOutput;
    readonly CancellationToken cancellationToken;

    [Fact]
    public async Task CannotParseEmptyFile() {
        // Arrange
        string text = """

            """;
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);
        Assert.True(result.TryPickT1(out Error<Exception> error, out _));

        // Assert
        testOutput.WriteLine($"Expected error {error.Value}");
        Assert.NotNull(error.Value);
    }

    [Fact]
    public async Task CanParseOnlyEmptyNavDefinition() {
        // Arrange
        string text = """
            navGrid = []
            """;
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);

        // Assert
        if (result.TryPickT1(out Error<Exception> error, out Result<NavGridRoot> navGridRootRes))
            Assert.Fail(error.Value.ToString());
        NavGridRoot navGridRoot = navGridRootRes.Value;
        Assert.NotNull(navGridRoot);
        Assert.Null(navGridRoot.MetaInformation);
        Assert.Empty(navGridRoot.RoadNodes);
    }


    [Fact]
    public async Task CannotParseOnlyMetaData() {
        // Arrange
        string text = """
            /*{"systemTimeUCT_G":"2021-06-24 11:14:25","worldName":"Stratis","StreetArtist_Config":{"_flatMaxDrift":-1,"_juncMergeDistance":-1,"_humanEdited": true}}*/
            """;
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);
        Assert.True(result.TryPickT1(out Error<Exception> error, out _));

        // Assert
        testOutput.WriteLine($"Expected error {error.Value}");
        Assert.NotNull(error.Value);
    }


    [Fact]
    public async Task CanParseEmptyDefinitions() {
        // Arrange
        string text = """
            /*{}*/
            navGrid = []
            """;
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);

        // Assert
        if (result.TryPickT1(out Error<Exception> error, out Result<NavGridRoot> navGridRootRes))
            Assert.Fail(error.Value.ToString());
        NavGridRoot navGridRoot = navGridRootRes.Value;
        Assert.NotNull(navGridRoot);
        Assert.NotNull(navGridRoot.MetaInformation);
        Assert.Empty(navGridRoot.RoadNodes);
    }

    [Fact]
    public async Task CanParseMeta() {
        // Arrange
        string systemTimeUTC_G = "2021-06-24 11:14:25";
        string worldName = "Stratis";
        float flatMaxDrift = 15.35f;
        float juncMergeDistance = -5.78f;
        bool humanEdited = true;
        string text = """
            /*{"systemTimeUCT_G":"2021-06-24 11:14:25","worldName":"Stratis","StreetArtist_Config":{"_flatMaxDrift":15.35,"_juncMergeDistance":-5.78,"_humanEdited": true}}*/
            navGrid = []
            """;
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);

        // Assert
        if (result.TryPickT1(out Error<Exception> error, out Result<NavGridRoot> navGridRootRes))
            Assert.Fail(error.Value.ToString());
        NavGridRoot navGridRoot = navGridRootRes.Value;
        Assert.NotNull(navGridRoot);
        Assert.NotNull(navGridRoot.MetaInformation);
        Assert.NotNull(navGridRoot.MetaInformation.StreetArtistConfig);
        Assert.Empty(navGridRoot.RoadNodes);

        Assert.Equal(systemTimeUTC_G, navGridRoot.MetaInformation.SystemTimeUTC_G);
        Assert.Equal(worldName, navGridRoot.MetaInformation.WorldName);
        Assert.Equal(flatMaxDrift, navGridRoot.MetaInformation.StreetArtistConfig.FlatMaxDrift, floatTolerance);
        Assert.Equal(juncMergeDistance, navGridRoot.MetaInformation.StreetArtistConfig.JunctionMergeDistance, floatTolerance);
        Assert.Equal(humanEdited, navGridRoot.MetaInformation.StreetArtistConfig.HumanEdited);
    }

    [Fact]
    public async Task CanParseOnlyIslandNav() {
        // Arrange
        string text = """
            /*{"systemTimeUCT_G":"2021-06-24 11:14:25","worldName":"Stratis","StreetArtist_Config":{"_flatMaxDrift":-1,"_juncMergeDistance":-1,"_humanEdited": true}}*/
            navGrid = [
                [[3464.73,5624.75,0],0,false,[]]
            ]
            """;
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);

        // Assert
        if (result.TryPickT1(out Error<Exception> error, out Result<NavGridRoot> navGridRootRes))
            Assert.Fail(error.Value.ToString());
        NavGridRoot navGridRoot = navGridRootRes.Value;
        Assert.NotNull(navGridRoot);
        Assert.NotNull(navGridRoot.MetaInformation);
        Assert.Single(navGridRoot.RoadNodes);

        RoadNode node = navGridRoot.RoadNodes[0];
        Assert.Equal(3464.73f, node.Position.X, floatTolerance);
        Assert.Equal(5624.75f, node.Position.Y, floatTolerance);
        Assert.Equal(0f, node.Position.Z, floatTolerance);
        Assert.Equal(0, node.IslandId);
        Assert.False(node.IsJunction);
        Assert.Empty(node.Connections);
    }


    [Fact]
    public async Task CanParseSmallNav() {
        // Arrange
        string text = """
/*{"systemTimeUCT_G":"2021-06-24 11:14:25","worldName":"Stratis","StreetArtist_Config":{"_flatMaxDrift":-1,"_juncMergeDistance":-1,"_humanEdited": true}}*/
navGrid = [
    [[3464.73,5624.75,0],0,false,[[239,0,19.3027],[75,0,17.8279]]],
    [[5244.62,5325.15,0],0,false,[[316,0,26.1947],[328,0,34.1498]]],
    [[4098.39,5413.42,0],0,true,[[196,0,21.4635],[20,0,53.6343],[51,0,24.8014]]],
    [[4076.44,6331.33,0],0,true,[[313,0,27.2533],[366,0,55.7751],[261,0,5.0124]]],
    [[3878.79,5527.74,0],0,true,[[120,0,15.601],[172,0,17.1676],[297,0,6.19422]]]
]
""";
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);

        // Assert
        if (result.TryPickT1(out Error<Exception> error, out Result<NavGridRoot> navGridRootRes))
            Assert.Fail(error.Value.ToString());
        NavGridRoot navGridRoot = navGridRootRes.Value;
        Assert.NotNull(navGridRoot);
        Assert.NotNull(navGridRoot.MetaInformation);
        Assert.Equal(5, navGridRoot.RoadNodes.Count);

        Assert.Equal(3464.73, navGridRoot.RoadNodes[0].Position.X, floatTolerance);
        Assert.Equal(5244.62, navGridRoot.RoadNodes[1].Position.X, floatTolerance);
        Assert.Equal(4098.39, navGridRoot.RoadNodes[2].Position.X, floatTolerance);
        Assert.Equal(4076.44, navGridRoot.RoadNodes[3].Position.X, floatTolerance);
        Assert.Equal(3878.79, navGridRoot.RoadNodes[4].Position.X, floatTolerance);
    }

    [Fact]
    public async Task CannotParseSyntaxicallyInvalidNav() {
        // Arrange
        string text = """
            /*{"systemTimeUCT_G":"2021-06-24 11:14:25","worldName":"Stratis","StreetArtist_Config":{"_flatMaxDrift":-1,"_juncMergeDistance":-1,"_humanEdited": true}}*/
            navGrid = [
                [[3464.73,5624.75,0],0,false,[[239,0,19.3027],[75,0,17.8279]]],
                [[5244.62,5325.15,0],0,false,[[316,0,26.1947],[328,0,34.1498]]],
                [[4098.39,5413.42,0],0,,[[196,0,21.4635],[20,0,53.6343],[51,0,24.8014]]],
                [[4076.44,6331.33,0],0,true,[[313,0,27.2533],[366,0,55.7751],[261,0,5.0124]]],
                [[3878.79,5527.74,0],0,true,[[120,0,15.601],[172,0,17.1676],[297,0,6.19422]]]
            ]
            """;
        Stream textStream = GetStringStream(text);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);
        Assert.True(result.TryPickT1(out Error<Exception> error, out _));

        // Assert
        testOutput.WriteLine($"Expected error {error.Value}");
        Assert.NotNull(error.Value);
    }

    [Fact]
    public async Task CanParseMediumNav() {
        // Arrange
        using FileStream textStream = new FileStream("TestData/Antistasi_Stratis.Stratis/navGrid.sqf", FileMode.Open, FileAccess.Read);
        A3aNgParser parser = new(textStream);

        // Act
        var result = await parser.ReadNavGridRoot(cancellationToken);

        // Assert
        if (result.TryPickT1(out Error<Exception> error, out Result<NavGridRoot> navGridRootRes))
            Assert.Fail(error.Value.ToString());
        NavGridRoot navGridRoot = navGridRootRes.Value;
        Assert.NotNull(navGridRoot);
        Assert.NotNull(navGridRoot.MetaInformation);

        Assert.Equal(3464.73, navGridRoot.RoadNodes[0].Position.X, floatTolerance);
        Assert.Equal(5244.62, navGridRoot.RoadNodes[1].Position.X, floatTolerance);
        Assert.Equal(4098.39, navGridRoot.RoadNodes[2].Position.X, floatTolerance);
        Assert.Equal(4076.44, navGridRoot.RoadNodes[3].Position.X, floatTolerance);
        Assert.Equal(3878.79, navGridRoot.RoadNodes[4].Position.X, floatTolerance);
        testOutput.WriteLine($"Last Node index: {navGridRoot.RoadNodes.Count - 1} Pos.X: {navGridRoot.RoadNodes.Last().Position.X}");
        Assert.Equal(395, navGridRoot.RoadNodes.Count);
        Assert.Equal(3102.44, navGridRoot.RoadNodes[394].Position.X, floatTolerance);
    }

    static Stream GetStringStream(string value) => new MemoryStream(Encoding.UTF8.GetBytes(value));
    const float floatTolerance = 0.001f;
}
