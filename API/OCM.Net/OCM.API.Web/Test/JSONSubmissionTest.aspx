<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JSONSubmissionTest.aspx.cs" Inherits="OCM.API.Test.JSONSubmissionTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.6.2/jquery.min.js"></script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
    <script type="text/javascript">
        
        $(document).ready(function () {
            $.ajax({
                type: "POST",
                url: "../service.ashx?action=cp_submission&format=json",
                data: "{\"AddressInfo\":{\"Title\":\"Test\"}}",
                success: function (msg) {
                    alert("Data Saved: " + msg);
                }
            });
        });
        </script>
    </div>
    </form>
</body>
</html>
