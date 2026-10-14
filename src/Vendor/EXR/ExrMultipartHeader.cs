#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;

namespace Electron2DCodecs.EXR
{
    internal sealed class ExrMultipartHeader
    {
        public ExrMultipartHeader(IEnumerable<ExrHeader> headers)
        {
            Headers = headers?.ToList() ?? throw new ArgumentNullException(nameof(headers));
        }

        public IList<ExrHeader> Headers { get; }
    }
}
