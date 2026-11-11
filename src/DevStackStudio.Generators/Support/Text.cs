using System.Text;

namespace DevStackStudio.Generators.Support;

internal sealed class TextBuilder
{
    private readonly StringBuilder _builder = new();

    public TextBuilder Line(string? value = null)
    {
        _builder.AppendLine(value ?? string.Empty);
        return this;
    }

    public override string ToString() => _builder.ToString();
}
