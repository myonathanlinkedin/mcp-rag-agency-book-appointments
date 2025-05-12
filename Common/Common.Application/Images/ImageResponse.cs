public class ImageResponse
{
    public ImageResponse(
        byte[] originalContent,
        byte[] thumbnailContent)
    {
        OriginalContent = originalContent;
        ThumbnailContent = thumbnailContent;
    }

    public byte[] OriginalContent { get; }

    public byte[] ThumbnailContent { get; }
}