using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace OCM.MVC
{
    public static class HttpRequestExtensions
    {
        public static Uri GetUri(this HttpRequest request)
        {

            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host,
                Path = request.Path.ToString(),
                Query = request.QueryString.ToString()
            };
            return uriBuilder.Uri;
        }

        public static string UserAgent(this HttpRequest request)
        {
            return request.Headers["User-Agent"].ToString();
        }
    }

    public static class HttpResponseExtensions
    {
        internal static string GetCookieValueFromResponse(this HttpResponse httpResponse, string cookieName)
        {
            // https://stackoverflow.com/questions/36899875/how-can-i-check-for-a-response-cookie-in-asp-net-core-mvc-aka-asp-net-5-rc1
            foreach (var cookieStr in httpResponse.Headers.GetCommaSeparatedValues("Set-Cookie"))
            {
                if (string.IsNullOrEmpty(cookieStr))
                    continue;

                var array = cookieStr.Split(';')
                    .Where(x => x.Contains('=')).Select(x => x.Trim());

                var dict = array.Select(item => item.Split(new[] { '=' }, 2)).ToDictionary(s => s[0], s => s[1]);


                if (dict.ContainsKey(cookieName))
                    return dict[cookieName];
            }

            return null;
        }
    }


    public static class MVCExtensions
    {
        public static HtmlString ExtendedLabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression)
        {
            return ExtendedLabelFor<TModel, TValue>(html, expression, labelText: null);
        }

        public static HtmlString ExtendedLabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText)
        {
            return ExtendedLabelFor(html, expression, labelText, htmlAttributes: null, metadataProvider: null);
        }

        internal static HtmlString ExtendedLabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText, IDictionary<string, object> htmlAttributes, ModelMetadataProvider metadataProvider)
        {
            //FIXME:
            return new HtmlString(labelText);
            /*
            return LabelTagHelper(html,
                               ModelMetadata.FromLambdaExpression(expression, html.ViewData),
                               ExpressionHelper.GetExpressionText(expression),
                               labelText,
                               htmlAttributes);
                               */
        }
        internal static HtmlString ExtendedLabelHelper(HtmlHelper html, ModelMetadata metadata, string htmlFieldName, string labelText = null, IDictionary<string, object> htmlAttributes = null)
        {
            string resolvedLabelText = labelText ?? metadata.DisplayName ?? metadata.PropertyName ?? htmlFieldName.Split('.').Last();
            if (String.IsNullOrEmpty(resolvedLabelText))
            {
                return HtmlString.Empty;
            }

            var tag = new TagBuilder("label");
            tag.Attributes.Add("for", TagBuilder.CreateSanitizedId(html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName), "_"));
            tag.InnerHtml.SetContent(resolvedLabelText);
            tag.MergeAttributes(htmlAttributes, replaceExisting: true);
            return new HtmlString(tag.ToString());
        }



    }
}
