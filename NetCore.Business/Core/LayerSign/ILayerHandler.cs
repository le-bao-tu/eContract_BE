using iText.IO.Image;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface ILayerHandler
    {
        Task<ImageData> CreateLayerSignHandler(LayerModel model); 
    }
}
