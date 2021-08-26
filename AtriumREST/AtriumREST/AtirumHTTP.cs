using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

using ThreeRiversTech.Zuleger.Atrium.REST.Security;
using ThreeRiversTech.Zuleger.Atrium.REST.Exceptions;

namespace ThreeRiversTech.Zuleger.Atrium.REST
{
    public partial class AtriumController
    {
        // Checks an element in an XML Response String that it has an "ok" answer.
        private bool CheckAnswer(
            XElement xml,
            XName elementName,
            String attr = "err",
            bool throwException = true)
        {
            var e = xml.Element(elementName);
            var res = e.Attribute(attr);
            if (res.Value != "ok")
            {
                if (throwException)
                {
                    throw new SdkRequestException(res.Value);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        // Checks a set of elements in an XML Response String that it has an "ok" answer.
        private bool CheckAllAnswers(
            IEnumerable<XElement> xmlElements,
            XName elementName,
            String attr = "res",
            bool throwException = true)
        {
            foreach (var el in xmlElements)
            {
                if (!CheckAnswer(el, elementName, attr, throwException))
                {
                    return false;
                }
            }
            return true;
        }

        // Performs a POST request to the specified Subdomain (under the Address provided from construction) with specific parameters to send.
        private async Task<XElement> DoPOSTAsync(
            String subdomain,
            Dictionary<String, String> parameters,
            bool setSessionCookie = false,
            bool encryptedExchange = false)
        {
            var encodedContent = new FormUrlEncodedContent(parameters);
            // Address is specifically: ".../sdk.xml?_=<UNIXTIMENOW>&sid=<SESSIONID>
            var addr = _address
                + subdomain
                + "?_=" + Convert.ToString(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds())
                + "&sid=" + _sessionId;
            if (setSessionCookie)
            {
                _cookies.Add(new Uri(addr), new Cookie("Session", $"{_sessionId}-{AtriumController.PadLeft(_userId, '0', 2)}"));
            }
            var response = await _client.PostAsync(addr, encodedContent);
            String responseString = null;
            if (response.IsSuccessStatusCode)
            {
                responseString = await response.Content.ReadAsStringAsync();
                if (encryptedExchange)
                {
                    this.EncryptedResponse = responseString;
                    var postEnc = responseString.Replace("post_enc=", "");
                    postEnc = postEnc.Substring(0, postEnc.IndexOf("&"));
                    var checkSum = responseString.Substring(responseString.IndexOf("&") + 1, responseString.Length);

                    responseString = RC4.Decrypt(_sessionKey, postEnc);
                    if (RC4.CheckSum(responseString) != checkSum)
                    {
                        throw new IntegrityException();
                    }
                }
            }
            else
            {
                this.ResponseText = await response.Content.ReadAsStringAsync();
                throw new ThreeRiversTech.Zuleger.Atrium.REST.Exceptions.HttpRequestException(responseString);
            }

            var xml = XElement.Parse(responseString);
            _transactionNum++;

            ResponseText = xml.ToString();
            return xml;
        }

        // Performs a GET request to the specified Subdomain (under the Address provided from construction)
        private async Task<XElement> DoGETAsync(String subdomain, bool encryptedExchange = true)
        {
            RequestText = "GET " + _address + subdomain;
            var response = await _client.GetAsync(_address + subdomain);
            var responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                responseString = await response.Content.ReadAsStringAsync();
                if (encryptedExchange)
                {
                    this.EncryptedResponse = responseString;
                    var postEnc = responseString.Replace("post_enc=", "");
                    postEnc = postEnc.Substring(0, postEnc.IndexOf("&"));
                    var checkSum = responseString.Substring(responseString.IndexOf("&") + 1, responseString.Length);

                    responseString = RC4.Decrypt(_sessionKey, postEnc);
                    if (RC4.CheckSum(responseString) != checkSum)
                    {
                        throw new IntegrityException();
                    }
                }
            }
            else
            {
                this.ResponseText = await response.Content.ReadAsStringAsync();
                throw new ThreeRiversTech.Zuleger.Atrium.REST.Exceptions.HttpRequestException(responseString);
            }
            var xml = XElement.Parse(responseString);
            _transactionNum++;

            ResponseText = xml.ToString();
            return xml;
        }

        // Fetches an XML template and substitutes provided arguments, encrypts it under the RC4 Encryption Algorithm, then creates an encrypted request to be sent.
        private Dictionary<String, String> FetchAndEncryptXML(String xml, params String[] args)
        {
            Dictionary<String, String> parameters = new Dictionary<String, String>();
            var fileContent = xml;
            for (int i = 0; i < args.Length; i += 2)
            {
                fileContent = fileContent.Replace(args[i], args[i + 1]);
            }

            RequestText = fileContent;

            var postEnc = RC4.Encrypt(_sessionKey, fileContent);
            var postChk = RC4.CheckSum(fileContent);
            parameters.Add("sid", _sessionId);
            parameters.Add("post_enc", postEnc);
            parameters.Add("post_chk", postChk);

            this.EncryptedRequest = $"sid={_sessionId}&post_enc={postEnc}&post_chk={postChk}";

            return parameters;
        }

    }
}
