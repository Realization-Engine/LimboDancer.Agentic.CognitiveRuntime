using System.Text;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The audit panel's tail (the Play Step Plan, task 5): read back from the file's end a block at a time, it gives the lines that reading the whole
/// file gave, split the same way.
/// </summary>
public sealed class FileAuditSinkTests : IDisposable
{
    private static readonly int[] ShortCounts = [1, 2, 3, 12];
    private static readonly int[] LongCounts = [1, 12, 100, 3999, 4000, 5000];
    private static readonly string[] OneTwo = ["one", "two"];
    private static readonly string[] TwoThree = ["two", "three"];

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-audit-tail-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private string Write(string text)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "audit.jsonl");
        File.WriteAllText(path, text, new UTF8Encoding(false));
        return path;
    }

    private static void SameAsTheWholeRead(string path, int count) => Assert.Equal(File.ReadLines(path).TakeLast(count), new FileAuditSink(path).Tail(count));

    [Fact]
    public void NoFileGivesNoLines() => Assert.Empty(new FileAuditSink(Path.Combine(root, "none.jsonl")).Tail(12));

    [Theory]
    [InlineData("")]
    [InlineData("one")]
    [InlineData("one\n")]
    [InlineData("one\ntwo\nthree")]
    [InlineData("one\ntwo\nthree\n")]
    [InlineData("one\n\nthree\n\n")]
    [InlineData("one\r\ntwo\r\n")]
    public void AFileShorterThanABlockGivesItsLines(string text)
    {
        var path = Write(text);
        foreach (var count in ShortCounts)
        {
            SameAsTheWholeRead(path, count);
        }

        Assert.Empty(new FileAuditSink(path).Tail(0));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFileOfManyBlocksGivesItsLastLines(bool endsWithANewline)
    {
        // About 400 KB, some 50 blocks, with lines of every length and a character of three bytes that falls across blocks.
        var text = new StringBuilder();
        for (var index = 0; index < 4000; index++)
        {
            text.Append("{\"line\":").Append(index).Append(",\"text\":\"").Append('x', index % 97).Append("€\"}").Append('\n');
        }

        var path = Write(endsWithANewline ? text.ToString() : text.ToString().TrimEnd('\n'));
        foreach (var count in LongCounts)
        {
            SameAsTheWholeRead(path, count);
        }
    }

    [Fact]
    public void ALineWrittenAfterTheFirstReadIsRead()
    {
        // The two Studios share the folder: a line another writes is in the next tail.
        var path = Write("one\ntwo\n");
        var sink = new FileAuditSink(path);
        Assert.Equal(OneTwo, sink.Tail(12));
        File.AppendAllText(path, "three\n");
        Assert.Equal(TwoThree, sink.Tail(2));
    }
}
