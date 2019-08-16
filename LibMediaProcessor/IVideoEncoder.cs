using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibMediaProcessor
{
    public interface IVideoEncoder
    {
        void Execute(VideoEncodeJob job);
    }
}
