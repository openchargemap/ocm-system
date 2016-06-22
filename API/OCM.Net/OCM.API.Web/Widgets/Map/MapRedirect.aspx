<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MapRedirect.aspx.cs" Inherits="OCM.API.Widgets.Map.MapRedirect" %>

<%

    Response.Redirect("https://api.openchargemap.io/map?" + Request.QueryString.ToString());
         %>