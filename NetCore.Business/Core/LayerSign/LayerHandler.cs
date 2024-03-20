using CoreHtmlToImage;
using iText.IO.Image;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class LayerHandler : ILayerHandler
    {
        public async Task<ImageData> CreateLayerSignHandler(LayerModel model)
        {
            var height = model.Styles[DictionaryStyleKey.Height];
            var width = model.Styles[DictionaryStyleKey.Width];

            if (string.IsNullOrEmpty(height) || string.IsNullOrEmpty(width))
            {
                throw new Exception("Width và Height không được null");
            }

            var color = model.Styles[DictionaryStyleKey.Color];
            // set màu mặc định
            if (string.IsNullOrEmpty(color))
            {
                color = "black";
            }

            var font = model.Styles[DictionaryStyleKey.Font];
            // set font mặc định
            if (string.IsNullOrEmpty(font))
            {
                color = "'open sans',sans-serif";
            }

            string html = string.Empty;

            // case 1 nếu đủ cả 3
            if (!string.IsNullOrEmpty(model.Logo) && !string.IsNullOrEmpty(model.Image) && !string.IsNullOrEmpty(model.Text))
            {
                html = $@"<div style='height: {height}px; width: {width}px;'>
                            <img style='width: 50%; height: 100%; float: left;' src='data:image/png;base64, {model.Logo}'/>
                            <div style='width: 50%; height: 100%; float: right;'>
                                <img style='width: 100%; height: 40%;' src='data:image/png;base64, {model.Image}'/>                                 
                                <div style='width: 100%; height: 60%; padding: 5px; color: {color}; font-family: {font};'>{model.Text}</div>
                            </div>
                        </div>";
            } 
            else
            {
                // case 2 nếu có ảnh và text
                if (!string.IsNullOrEmpty(model.Image) && !string.IsNullOrEmpty(model.Text))
                {
                    html = $@"<div style='height: {height}px; width: {width}px;'>                                 
                                <img style='width: 100%; height: 40%;' src='data:image/png;base64, {model.Image}'/>    
                                <div style='width: 100%; height: 60%; padding: 5px; color: {color}; font-family: {font};'>{model.Text}</div>  
                            </div>";
                }
                // case 3 nếu có logo và text
                else if (!string.IsNullOrEmpty(model.Logo) && !string.IsNullOrEmpty(model.Text))
                {
                    html = $@"<div style='height: {height}px; width: {width}px;'>                                 
                                <img style='width: 50%; height: 100%; float: left;' src='data:image/png;base64, {model.Logo}'/>                                      
                                <div style='width: 50%; height: 100%; float: right;'>
                                    <span style='padding: 5px; color: {color}; font-family: {font};'>{model.Text}</span>
                                </div>                                
                            </div>";
                } 
                // case 4 nếu chỉ có 1 trong 3
                else
                {
                    if (!string.IsNullOrEmpty(model.Logo))
                        html = $@"<img style='width: {width}px; height: {height}px;' src='data:image/png;base64, {model.Logo}'/>";
                    else if (!string.IsNullOrEmpty(model.Image))
                        html = $@"<img style='width: {width}px; height: {height}px;' src='data:image/png;base64, {model.Image}'/>";
                    else
                        html = $@"<div style='width: {width}px; height: {height}px; padding: 5px; color: {color}; font-family: {font};'>{model.Text}</div>";
                }
            }

            var converter = new HtmlConverter();
            // convert html string sang bytes            
            var bytes = converter.FromHtmlString(html, Convert.ToInt32(width), CoreHtmlToImage.ImageFormat.Png, 300);

            // tạo imageData của iText7
            ImageData imageData = ImageDataFactory.Create(bytes);

            return imageData;
        }      
    }    
}
