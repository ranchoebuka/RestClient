using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestClientLibrary.Interface
{
    public interface IRestClient
    {
        int Timeout { get; set; }

        bool CanSetTimeout { get; set; }

        bool UseStreams { get; set; }

        void CancelRequest();

        Task<IEnumerable<T>> PostAsync<T>(string apiResourceAddress, object objectToPost);

        Task<IEnumerable<T>> GetAsync<T>(string apiResourceAddress);
    }
}
