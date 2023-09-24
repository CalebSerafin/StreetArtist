using System.Text;

using StreetArtist.Core.A3SA.Parsing;

namespace StreetArtist.Core.Tests.A3SA.Parsing;
public class BufferedTokenReaderTests {
    public BufferedTokenReaderTests(ITestOutputHelper testOutput) {
        this.testOutput = testOutput;
        cancellationToken = CancellationToken.None;
    }
    readonly ITestOutputHelper testOutput;
    readonly CancellationToken cancellationToken;

    [Fact]
    public async Task CanReadEmptyStream() {
        // Arrange
        Stream stream = GetStringStream("");
        BufferedTokenReader tokenReader = new(stream);

        // Act
        string characters = await tokenReader.ReadToken(cancellationToken);

        // Assert
        Assert.Empty(characters);
        Assert.False((await tokenReader.TryReadChar(cancellationToken)).TryPickT0(out _, out _));
    }

    [Fact]
    public async Task CanReadTokenStream() {
        // Arrange
        string token = "Hello World!";
        Stream stream = GetStringStream(token);
        BufferedTokenReader tokenReader = new(stream);

        // Act
        string characters = await tokenReader.ReadToken(cancellationToken);

        // Assert
        Assert.Equal(token, characters);
        Assert.False((await tokenReader.TryReadChar(cancellationToken)).TryPickT0(out _, out _));
    }

    [Fact]
    public async Task CanReadTripleTokenStream() {
        // Arrange
        string token1 = "Hello World!";
        string token2 = "0.6364";
        string token3 = "true";
        string remainder = "]";
        Stream stream = GetStringStream($"{token1},{token2},{token3}{remainder}");
        BufferedTokenReader tokenReader = new(stream);

        // Act
        string characters1 = await tokenReader.ReadToken(cancellationToken);
        Assert.True(await tokenReader.AdvanceIfExpectedChar(',', cancellationToken));
        string characters2 = await tokenReader.ReadToken(cancellationToken);
        Assert.True(await tokenReader.AdvanceIfExpectedChar(',', cancellationToken));
        string characters3 = await tokenReader.ReadToken(cancellationToken);
        Assert.False(await tokenReader.AdvanceIfExpectedChar(',', cancellationToken));

        // Assert
        Assert.Equal(token1, characters1);
        Assert.Equal(token2, characters2);
        Assert.Equal(token3, characters3);
        Assert.Equal(remainder, await tokenReader.ReadRestAsString(cancellationToken));
    }

    [Fact]
    public async Task CanReadSequenceDelimitersFromEmptyStream() {
        // Arrange
        string[] delimiters = new string[] { "*/" };
        Stream stream = GetStringStream("");
        BufferedTokenReader tokenReader = new(stream);

        // Act
        string characters = await tokenReader.ReadToken(delimiters, cancellationToken);

        // Assert
        Assert.Empty(characters);
        Assert.False((await tokenReader.TryReadChar(cancellationToken)).TryPickT0(out _, out _));
    }

    [Fact]
    public async Task CanSingleReadSequenceDelimitersStream() {
        // Arrange
        string[] delimiters = new string[] { "*/" };
        string token = "Hello";
        string remainder = "*/World";
        Stream stream = GetStringStream($"{token}{remainder}");
        BufferedTokenReader tokenReader = new(stream);

        // Act
        string characters = await tokenReader.ReadToken(delimiters, cancellationToken);
        string actualRemainder = await tokenReader.ReadToken(Array.Empty<string>(), cancellationToken);

        // Assert
        Assert.Equal(token, characters);
        Assert.Equal(remainder, actualRemainder);
        Assert.False((await tokenReader.TryReadChar(cancellationToken)).TryPickT0(out _, out _));
    }

    [Fact]
    public async Task CanReadTripleSequenceDelimitersStream() {
        // Arrange
        string token1 = "Hello World!";
        string token2 = "0.6364";
        string token3 = "true";
        string openingDelimiter = "/*";
        string closingDelimiter = "*/";
        string[] openingDelimiters = new string[] { openingDelimiter };
        string[] closingDelimiters = new string[] { closingDelimiter };
        string remainder = "Y";

        Stream stream = GetStringStream($"/*{token1}*/Lol/*{token2}*/Hey!/*{token3}*/{remainder}");
        BufferedTokenReader tokenReader = new(stream);

        // Act
        _ = await tokenReader.ReadToken(openingDelimiters, cancellationToken);

        Assert.True(await tokenReader.AdvanceIfExpectedSequence(openingDelimiter, cancellationToken));
        string characters1 = await tokenReader.ReadToken(closingDelimiters, cancellationToken);
        Assert.True(await tokenReader.AdvanceIfExpectedSequence(closingDelimiter, cancellationToken));

        _ = await tokenReader.ReadToken(openingDelimiters, cancellationToken);

        Assert.True(await tokenReader.AdvanceIfExpectedSequence(openingDelimiter, cancellationToken));
        string characters2 = await tokenReader.ReadToken(closingDelimiters, cancellationToken);
        Assert.True(await tokenReader.AdvanceIfExpectedSequence(closingDelimiter, cancellationToken));

        _ = await tokenReader.ReadToken(openingDelimiters, cancellationToken);

        Assert.True(await tokenReader.AdvanceIfExpectedSequence(openingDelimiter, cancellationToken));
        string characters3 = await tokenReader.ReadToken(closingDelimiters, cancellationToken);
        Assert.True(await tokenReader.AdvanceIfExpectedSequence(closingDelimiter, cancellationToken));

        Assert.False(await tokenReader.AdvanceIfExpectedSequence(openingDelimiter, cancellationToken));

        // Assert
        Assert.Equal(token1, characters1);
        Assert.Equal(token2, characters2);
        Assert.Equal(token3, characters3);
        Assert.Equal(remainder, await tokenReader.ReadRestAsString(cancellationToken));
    }

    [Fact]
    public async Task CanAdvanceOverNoWhiteSpace() {
        // Arrange
        string token = "Hello";
        Stream stream = GetStringStream($"{token}");
        BufferedTokenReader tokenReader = new(stream);

        // Act
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        string characters = await tokenReader.ReadToken(cancellationToken);

        // Assert
        Assert.Equal(token, characters);
    }

    [Fact]
    public async Task CanAdvanceOverWhiteSpace() {
        // Arrange
        string token = "Hello";
        Stream stream = GetStringStream($"  {token}");
        BufferedTokenReader tokenReader = new(stream);

        // Act
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        string characters = await tokenReader.ReadToken(cancellationToken);

        // Assert
        Assert.Equal(token, characters);
    }

    [Fact]
    public async Task ErrorsAreReportedAtCorrectPosition() {
        // Arrange
        string token = " 2 ";
        int errorCharacter = 12;
        int errorLine = 2;
        int errorColumn = 9;
        string text = $$"""
            [
                [{{token}}, ]
            ]
            """;
        Stream stream = GetStringStream(text);
        BufferedTokenReader tokenReader = new(stream);

        // Act
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        Assert.True(await tokenReader.AdvanceIfExpectedChar('[', cancellationToken));
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        Assert.True(await tokenReader.AdvanceIfExpectedChar('[', cancellationToken));
        Assert.Equal(token, await tokenReader.ReadToken(cancellationToken));
        Assert.False(await tokenReader.AdvanceIfExpectedChar(']', cancellationToken));
        testOutput.WriteLine($"Character: {tokenReader.CharacterNumber}, Line: {tokenReader.LineNumber}, Column: {tokenReader.ColumnNumber};");
        Assert.Equal(errorCharacter, tokenReader.CharacterNumber);
        Assert.Equal(errorLine, tokenReader.LineNumber);
        Assert.Equal(errorColumn, tokenReader.ColumnNumber);
        //Assert.Equal(1, await tokenReader.LineNumber);

        // Assert

    }

    static Stream GetStringStream(string value) => new MemoryStream(Encoding.UTF8.GetBytes(value));
}
