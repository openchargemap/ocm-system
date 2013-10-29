OCM_App.prototype.getCookie = function (c_name) {
    if (this.isRunningUnderCordova) {
        console.log("getting cookie:" + c_name + "::" + this.ocm_data.getCachedDataObject("_pref_" + c_name));
        return this.ocm_data.getCachedDataObject("_pref_" + c_name);
    } else {
        //http://www.w3schools.com/js/js_cookies.asp
        var i, x, y, ARRcookies = document.cookie.split(";");
        for (i = 0; i < ARRcookies.length; i++) {
            x = ARRcookies[i].substr(0, ARRcookies[i].indexOf("="));
            y = ARRcookies[i].substr(ARRcookies[i].indexOf("=") + 1);
            x = x.replace(/^\s+|\s+$/g, "");
            if (x == c_name) {
                var val = unescape(y);
                if (val == "null")
                    val = null;
                return val;
            }
        }
        return null;
    }
};

OCM_App.prototype.setCookie = function (c_name, value, exdays) {
    if (this.isRunningUnderCordova) {
        this.ocm_data.setCachedDataObject("_pref_" + c_name, value);
    } else {
        if (exdays == null)
            exdays = 1;

        //http://www.w3schools.com/js/js_cookies.asp
        var exdate = new Date();
        exdate.setDate(exdate.getDate() + exdays);
        var c_value = escape(value) + "; expires=" + exdate.toUTCString();
        document.cookie = c_name + "=" + c_value;
    }
};

OCM_App.prototype.clearCookie = function (c_name) {
    if (this.isRunningUnderCordova) {
        this.ocm_data.setCachedDataObject("_pref_" + c_name, null);
    } else {
        var expires = new Date();
        expires.setUTCFullYear(expires.getUTCFullYear() - 1);
        document.cookie = c_name + '=; expires=' + expires.toUTCString() + '; path=/';
    }
};

OCM_App.prototype.getParameter = function (name) {
    //Get a parameter value from the document url
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS);
    var results = regex.exec(window.location.href);
    if (results == null)
        return "";
else
        return results[1];
};

OCM_App.prototype.setDropdown = function (id, selectedValue) {
    if (selectedValue == null)
        selectedValue = "";
    var $dropdown = $("#" + id);
    $dropdown.val(selectedValue);
};

OCM_App.prototype.populateDropdown = function (id, refDataList, selectedValue, defaultToUnspecified, useTitleAsValue, unspecifiedText) {
    var $dropdown = $("#" + id);
    $('option', $dropdown).remove();

    if (defaultToUnspecified == true) {
        if (unspecifiedText == null)
            unspecifiedText = "Unknown";
        $dropdown.append($('<option > </option>').val("").html(unspecifiedText));
        $dropdown.val("");
    }

    for (var i = 0; i < refDataList.length; i++) {
        if (useTitleAsValue == true) {
            $dropdown.append($('<option > </option>').val(refDataList[i].Title).html(refDataList[i].Title));
        } else {
            $dropdown.append($('<option > </option>').val(refDataList[i].ID).html(refDataList[i].Title));
        }
    }

    if (selectedValue != null)
        $dropdown.val(selectedValue);
};

OCM_App.prototype.showProgressIndicator = function () {
    $("#progress-indicator").fadeIn('slow');
};

OCM_App.prototype.hideProgressIndicator = function () {
    $("#progress-indicator").fadeOut();
};
