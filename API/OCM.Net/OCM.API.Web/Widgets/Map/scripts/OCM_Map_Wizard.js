var ocm_data = new OCM_Data();
var ocm_app = new OCM_App();

var supported_languages = [
			{ "Title": "English", "LanguageCode": "en" },
			{ "Title": "French/Français", "LanguageCode": "fr" },
			{ "Title": "Dutch/Nederlands", "LanguageCode": "nl" },
            { "Title": "German/Deutsch", "LanguageCode": "de" },
			{ "Title": "Japanese", "LanguageCode": "ja" },
            { "Title": "Chinese", "LanguageCode": "zh" },
            { "Title": "Russian", "LanguageCode": "ru" },
			{ "Title": "Test Mode (for use by translators)", "LanguageCode": "test" },
		];

function checkboxVals(groupName) {
    var ids = '';
    $.each($("input[name='" + groupName + "[]']:checked"), function () {
        ids += (ids ? ',' : '') + $(this).attr('value');
    });
    return ids;
};

function refreshMap() {
    var map_style = "width:" + $("#map-width").val() + ";height:" + $("#map-height").val()+";";
    var map_title = $("#map-title").val();

    var url_params = "maptitle=" + escape(map_title);

    var languagecode = $("#languagecode").val();
    if (languagecode != "") {
        url_params += "&languagecode=" + languagecode;
    }

    //preset data filters
    if ($("#countryid").val() != "") {
        url_params += "&countryid=" + $("#countryid").val();
    }

    if ($("#operatorid").val() != "") {
        url_params += "&operatorid=" + $("#operatorid").val();
    }

    if ($("#connectiontypeid").val() != "") {
        url_params += "&connectiontypeid=" + $("#connectiontypeid").val();
    }

    if ($("#levelid").val() != "") {
        url_params += "&levelid=" + $("#levelid").val();
    }
    
    if ($("#usagetypeid").val() != "") {
		url_params += "&usagetypeid=" + $("#usagetypeid").val();
    }
    
    if ($("#statustypeid").val() != "") {
		url_params += "&statustypeid=" + $("#statustypeid").val();
    }

    url_params += "&maxresults=" + $("#max-results").val();

    //filter control toggles
    var controlFilterChecks = document.getElementById("map_options")["filtercontrol"];
    var controlFilterCheckValues = "";
    for (i = 0; i < controlFilterChecks.length; i++) {
        if (controlFilterChecks[i].checked) {
            controlFilterCheckValues += "," + controlFilterChecks[i].value;
        }
    }
    if (controlFilterCheckValues != "") {
        url_params += "&filtercontrols=" + controlFilterCheckValues;
    }

    if (document.getElementById("geolocate-onload").checked) {
        url_params += "&geolocation=auto";
    }

    if ($("#icon-set").val()!="") {
        url_params += "&iconset="+$("#icon-set").val();
    }

    var embedHTML = "<iframe style=\"" + map_style + "\" src=\"http://openchargemap.org/api/widgets/map/?" + url_params + "\"></iframe>";

    document.getElementById("ocm-map-embed-html").value = embedHTML;
    document.getElementById("ocm-map-embed").innerHTML = embedHTML;
};

$(document).ready(function () {

    //render options accordion
    $("#accordion").accordion();

    //populate dropdowns
    ocm_data.fetchCoreReferenceData("getReferenceData_Completed");

});

function getReferenceData_Completed(refData) {
    ocm_data.referenceData = refData;

    //setup dropdown options
    ocm_app.populateDropdown("countryid", refData.Countries, "1", true, false, "(All)");

    //set pre-filter dropdown options
    ocm_app.populateDropdown("connectiontypeid", refData.ConnectionTypes, null, true, false, "(All)");
    ocm_app.populateDropdown("operatorid", refData.Operators, null, true, false, "(All)");
    ocm_app.populateDropdown("submissionstatustypeid", refData.SubmissionStatusTypes, null, true, false, "(All Published)");
    ocm_app.populateDropdown("usagetypeid", refData.UsageTypes, null, true, false, "(All)");
    ocm_app.populateDropdown("statustypeid", refData.StatusTypes, null, true, false, "(All)");
    

    //populate language dropdown
    var $language_dropdown = $("#languagecode");
    $('option', $language_dropdown).remove();
    for (var i = 0; i < supported_languages.length; i++) {
        if (supported_languages[i] != null) {
            $language_dropdown.append($('<option > </option>').val(supported_languages[i].LanguageCode).html(supported_languages[i].Title));
        }
    }

    refreshMap();
};