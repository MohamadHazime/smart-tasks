using SmartTasks.Api.Application.Ai;
using SmartTasks.Api.Domain;
using SmartTasks.Api.Infrastructure.Ai;

namespace SmartTasks.Api.Tests.Infrastructure;

public class SubTaskSuggestionParserTests
{
    [Fact]
    public void Parse_PlainJsonArray_ReturnsTrimmedTitles()
    {
        var result = SubTaskSuggestionParser.Parse("""[" Book venue ", "Send invites", "Order food"]""");

        Assert.Equal(new[] { "Book venue", "Send invites", "Order food" }, result);
    }

    [Fact]
    public void Parse_ArrayWrappedInCodeFenceAndProse_ExtractsArray()
    {
        const string content = """
            Sure! Here are some subtasks:
            ```json
            ["Draft outline", "Write intro", "Review"]
            ```
            """;

        var result = SubTaskSuggestionParser.Parse(content);

        Assert.Equal(3, result.Count);
        Assert.Equal("Draft outline", result[0]);
    }

    [Fact]
    public void Parse_DropsBlanksAndCaseInsensitiveDuplicates()
    {
        var result = SubTaskSuggestionParser.Parse("""["One", "one", "", "  ", null, "Two", "Three"]""");

        Assert.Equal(new[] { "One", "Two", "Three" }, result);
    }

    [Fact]
    public void Parse_CapsAtMaxSuggestions()
    {
        var result = SubTaskSuggestionParser.Parse("""["a","b","c","d","e","f","g","h"]""");

        Assert.Equal(SubTaskSuggestionParser.MaxSuggestions, result.Count);
    }

    [Fact]
    public void Parse_TruncatesTitlesToDomainMaxLength()
    {
        var longTitle = new string('x', SubTask.TitleMaxLength + 50);

        var result = SubTaskSuggestionParser.Parse($"""["{longTitle}", "Two", "Three"]""");

        Assert.Equal(SubTask.TitleMaxLength, result[0].Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("I cannot help with that.")]
    [InlineData("[1, 2, 3]")]
    [InlineData("""["only", "two"]""")]
    [InlineData("""{"subtasks": "not an array"}""")]
    public void Parse_InvalidContent_ThrowsAiServiceException(string? content)
    {
        Assert.Throws<AiServiceException>(() => SubTaskSuggestionParser.Parse(content));
    }
}
