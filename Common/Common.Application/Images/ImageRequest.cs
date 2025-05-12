public class ImageRequest
{
    public ImageRequest(Stream content) => Content = content;

    public Stream Content { get; }
}