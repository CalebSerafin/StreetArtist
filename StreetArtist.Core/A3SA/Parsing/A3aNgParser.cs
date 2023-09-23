// Ignore Spelling: nav Ng

using OneOf;
using OneOf.Types;

using StreetArtist.Core.A3SA.Storage.Models;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StreetArtist.Core.A3SA.Parsing;
class A3aNgParser {
    public A3aNgParser(Stream navGridFileStream) {
        tokenReader = new(navGridFileStream) { Delimiters = delimiters };
    }

    public async ValueTask<OneOf<Result<NavGridRoot>, Error<Exception>>> ReadNavGridRoot(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }
    public async ValueTask<OneOf<Result<MetaInformation>, Error<Exception>>> ReadMetaInformation(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }
    public async ValueTask<OneOf<Result<Node>, Error<Exception>>> ReadNode(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }
    public async ValueTask<OneOf<Result<PositionATL>, Error<Exception>>> ReadPos3(CancellationToken cancellationToken) {
        if (!await tokenReader.AdvanceIfExpectedChar(arraySeparator, cancellationToken))
            return CreateErrorAtLocation($"Did not find expected {arraySeparator}");
        throw new NotImplementedException();
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
    public async ValueTask<OneOf<Result<bool>, Error<Exception>>> ReadConnection(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadComma(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadOpeningBracket(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }
    public async ValueTask<OneOf<Success, Error<Exception>>> ReadClosingBracket(CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }

    #region Fields
    readonly BufferedTokenReader tokenReader;
    const char arraySeparator = ',';
    const char openingBracket = '[';
    const char closingBracket = ']';
    static readonly char[] delimiters = new char[] { arraySeparator, openingBracket, closingBracket };
    #endregion

    Error<Exception> CreateErrorAtLocation(string message) {
        return new Error<Exception>(new Exception($"At character: {tokenReader.Position}; {message}"));
    }
}
