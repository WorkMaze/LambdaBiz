using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
	public interface IService
	{
		Task<T> Get<T>(string url, string queryString, Dictionary<string, string> headers);
		Task<object> Get(string url, string queryString, Dictionary<string, string> headers);
		Task<T> Delete<T>(string url, string queryString, Dictionary<string, string> headers);
		Task<object> Delete(string url, string queryString, Dictionary<string, string> headers);
		Task<T> Post<T>(string url, string queryString, object body, Dictionary<string, string> headers);
		Task<object> Post(string url, string queryString, object body, Dictionary<string, string> headers);
		Task<T> Put<T>(string url, string queryString, object body, Dictionary<string, string> headers);
		Task<object> Put(string url, string queryString, object body, Dictionary<string, string> headers);
	}
}
