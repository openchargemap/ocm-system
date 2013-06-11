using System.IO;
using Newtonsoft.Json;

namespace OCM.API.Common.Model
{
    public static class ObjectExtension
    {
        //https://gist.github.com/erichexter/4166568#file-tojsonextension-cs

        public static string ToJson(this object obj)
        {
            JsonSerializer js = JsonSerializer.Create(new JsonSerializerSettings());
            var jw = new StringWriter();
            js.Serialize(jw, obj);
            return jw.ToString();
        }

    }
}