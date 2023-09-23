// Ignore Spelling: nav Ng

using System.Text;

using OneOf;
using OneOf.Types;

namespace StreetArtist.Core.A3SA.Parsing;

/// <summary>
/// Consumes stream and reads it in blocks to remainingBuffer data. <br/>
/// Stream may be at an undefined position after consumption. <br/>
/// </summary>
internal class BufferedTokenReader {
    public BufferedTokenReader(Stream stream) {
        this.stream = stream;
    }

    public int BlockSize { get; init; } = 256;
    public char[] Delimiters { get; init; } = defaultdelimiters;
    public long Position => (stream.CanRead ? stream.Position : 0 ) - bufferLength;
    public long LineNumber { get; private set; } = 0;
    public long ColumnNumber { get; private set; } = 0;

    /// <summary>
    /// Reads a token until a delimiter is found. <br/>
    /// The token does not include the delimiter. <br/>
    /// The delimiter is still left in the stream. <br/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<string> ReadToken(CancellationToken cancellationToken) =>
        ReadToken(Delimiters, cancellationToken);

    /// <summary>
    /// Reads a token until a delimiter is found. <br/>
    /// The token does not include the delimiter. <br/>
    /// The delimiter is still left in the stream. <br/>
    /// </summary>
    /// <param name="delimiters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<string> ReadToken(char[] delimiters, CancellationToken cancellationToken) {
        StringBuilder token = new();
        while (true) {
            if (!(await TryPeekChar(cancellationToken)).TryPickT0(out char nextChar, out _))
                goto ReturnToken;
            if (IsDelimeter(delimiters, nextChar))
                goto ReturnToken;
            AdvanceBuffer(1);
            token.Append(nextChar);
        }
ReturnToken:
        return token.ToString();
    }

    /// <summary>
    /// Reads a token until a delimiter is found. <br/>
    /// The delimiter length must be equal to or less than <see cref="BlockSize"/> <br/>
    /// The token does not include the delimiter. <br/>
    /// The delimiter is still left in the stream. <br/>
    /// </summary>
    /// <param name="delimiters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<string> ReadToken(string[] delimiters, CancellationToken cancellationToken) {
        StringBuilder token = new();
        int maxDelimiterLength = delimiters.Length > 0 ? delimiters.Select(x => x.Length).Max() : 1;
        maxDelimiterLength = int.Min(maxDelimiterLength, BlockSize);

        while (true) {
            ReadOnlyMemory<char> nextBuffer = await PeekExtendedBuffer(maxDelimiterLength, cancellationToken);
            // If empty, use rest as token.
            if (nextBuffer.IsEmpty)
                goto ReturnToken;

            int indexOfDeliminator = FindDelimeterSequence(delimiters, nextBuffer.Span);
            // If delimiter was found, append remainingBuffer before it, and return token.
            if (indexOfDeliminator >= 0) {
                ReadOnlyMemory<char> preDelimiter = nextBuffer[..indexOfDeliminator];
                token.Append(preDelimiter);
                AdvanceBuffer(preDelimiter.Length);
                goto ReturnToken;
            }
            // If no delimiter was not found, append remainingBuffer and continue searching.
            token.Append(nextBuffer);
            AdvanceBuffer(nextBuffer.Length);
        }
ReturnToken:
        return token.ToString();
    }

    /// <summary>
    /// Reads the rest of the internal remainingBuffer and stream as a string. <br/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<string> ReadRestAsString(CancellationToken cancellationToken) {
        StringBuilder token = new();
        do {
            token.Append(ReadBuffer());
        } while (await LoadNextBlock(cancellationToken));
        return token.ToString();
    }

    #region Buffer
    /// <summary>
    /// Tries to get the next character. <br/>
    /// Does not advance the position. <br/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<OneOf<char, None>> TryPeekChar(CancellationToken cancellationToken) {
        if (PeekBuffer().Length == 0) {
            if (!await LoadNextBlock(cancellationToken))
                return new None();
        }
        return PeekBuffer().Span[0];
    }

    /// <summary>
    /// Tries to read the next character. <br/>
    /// Advances the position. <br/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<OneOf<char, None>> TryReadChar(CancellationToken cancellationToken) {
        return (await TryPeekChar(cancellationToken)).Match<OneOf<char, None>>(
            character => { AdvanceBuffer(1); return character; },
            none => none
        );
    }

    /// <summary>
    /// Tries to peek the next character. <br/>
    /// If the character is expected, then it advances the position and returns true. <br/>
    /// If the character is not expected, or the stream is finished, it does not advance the position and returns false. <br/>
    /// </summary>
    /// <param name="expected"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<bool> AdvanceIfExpectedChar(char expected, CancellationToken cancellationToken) {
        if (!(await TryPeekChar(cancellationToken)).TryPickT0(out char actual, out _) || expected != actual)
            return false;
        AdvanceBuffer(1);
        return true;
    }

    /// <summary>
    /// Tries to peek the next sequence. <br/>
    /// If the sequence is expected, then it advances the position and returns true. <br/>
    /// If the sequence is not expected, or the stream is finished, it does not advance the position and returns false. <br/>
    /// </summary>
    /// <param name="expected"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<bool> AdvanceIfExpectedSequence(ReadOnlyMemory<char> expected, CancellationToken cancellationToken) {
        int expectedLength = expected.Length;
        ReadOnlyMemory<char> buffer = await PeekExtendedBuffer(expectedLength, cancellationToken);
        bool isEqual = buffer.Length >= expectedLength && expected.Span.SequenceEqual(buffer.Span[..expectedLength]);
        if (isEqual)
            AdvanceBuffer(expectedLength);
        return isEqual;
    }

    /// <inheritdoc cref="AdvanceIfExpectedSequence(ReadOnlyMemory{char}, CancellationToken)"/>
    public ValueTask<bool> AdvanceIfExpectedSequence(string expected, CancellationToken cancellationToken) =>
        AdvanceIfExpectedSequence(expected.AsMemory(), cancellationToken);

    /// <summary>
    /// Checks if the passed character is one of the expected deliminters.
    /// </summary>
    /// <param name="delimiters"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    static bool IsDelimeter(IEnumerable<char> delimiters, char character) {
        foreach (char delimiter in delimiters)
            if (character == delimiter)
                return true;
        return false;
    }

    /// <summary>
    /// The passed sequence must be longer than the delimiter to find a successful match.
    /// </summary>
    /// <param name="delimiters"></param>
    /// <param name="sequence"></param>
    /// <returns></returns>
    static bool IsDelimeterSequence(string[] delimiters, ReadOnlySpan<char> sequence) {
        foreach (ReadOnlySpan<char> delimiter in delimiters)
            if (sequence.Length >= delimiter.Length && delimiter.SequenceEqual(sequence[..delimiter.Length]))
                return true;
        return false;
    }

    /// <summary>
    /// The passed sequence must be longer than the delimiter to find a successful match.
    /// </summary>
    /// <param name="delimiters"></param>
    /// <param name="sequence"></param>
    /// <returns></returns>
    static int FindDelimeterSequence(string[] delimiters, ReadOnlySpan<char> sequence) {
        foreach (ReadOnlySpan<char> delimiter in delimiters) {
            int sequenceIndex = sequence.IndexOf(delimiter);
            if (sequenceIndex >= 0)
                return sequenceIndex;
        }
        return -1;
    }

    /// <summary>
    /// Reads a whole or partial block into the remainingBuffer <br/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns> True if the remainingBuffer contains items. False if no more items could be read. </returns>
    async ValueTask<bool> LoadNextBlock(CancellationToken cancellationToken) {
        if (buffer.Length == 0)
            buffer = new char[BlockSize];
        else if (!PeekBuffer().IsEmpty)
            return true;

        bufferIndex = 0;
        if (stream.CanRead)
            using (StreamReader reader = new(stream)) {
                bufferLength = await reader.ReadAsync(buffer, cancellationToken);
            }
        return bufferLength > 0;
    }

    /// <summary>
    /// Appends a whole or partial block into the remainingBuffer <br/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns> True if the remainingBuffer contains items. False if no more items could be read. </returns>
    async ValueTask<bool> LoadNextBlockToAppend(CancellationToken cancellationToken) {
        if (bufferLength <= 0 || bufferIndex >= buffer.Length)
            return await LoadNextBlock(cancellationToken);

        int remainingLength = BlockSize - bufferLength;
        if (remainingLength <= 0)
            return true;
        // Repack remainingBuffer contents to start.
        PeekBuffer().CopyTo(buffer);
        bufferIndex = 0;
        // Append remainingBuffer with new items.
        if (stream.CanRead)
            using (StreamReader reader = new(stream)) {
                Memory<char> destination = buffer.AsMemory()[bufferLength..];
                bufferLength += await reader.ReadAsync(destination, cancellationToken);
            }
        return bufferLength > 0;
    }

    /// <summary>
    /// Gets Content from the remainingBuffer. <br/>
    /// </summary>
    /// <returns></returns>
    ReadOnlyMemory<char> PeekBuffer() => bufferLength > 0 ? buffer.AsMemory(bufferIndex, bufferLength) : ReadOnlyMemory<char>.Empty;

    /// <summary>
    /// Gets Content from the remainingBuffer and advances the position to consume the remainingBuffer. <br/>
    /// </summary>
    /// <returns></returns>
    ReadOnlyMemory<char> ReadBuffer() {
        if (bufferLength <= 0)
            return ReadOnlyMemory<char>.Empty;
        ReadOnlyMemory<char> memory = buffer.AsMemory(bufferIndex, bufferLength);
        AdvanceBuffer(memory.Length);
        return memory;
    }

    /// <summary>
    /// Gets Content from the remainingBuffer. <br/>
    /// Attempts to refill remainingBuffer if it is not full. <br/>
    /// </summary>
    /// <returns></returns>
    async ValueTask<ReadOnlyMemory<char>> PeekFullBuffer(CancellationToken cancellationToken) {
        await LoadNextBlockToAppend(cancellationToken);
        return PeekBuffer();
    }

    /// <summary>
    /// Gets Content from the remainingBuffer. <br/>
    /// Attempts to refill remainingBuffer if it is shorter than <paramref name="suggestedMinLength"/>. <br/>
    /// </summary>
    /// <returns></returns>
    async ValueTask<ReadOnlyMemory<char>> PeekExtendedBuffer(int suggestedMinLength, CancellationToken cancellationToken) {
        if (bufferLength < suggestedMinLength)
            await LoadNextBlockToAppend(cancellationToken);
        return PeekBuffer();
    }

    void AdvanceBuffer(int amount) {
        RecordNewLines(PeekBuffer()[..amount].Span);
        bufferIndex += amount;
        bufferLength -= amount;
    }

    void RecordNewLines(ReadOnlySpan<char> sequence) {
        (int lines, int columnNumber) = CountNewLines(sequence);
        LineNumber += lines;
        if (lines > 0)
            ColumnNumber = 0;
        ColumnNumber += columnNumber;
    }

    static (int Lines, int ColumnNumber) CountNewLines(ReadOnlySpan<char> sequence) {
        int count = 0;
        while (true) {
            int index = sequence.IndexOf(newLine);
            if (index < 0)
                return (count, sequence.Length - 1);
            sequence = sequence[(index + 1)..];
            count++;
        }
    }

    char[] buffer = Array.Empty<char>();
    int bufferIndex = 0;
    int bufferLength = 0;
    const char newLine = '\n';
    static readonly char[] defaultdelimiters = new char[] { ',', '[', ']' };
    readonly Stream stream;
    #endregion

}