public class QdrantResult
{
    public string Id { get; set; }
    public int Version { get; set; }
    public float Score { get; set; }
    public Payload Payload { get; set; }
    public List<float> Vector { get; set; }
}