using System;

public class VectorUtility
{
    /// <summary>
    /// Computes the cosine similarity between two vectors.
    /// Cosine similarity is a measure of similarity between two non-zero vectors of an inner product space
    /// that measures the cosine of the angle between them.  The result is a value between -1 and 1.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The cosine similarity between the two vectors.</returns>
    public static float ComputeCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1 == null || vector2 == null || vector1.Length != vector2.Length)
        {
            return 0f; // Or throw an exception:  throw new ArgumentException("Vectors must not be null and must have the same length.");
        }

        float dotProduct = 0f;
        float magnitude1 = 0f;
        float magnitude2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0f;  // Prevent division by zero

        return dotProduct / (magnitude1 * magnitude2);
    }
}
