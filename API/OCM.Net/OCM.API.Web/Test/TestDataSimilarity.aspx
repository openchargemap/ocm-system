<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestDataSimilarity.aspx.cs" Inherits="OCM.API.Test.TestDataSimilarity" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        body
        {
            font-family: Arial, Helvetica, sans-serif;
            font-size: 9pt;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:GridView ID="GridViewSource" runat="server">
            <Columns>
             <asp:TemplateField HeaderText="">
                    <ItemTemplate>
                      
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="DataProvider">
                    <ItemTemplate>
                        <%#Eval("DataProvider.Title") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Title">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.Title") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="AddressLine1">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.AddressLine1") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="AddressLine2">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.AddressLine2") %>
                    </ItemTemplate>
                </asp:TemplateField>
                 <asp:TemplateField HeaderText="Postcode">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.Postcode") %>
                    </ItemTemplate>
                </asp:TemplateField>
                  <asp:TemplateField HeaderText="Position">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.Latitude") %>,<%#Eval("AddressInfo.Longitude") %>,
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        
        </asp:GridView>
        <asp:GridView ID="GridViewSimilar" runat="server">
        <Columns>
        <asp:TemplateField HeaderText="% Similar">
                    <ItemTemplate>
                        <%#Eval("PercentageSimilarity") %>
                    </ItemTemplate>
                </asp:TemplateField>
                 <asp:TemplateField HeaderText="DataProvider">
                    <ItemTemplate>
                        <%#Eval("DataProvider.Title") %>
                    </ItemTemplate>
                </asp:TemplateField>
        <asp:TemplateField HeaderText="Title">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.Title") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="AddressLine1">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.AddressLine1") %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="AddressLine2">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.AddressLine2") %>
                    </ItemTemplate>
                </asp:TemplateField>
                 <asp:TemplateField HeaderText="Postcode">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.Postcode") %>
                    </ItemTemplate>
                </asp:TemplateField>
                 <asp:TemplateField HeaderText="Position">
                    <ItemTemplate>
                        <%#Eval("AddressInfo.Latitude") %>,<%#Eval("AddressInfo.Longitude") %>,
                    </ItemTemplate>
                </asp:TemplateField>
                </Columns>
        </asp:GridView>
    </div>
    </form>
</body>
</html>
