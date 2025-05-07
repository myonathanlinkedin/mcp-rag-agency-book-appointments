public class QdrantQueryResponse
{
    public List<QdrantResult> Result { get; set; }

    public QdrantQueryResponse()
    {
        Result = new List<QdrantResult>();
    }
}