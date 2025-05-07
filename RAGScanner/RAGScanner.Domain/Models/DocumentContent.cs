public class DocumentContent : IEquatable<DocumentContent>
{
    public string Content { get; }
    public int Index { get; }

    public DocumentContent(string content, int index)
    {
        Content = content;
        Index = index;
    }

    public bool Equals(DocumentContent? other) => other is not null && Content == other.Content && Index == other.Index;
    public override int GetHashCode() => HashCode.Combine(Content, Index);
}