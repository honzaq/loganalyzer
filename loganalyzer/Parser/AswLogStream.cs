using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace loganalyzer.Parser
{
    public class AswLogStream
    {
        private readonly TextReader mTextReader;
        private readonly int mBufferSize;
        private char[] mBuffer = null;
        private int mBufferLen;

        public AswLogStream(TextReader textReader)
            : this(textReader, 65536)
        {}

        public AswLogStream(TextReader textReader, int bufferSize)
        {
            mTextReader = textReader;
            mBufferSize = bufferSize;
        }

    }
}
