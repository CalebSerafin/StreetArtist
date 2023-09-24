// Ignore Spelling: nav Ng

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using OneOf;
using OneOf.Types;

using StreetArtist.Core.A3SA.Storage.Models;

namespace StreetArtist.Core.A3SA.Parsing;
class A3aNgParser {
    public A3aNgParser(Stream navGridFileStream) {
        tokenReader = new(navGridFileStream) { Delimiters = delimiters };
    }

    public async ValueTask<OneOf<Result<NavGridRoot>, Error<Exception>>> ReadNavGridRoot(CancellationToken cancellationToken) {
        Error<Exception> error;

        // MetaInformation is optional
        MetaInformation? metaInformation = null;
        if ((await ReadMetaInformation(cancellationToken)).TryPickT0(out Result<MetaInformation> metaInformationRes, out _))
            metaInformation = metaInformationRes.Value;
        // node = 
        await tokenReader.ReadToken(cancellationToken);
        // RoadNodes
        if ((await ReadArray(ReadRoadNode, cancellationToken)).TryPickT1(out error, out Result<List<RoadNode>> roadNodes))
            return error;

        return new Result<NavGridRoot>(new NavGridRoot() {
            MetaInformation = metaInformation,
            RoadNodes = roadNodes.Value
        });
    }


    public async ValueTask<OneOf<Result<MetaInformation>, Error<Exception>>> ReadMetaInformation(CancellationToken cancellationToken) {
        Error<Exception> error;

        // /*
        if (ShouldExit(await ReadOpeningComment(cancellationToken), out error))
            return error;
        // JSON
        string jsonData = await tokenReader.ReadToken(commentDelimiters, cancellationToken);
        MetaInformation? metaInformation;
        try {
            metaInformation = JsonSerializer.Deserialize<MetaInformation>(jsonData, jsonSerializerOptions);
        } catch (Exception exception) {
            Exception wrapper = new Exception(GetMessageAtLocation($"Failed to deserialize MetaInformation: {exception}"), exception);
            return new Error<Exception>(exception);
        }
        if (metaInformation is null)
            return CreateErrorAtLocation("Failed to deserialize MetaInformation");
        // */
        if (ShouldExit(await ReadClosingComment(cancellationToken), out error))
            return error;

        return new Result<MetaInformation>(metaInformation);
    }
    public async ValueTask<OneOf<Result<RoadNode>, Error<Exception>>> ReadRoadNode(CancellationToken cancellationToken) {
        return (await ReadStructure(ReadPosATL, ReadFloat, ReadBool, ct => ReadArray(ReadConnection, ct), cancellationToken)).Match<OneOf<Result<RoadNode>, Error<Exception>>>(
            result => {
                var (position, islandId, isJunction, connections) = result.Value;
                return new Result<RoadNode>(new RoadNode() {
                    Position = position,
                    IslandId = (int)islandId,
                    IsJunction = isJunction,
                    Connections = connections
                });
            },
            error => error
        );
    }
    public async ValueTask<OneOf<Result<Connection>, Error<Exception>>> ReadConnection(CancellationToken cancellationToken) {
        return (await ReadStructure(ReadFloat, ReadFloat, ReadFloat, cancellationToken)).Match<OneOf<Result<Connection>, Error<Exception>>>(
            result => {
                var (indexOfConnectedNode, roadType, trueDrivingDistance) = result.Value;
                return new Result<Connection>(new Connection() {
                    IndexOfConnectedNode = (int)indexOfConnectedNode,
                    RoadType = (int)roadType,
                    TrueDrivingDistance = trueDrivingDistance,
                });
            },
            error => error
        );
    }

    public async ValueTask<OneOf<Result<PositionATL>, Error<Exception>>> ReadPosATL(CancellationToken cancellationToken) {
        return (await ReadStructure(ReadFloat, ReadFloat, ReadFloat, cancellationToken)).Match<OneOf<Result<PositionATL>, Error<Exception>>>(
            result => {
                var (x, y, z) = result.Value;
                return new Result<PositionATL>(new PositionATL(x, y, z));
            },
            error => error
        );
    }
    public async ValueTask<OneOf<Result<float>, Error<Exception>>> ReadFloat(CancellationToken cancellationToken) {
        ReadOnlyMemory<char> floatString = (await tokenReader.ReadToken(cancellationToken)).AsMemory();
        floatString = floatString.Trim();
        if (float.TryParse(floatString.Span, out float scalar))
            return new Result<float>(scalar);
        return CreateErrorAtLocation($"Could not parse float from \"{floatString}\"");
    }
    public async ValueTask<OneOf<Result<bool>, Error<Exception>>> ReadBool(CancellationToken cancellationToken) {
        ReadOnlyMemory<char> boolString = (await tokenReader.ReadToken(cancellationToken)).AsMemory();
        boolString = boolString.Trim();
        if (bool.TryParse(boolString.Span, out bool @bool))
            return new Result<bool>(@bool);
        return CreateErrorAtLocation($"Could not parse bool from \"{boolString}\"");
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadComma(CancellationToken cancellationToken) {
        // whitespace
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        if (await tokenReader.AdvanceIfExpectedChar(arraySeparator, cancellationToken))
            return new Success();
        return CreateErrorAtLocation($"Did not find expected comma.");
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadOpeningBracket(CancellationToken cancellationToken) {
        // whitespace
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        if (await tokenReader.AdvanceIfExpectedChar(openingBracket, cancellationToken))
            return new Success();
        return CreateErrorAtLocation($"Did not find expected opening bracket.");
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadClosingBracket(CancellationToken cancellationToken) {
        // whitespace
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        if (await tokenReader.AdvanceIfExpectedChar(closingBracket, cancellationToken))
            return new Success();
        return CreateErrorAtLocation($"Did not find expected closing bracket.");
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadOpeningComment(CancellationToken cancellationToken) {
        // whitespace
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        if (await tokenReader.AdvanceIfExpectedSequence(openingComment, cancellationToken))
            return new Success();
        return CreateErrorAtLocation($"Did not find expected opening comment.");
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadClosingComment(CancellationToken cancellationToken) {
        // whitespace
        await tokenReader.AdvanceOverWhitespace(cancellationToken);
        if (await tokenReader.AdvanceIfExpectedSequence(closingComment, cancellationToken))
            return new Success();
        return CreateErrorAtLocation($"Did not find expected closing comment.");
    }


    public async ValueTask<OneOf<Result<List<T>>, Error<Exception>>> ReadArray<T>(Func<CancellationToken, ValueTask<OneOf<Result<T>, Error<Exception>>>> readElement, CancellationToken cancellationToken) {
        Error<Exception> error;
        // [
        if (ShouldExit(await ReadOpeningBracket(cancellationToken), out error))
            return error;
        // ]
        List<T> items = new();
        if (await tokenReader.AdvanceIfExpectedChar(']', cancellationToken))
            return new Result<List<T>>(items);
        // T
        while (true) {
            // T
            if ((await readElement(cancellationToken)).TryPickT1(out error, out Result<T> result))
                return error;
            items.Add(result.Value);
            // whitespace
            await tokenReader.AdvanceOverWhitespace(cancellationToken);
            // ]
            if (await tokenReader.AdvanceIfExpectedChar(']', cancellationToken))
                return new Result<List<T>>(items);
            // ,
            if (ShouldExit(await ReadComma(cancellationToken), out error))
                return error;
        }
    }

    #region Read Structure
    public async ValueTask<OneOf<Result<ValueTuple<T1, T2, T3>>, Error<Exception>>> ReadStructure<T1, T2, T3>(
        Func<CancellationToken, ValueTask<OneOf<Result<T1>, Error<Exception>>>> readElement1,
        Func<CancellationToken, ValueTask<OneOf<Result<T2>, Error<Exception>>>> readElement2,
        Func<CancellationToken, ValueTask<OneOf<Result<T3>, Error<Exception>>>> readElement3,
        CancellationToken cancellationToken
    ) {
        Error<Exception> error;

        // [
        if (ShouldExit(await ReadOpeningBracket(cancellationToken), out error))
            return error;
        // T1
        if ((await readElement1(cancellationToken)).TryPickT1(out error, out Result<T1> result1))
            return error;
        // ,
        if (ShouldExit(await ReadComma(cancellationToken), out error))
            return error;
        // T2
        if ((await readElement2(cancellationToken)).TryPickT1(out error, out Result<T2> result2))
            return error;
        // ,
        if (ShouldExit(await ReadComma(cancellationToken), out error))
            return error;
        // T3
        if ((await readElement3(cancellationToken)).TryPickT1(out error, out Result<T3> result3))
            return error;
        // ]
        if (ShouldExit(await ReadClosingBracket(cancellationToken), out error))
            return error;

        return new Result<ValueTuple<T1, T2, T3>>(new ValueTuple<T1, T2, T3>(result1.Value, result2.Value, result3.Value));
    }


    public async ValueTask<OneOf<Result<ValueTuple<T1, T2, T3, T4>>, Error<Exception>>> ReadStructure<T1, T2, T3, T4>(
        Func<CancellationToken, ValueTask<OneOf<Result<T1>, Error<Exception>>>> readElement1,
        Func<CancellationToken, ValueTask<OneOf<Result<T2>, Error<Exception>>>> readElement2,
        Func<CancellationToken, ValueTask<OneOf<Result<T3>, Error<Exception>>>> readElement3,
        Func<CancellationToken, ValueTask<OneOf<Result<T4>, Error<Exception>>>> readElement4,
        CancellationToken cancellationToken
    ) {
        Error<Exception> error;

        // [
        if (ShouldExit(await ReadOpeningBracket(cancellationToken), out error))
            return error;
        // T1
        if ((await readElement1(cancellationToken)).TryPickT1(out error, out Result<T1> result1))
            return error;
        // ,
        if (ShouldExit(await ReadComma(cancellationToken), out error))
            return error;
        // T2
        if ((await readElement2(cancellationToken)).TryPickT1(out error, out Result<T2> result2))
            return error;
        // ,
        if (ShouldExit(await ReadComma(cancellationToken), out error))
            return error;
        // T3
        if ((await readElement3(cancellationToken)).TryPickT1(out error, out Result<T3> result3))
            return error;
        // ,
        if (ShouldExit(await ReadComma(cancellationToken), out error))
            return error;
        // T4
        if ((await readElement4(cancellationToken)).TryPickT1(out error, out Result<T4> result4))
            return error;
        // ]
        if (ShouldExit(await ReadClosingBracket(cancellationToken), out error))
            return error;

        return new Result<ValueTuple<T1, T2, T3, T4>>(new ValueTuple<T1, T2, T3, T4>(result1.Value, result2.Value, result3.Value, result4.Value));
    }
    #endregion

    static A3aNgParser() {
        jsonSerializerOptions = new(JsonSerializerDefaults.Web) { AllowTrailingCommas = true };
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    #region Fields
    readonly BufferedTokenReader tokenReader;
    readonly static JsonSerializerOptions jsonSerializerOptions;
    const char arraySeparator = ',';
    const char openingBracket = '[';
    const char closingBracket = ']';
    const string openingComment = "/*";
    const string closingComment = "*/";
    static readonly char[] delimiters = new char[] { arraySeparator, openingBracket, closingBracket };
    static readonly string[] commentDelimiters = new string[] { openingComment, closingComment };
    #endregion

    bool ShouldExit(OneOf<Success, Error<Exception>> oneOf, out Error<Exception> error) =>
        oneOf.TryPickT1(out error, out _);

    Error<Exception> CreateErrorAtLocation(string message) {
        return new Error<Exception>(new Exception(GetMessageAtLocation(message)));
    }

    string GetMessageAtLocation(string message) {
        return $"Character: {tokenReader.CharacterNumber}, Line: {tokenReader.LineNumber}, Column: {tokenReader.ColumnNumber}; {message}";
    }
}
