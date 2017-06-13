using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BeaconUWP
{
    public class Beacon
    {
        public Beacon()
        {
        }
        public Beacon(string address)
        {
            this.Device = address;
        }
        [JsonProperty(PropertyName = "device")]
        public string Device { get; set; }

        [JsonProperty(PropertyName = "distance")]
        public int Distance { get; set; } = 0;

        [JsonIgnore]
        public DateTime UpdatedAt { get; set; }
    }

    public class Message
    {
  
        [JsonProperty(PropertyName = "username")]
        public string UserName { get; set; } = "test1";

        [JsonProperty(PropertyName = "batteryLevel")]
        public double BatteryLevel { get; set; } = 0.7;

        [JsonProperty(PropertyName = "timestampmobile")]
        public int TimeStampMobile { get; set; }

        [JsonProperty(PropertyName = "mapForSend")]
        public List<Beacon> MapForSend { get; set; }
    }

    class CommunicationHelper
    {
        public static List<Beacon> Beacons = new List<Beacon> {
            new Beacon("FA:D0:E3:F1:70:6D"),
            new Beacon("EF:C4:C9:64:50:8A"),
            new Beacon("D1:CE:2B:04:A5:C0")
        };

        private static string sasToken = null;
        static string serviceNamespace = "sbNamespace";
        static string hubName = "hubname";
        static string keyName = "SendKey";
        static string hubKey = "key";
        static string publisherName = "device1";
        private static int tokenValidityDuration = 60 * 60; //hour
        private static int tokenLastGenerated;

        public static async Task CallEventHubHttpAsync(string payload)
        {
            var baseAddress = new Uri(string.Format("https://{0}.servicebus.windows.net/", serviceNamespace));
            var url = baseAddress + string.Format("{0}/publishers/{1}/messages", hubName, publisherName);

            // Create client
            var httpClient = new HttpClient();

            if (sasToken == null || !isTokenValid())
            {
                sasToken = createToken(url);
        
                TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
                tokenLastGenerated = (int)sinceEpoch.TotalSeconds;
            }


            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sasToken);

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            content.Headers.Add("ContentType", "application/json");

            var response = await httpClient.PostAsync(url, content);
        }

        private static bool isTokenValid()
        {
            var sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return ((tokenLastGenerated + tokenValidityDuration) < (int)sinceEpoch.TotalSeconds);
        }

        private static string createToken(string resourceUri)
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var week = 60 * 60 * 24 * 7;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
            string stringToSign = Uri.EscapeDataString(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hubKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", Uri.EscapeDataString(resourceUri), Uri.EscapeDataString(signature), expiry, keyName);
            return sasToken;
        }
    }


}
