using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ContentTypeDetector
{
    public static bool IsPdf(string contentType)
    {
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }
}
