using Marvin.StreamExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RestClientLibrary.Implementations
{
    public class RestClient
    {
        private HttpClient _httpClient;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Creates a new instance of RestClient
        /// </summary>
        /// <param name="baseAddress">scheme://DNS:portNumber</param>
        public RestClient(string baseAddress)
        {
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Clear();

            _cancellationTokenSource = new CancellationTokenSource();

            SetProperties(baseAddress);

            UseStreams = true;
        }

        /// <summary>
        /// True means the User will supply Timeout in seconds
        /// False means the internal timeout will be used
        /// </summary>
        public bool CanSetTimeout { get; set; }

        /// <summary>
        /// Timeout in seconds. Default is 30 seconds
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// For better performance in terms of speed, This true by default
        /// </summary>
        public bool UseStreams { get; set; }

        private void SetProperties(string BaseAddress)
        {
            if (CanSetTimeout)
            {
                _httpClient.Timeout = new TimeSpan(0, 0, Timeout);
            }
            else
            {
                _httpClient.Timeout = new TimeSpan(0, 1, 30);
            }

            _httpClient.BaseAddress = new Uri(BaseAddress);
        }

        /// <summary>
        /// Use to GET resources from the API
        /// </summary>
        /// <typeparam name="T">Collection of the created resources at the API</typeparam>
        /// <param name="apiResourceAddress">API Address endpoint for getting resources</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAsync<T>(string apiResourceAddress)
        {
            var result = new List<T>();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiResourceAddress);

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (UseStreams)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync()) // reading response as stream
                    {
                        response.EnsureSuccessStatusCode();
                        result = stream.ReadAndDeserializeFromJson<List<T>>();
                    }
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync(); // reading response as string
                    result = JsonConvert.DeserializeObject<List<T>>(content);
                }
            }
            catch (OperationCanceledException ocException)
            {
                Console.WriteLine($"An operation was cancelled with message {ocException.Message}."); // Add Logging
            }

            return result ?? new List<T>();
        }

        /// <summary>
        /// Use to POST a resource to the API
        /// </summary>
        /// <typeparam name="T"> A collection containing the created resource at the API</typeparam>
        /// <param name="apiResourceAddress">API endpoint address</param>
        /// <param name="objectToPost"> Dto object of the resource that will be created at the API</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> PostAsync<T>(string apiResourceAddress, object objectToPost)
        {
            var result = new List<T>();
            try
            {
                if (UseStreams)
                {
                    var memoryContentStream = new MemoryStream();
                    memoryContentStream.SerializeToJsonAndWrite(
                        objectToPost,
                        new UTF8Encoding(),
                        1024,
                        true);

                    memoryContentStream.Seek(0, SeekOrigin.Begin);

                    using (var request = new HttpRequestMessage(HttpMethod.Post, apiResourceAddress))
                    {
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        using (var streamContent = new StreamContent(memoryContentStream))
                        {
                            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                            request.Content = streamContent;

                            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                            response.EnsureSuccessStatusCode();

                            var createdContentstream = await response.Content.ReadAsStreamAsync(); // reading response as stream

                            var createdObject = createdContentstream.ReadAndDeserializeFromJson<T>();

                            if (createdObject != null)
                                result.Add(createdObject);

                            return result;

                        }
                    }
                }
                else
                {
                    // object is turned to JSON objects
                    var serializedObjectToCreate = JsonConvert.SerializeObject(objectToPost);

                    // API address and content negotiation setup
                    var request = new HttpRequestMessage(HttpMethod.Post, apiResourceAddress);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // JSON onject is added to http request content as string
                    request.Content = new StringContent(serializedObjectToCreate);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    // Request is posted here
                    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync(); // reading response as string

                    var createdObject = JsonConvert.DeserializeObject<T>(content);

                    if (createdObject != null)
                        result.Add(createdObject);

                    return result;
                }
            }
            catch (OperationCanceledException ocException)
            {
                Console.WriteLine(ocException.Message); //Add Logging
            }
            return result ?? new List<T>();
        }

        /// <summary>
        /// Use to Delete resource at the API
        /// </summary>
        /// <param name="apiResourceAddress">API endpoint address of the resource to be deleted</param>
        /// <returns></returns>
        public async Task DeleteAsync(string apiResourceAddress)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, apiResourceAddress);

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Will add logging later
            }
        }

        /// <summary>
        /// Use to cancel a request
        /// </summary>
        public void CancelRequest()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
