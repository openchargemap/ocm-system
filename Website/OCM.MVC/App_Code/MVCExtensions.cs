using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace OCM.MVC
{
    public static class MVCExtensions
    {
        public static MvcHtmlString ExtendedLabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression)
        {
            return ExtendedLabelFor<TModel, TValue>(html, expression, labelText: null);
        }

        public static MvcHtmlString ExtendedLabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText)
        {
            return ExtendedLabelFor(html, expression, labelText, htmlAttributes: null, metadataProvider: null);
        }

        internal static MvcHtmlString ExtendedLabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string labelText, IDictionary<string, object> htmlAttributes, ModelMetadataProvider metadataProvider)
        {
            return ExtendedLabelHelper(html,
                               ModelMetadata.FromLambdaExpression(expression, html.ViewData),
                               ExpressionHelper.GetExpressionText(expression),
                               labelText,
                               htmlAttributes);
        }
        internal static MvcHtmlString ExtendedLabelHelper(HtmlHelper html, ModelMetadata metadata, string htmlFieldName, string labelText = null, IDictionary<string, object> htmlAttributes = null)
        {
            string resolvedLabelText = labelText ?? metadata.DisplayName ?? metadata.PropertyName ?? htmlFieldName.Split('.').Last();
            if (String.IsNullOrEmpty(resolvedLabelText))
            {
                return MvcHtmlString.Empty;
            }

            var tag = new TagBuilder("label");
            tag.Attributes.Add("for", TagBuilder.CreateSanitizedId(html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName)));
            tag.SetInnerText(resolvedLabelText);
            tag.MergeAttributes(htmlAttributes, replaceExisting: true);
            return new MvcHtmlString(tag.ToString());
        }



    }
}
