public interface IImageService
{
    Task<ImageResponse> Process(ImageRequest image);
}