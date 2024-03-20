using Com.Ascertia.ADSS.Client.API.Signing;
using Com.Ascertia.ADSS.Client.API.Util;
using Microsoft.AspNetCore.Hosting;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace NetCore.Business
{
    public class ADSSCoreHandler : IADSSCoreHandler
    {
        private IWebHostEnvironment _hostEnvironment;        

        public ADSSCoreHandler(IWebHostEnvironment environment)
        {
            _hostEnvironment = environment;
        }

        public NetCore.Shared.Response SignADSSWithNoExistingBlankSignatureField(ADSSCoreModelRequest model, SystemLogModel systemLog)
        {     
            try
            {
                Log.Information($"{systemLog.TraceId} - Start SignADSSWithNoExistingBlankSignatureField");
                Log.Information($"{systemLog.TraceId} - Model - {JsonSerializer.Serialize(model)}");
                string documentPath = Path.Combine(_hostEnvironment.WebRootPath + "/files", "test_file.pdf");

                PdfSigningRequest obj_signRequest = new PdfSigningRequest(model.OriginatorId, documentPath);

                if (model.Documents != null && model.Documents.Count > 0)
                {
                    foreach (var doc in model.Documents)
                        obj_signRequest.AddDocument(doc);
                }

                obj_signRequest.SetProfileId(model.ProfileId);
                obj_signRequest.SetCertificateAlias(model.Alias);
                if (!string.IsNullOrEmpty(model.Password)) obj_signRequest.SetCertificatePassword(model.Password);
                // optional - override the hand signature, company logo, signing reason, location
                // contact info which was set as default in the created signing profile            
                if (!string.IsNullOrEmpty(model.HandSignaturePath)) obj_signRequest.SetHandSignature(Util.ReadFile(model.HandSignaturePath));
                if (!string.IsNullOrEmpty(model.CompanyPath)) obj_signRequest.SetCompanyLogo(Util.ReadFile(model.CompanyPath));
                if (!string.IsNullOrEmpty(model.SigningReason)) obj_signRequest.SetSigningReason(model.SigningReason);
                if (!string.IsNullOrEmpty(model.SigningLocation)) obj_signRequest.SetSigningLocation(model.SigningLocation);
                if (!string.IsNullOrEmpty(model.ContactInfo)) obj_signRequest.SetContactInfo(model.ContactInfo);
                if (model.SigningPage.HasValue) obj_signRequest.SetSigningPage(model.SigningPage.Value);
                if (model.SigningArea.HasValue) obj_signRequest.SetSigningArea(model.SigningArea.Value);
                obj_signRequest.SetRequestMode(PdfSigningRequest.DSS);

                Log.Information($"{systemLog.TraceId} - Signing request - {JsonSerializer.Serialize(obj_signRequest)}");
                PdfSigningResponse obj_signingResponse = (PdfSigningResponse) obj_signRequest.Send(model.ADSSUrl);

                Log.Information($"{systemLog.TraceId} - Signing response - {JsonSerializer.Serialize(obj_signingResponse)}");
                if (obj_signingResponse.IsSuccessful())
                    return new ResponseObject<bool>(true, $"{systemLog.TraceId} - Ký thành công.", Code.Success);

                return new ResponseObject<bool>(false, $"{systemLog.TraceId} - Có lỗi xảy ra trong quá trình ký", Code.BadRequest);
            }
            catch(Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - {ex.Message}", ex);
                return new ResponseError(Code.BadRequest, ex.Message);
            }                      
        }

        public NetCore.Shared.Response SignADSSWithExistingSignatureField(ADSSCoreModelRequest model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Start SignADSSWithExistingSignatureField");
                Log.Information($"{systemLog.TraceId} - Model - {JsonSerializer.Serialize(model)}");
                string documentPath = Path.Combine(_hostEnvironment.WebRootPath + "/files", "test_file.pdf");

                PdfSigningRequest obj_signRequest = new PdfSigningRequest(model.OriginatorId, documentPath);

                if (model.Documents != null && model.Documents.Count > 0)
                {
                    foreach (var doc in model.Documents)
                        obj_signRequest.AddDocument(doc);
                }

                obj_signRequest.SetProfileId(model.ProfileId);
                obj_signRequest.SetCertificateAlias(model.Alias);
                if (!string.IsNullOrEmpty(model.Password)) obj_signRequest.SetCertificatePassword(model.Password);
                // optional - override the hand signature, company logo, signing reason, location                
                // contact info which was set as default in the created signing profile
                if (!string.IsNullOrEmpty(model.HandSignaturePath)) obj_signRequest.SetHandSignature(Util.ReadFile(model.HandSignaturePath));
                if (!string.IsNullOrEmpty(model.CompanyPath)) obj_signRequest.SetCompanyLogo(Util.ReadFile(model.CompanyPath));                
                if (!string.IsNullOrEmpty(model.SignBy)) obj_signRequest.SetSignedBy(model.SignBy);
                if (!string.IsNullOrEmpty(model.SigningReason)) obj_signRequest.SetSigningReason(model.SigningReason);
                if (!string.IsNullOrEmpty(model.SigningLocation)) obj_signRequest.SetSigningLocation(model.SigningLocation);
                if (!string.IsNullOrEmpty(model.ContactInfo)) obj_signRequest.SetContactInfo(model.ContactInfo);
                if (model.SigningPage.HasValue) obj_signRequest.SetSigningPage(model.SigningPage.Value);
                if (model.SigningArea.HasValue) obj_signRequest.SetSigningArea(model.SigningArea.Value);
                obj_signRequest.AddSignaturePosition(model.OriginatorId, 3, 231, 117, 190, 508, model.AppearanceId);
                obj_signRequest.SetRequestMode(PdfSigningRequest.DSS);

                Log.Information($"{systemLog.TraceId} - Signing request - {JsonSerializer.Serialize(obj_signRequest)}");
                PdfSigningResponse obj_signingResponse = (PdfSigningResponse) obj_signRequest.Send(model.ADSSUrl);

                Log.Information($"{systemLog.TraceId} - Signing response - {JsonSerializer.Serialize(obj_signingResponse)}");
                if (obj_signingResponse.IsSuccessful())
                    return new ResponseObject<bool>(true, $"{systemLog.TraceId} - Ký thành công.", Code.Success);

                return new ResponseObject<bool>(false, $"{systemLog.TraceId} - Có lỗi xảy ra trong quá trình ký", Code.BadRequest);
            }
            catch(Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - {ex.Message}", ex);
                return new ResponseError(Code.BadRequest, ex.Message);
            }
        }

        public NetCore.Shared.Response SignADSSExistingBlankSignatureField(ADSSCoreModelRequest model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Start SignADSSWithExistingSignatureField");
                Log.Information($"{systemLog.TraceId} - Model - {JsonSerializer.Serialize(model)}");
                string documentPath = Path.Combine(_hostEnvironment.WebRootPath + "/files", "test_file.pdf");

                PdfSigningRequest obj_signRequest = new PdfSigningRequest(model.OriginatorId, documentPath);

                if (model.Documents != null && model.Documents.Count > 0)
                {
                    foreach (var doc in model.Documents)
                        obj_signRequest.AddDocument(doc);
                }

                obj_signRequest.SetProfileId(model.ProfileId);
                obj_signRequest.SetCertificateAlias(model.Alias);
                obj_signRequest.SetSigningField(model.SignatureFieldName);
                if (!string.IsNullOrEmpty(model.Password)) obj_signRequest.SetCertificatePassword(model.Password);

                // optional - override the hand signature, company logo, signing reason, location
                // contact info which was set as default in the created signing profile
                if (!string.IsNullOrEmpty(model.HandSignaturePath)) obj_signRequest.SetHandSignature(Util.ReadFile(model.HandSignaturePath));
                if (!string.IsNullOrEmpty(model.CompanyPath)) obj_signRequest.SetCompanyLogo(Util.ReadFile(model.CompanyPath));        
                if (!string.IsNullOrEmpty(model.SigningReason)) obj_signRequest.SetSigningReason(model.SigningReason);
                if (!string.IsNullOrEmpty(model.SigningLocation)) obj_signRequest.SetSigningLocation(model.SigningLocation);
                if (!string.IsNullOrEmpty(model.ContactInfo)) obj_signRequest.SetContactInfo(model.ContactInfo);
                obj_signRequest.SetRequestMode(PdfSigningRequest.DSS);

                Log.Information($"{systemLog.TraceId} - Signing request - {JsonSerializer.Serialize(obj_signRequest)}");
                PdfSigningResponse obj_signingResponse = (PdfSigningResponse) obj_signRequest.Send(model.ADSSUrl);

                Log.Information($"{systemLog.TraceId} - Signing response - {JsonSerializer.Serialize(obj_signingResponse)}");
                if (obj_signingResponse.IsSuccessful())
                    return new ResponseObject<bool>(true, $"{systemLog.TraceId} - Ký thành công.", Code.Success);

                return new ResponseObject<bool>(false, $"{systemLog.TraceId} - Có lỗi xảy ra trong quá trình ký", Code.BadRequest);
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - {ex.Message}", ex);
                return new ResponseError(Code.BadRequest, ex.Message);
            }
        }
    }
}
