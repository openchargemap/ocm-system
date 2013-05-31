/*
    OCM charging point submission widget 
    Christopher Cook 2011 
    http://openchargemap.org
	
    See http://www.openchargemap.org/ for more details.

    Minimal example (jquery 1.5+ required): 

    <div id="ocmsubmission"></div>

    <script type="text/javascript">
        $(document).ready(function () {
        ocm_populateSubmissionForm("ocmsubmission");
        });
    </script>
*/
var ocm_api_baseurl = "http://openchargemap.org/api/";
//var ocm_api_baseurl = "http://localhost:8080/";
var ocm_odata_baseurl = ocm_api_baseurl + "odata/odata.svc/";

function ocm_populateSubmissionForm(divID) {
    //populated provided div (with given ID) with new submission form
    var contentHTML = '<form id="ocm_submitform" action="'+ocm_api_baseurl+'service.ashx" method="post"> ' +
        '<input type="hidden" name="action" value="cp_submission"/>' +
        '<fieldset> ' +
        '<legend>Location Details</legend> ' +
            '<p>All information is optional (except basic location details) but please provide as much detail as possible.</p>' +
            '<div><label for="ocm_loc_title">Descriptive Location Name</label><input type="text" id="ocm_loc_title" name="ocm_loc_title" value=""></div>' +
            '<div><label for="ocm_loc_addressline1">Address Line 1</label><input type="text" id="ocm_loc_addressline1" name="ocm_loc_addressline1" Value=""></div>' +
            '<div><label for="ocm_loc_addressline2">Address Line 2</label><input type="text" id="ocm_loc_addressline2" name="ocm_loc_addressline2" value=""></div>' +
            '<div><label for="ocm_loc_town">Town/City</label><input type="text" id="ocm_loc_town" name="ocm_loc_town" value=""></div>' +
            '<div><label for="ocm_loc_stateorprovince">State/Province</label><input type="text" id="ocm_loc_stateorprovince" name="ocm_loc_stateorprovince" value=""></div>' +
            '<div><label for="ocm_loc_postcode">Zip/Postcode</label><input type="text" id="ocm_loc_postcode" name="ocm_loc_postcode" value=""></div>' +
            '<div><label for="ocm_loc_countryid">Country</label><select id="ocm_loc_countryid" name="ocm_loc_countryid"><option value="">(Not Listed)</option></select></div>' +
            //'<div>Lookup the map location (latitude/longitude) for the above location: <input type="button" onclick="ocm_FindLocation()" value="Find" /></div>' +
            '<div><label for="ocm_loc_latitude">Latitude</label><input type="number" id="ocm_loc_latitude" name="ocm_loc_latitude" value=""></div>' +
            '<div><label for="ocm_loc_longitude">Longitude</label><input type="number" id="ocm_loc_longitude" name="ocm_loc_longitude" value=""></div>' +
            '<div><label for="ocm_loc_accesscomments">Comment for Access/Directions</label><input type="text" id="ocm_loc_accesscomments" name="ocm_loc_accesscomments" value=""></div>' +
            '<div><label for="ocm_loc_contacttelephone1">Contact telephone for enquiries</label><input type="tel" id="ocm_loc_contacttelephone1" name="ocm_loc_contacttelephone1" value=""></div>' +
            '<div><label for="ocm_loc_contactemail">Contact email for enquiries</label><input type="email" id="ocm_loc_contactemail" name="ocm_loc_contactemail" value=""></div>' +
        '</fieldset>' +
        '<fieldset><legend>Equipment Details</legend>' +
            '<div><label for="ocm_cp_numberofpoints">Number of points available</label><input type="number" id="ocm_cp_numberofpoints" name="ocm_cp_numberofpoints" value="1"></div>' +
            '<div><label for="ocm_cp_usagetype">Usage Restrictions</label><select id="ocm_cp_usagetype" name="ocm_cp_usagetype"><option value="">Unknown</option></select></div>' +
            '<div><label for="ocm_cp_usagecost">Usage Cost</label><input type="text" id="ocm_cp_usagecost" name="ocm_cp_usagecost" value=""></div>' +
            '<div><label for="ocm_cp_statustype">Status (if known)</label><select id="ocm_cp_statustype" name="ocm_cp_statustype"><option value="0">Unknown</option><option value="50">Operational</option><option value="100">Not Operational</option></select></div>' +
            '<div><label for="ocm_cp_generalcomments">Comments or Additional Info</label><input type="text" id="ocm_cp_generalcomments" name="ocm_cp_generalcomments" value=""></div>' +
        '</fieldset>' +
        '<fieldset> <legend>Connection Details - Primary</legend>' +
            '<div><label for="ocm_cp_connection1_type">Connection Type</label><select id="ocm_cp_connection1_type" name="ocm_cp_connection1_type"><option value="">Unknown</option></select></div>' +
            '<div><label for="ocm_cp_connection1_level">Charger Type/Level</label><select id="ocm_cp_connection1_level" name="ocm_cp_connection1_level"><option value="">Unknown</option></select></div>' +
            '<div><label for="ocm_cp_connection1_amps">Amps (if known)</label><input type="number" id="ocm_cp_connection1_amps" name="ocm_cp_connection1_amps" value=""></div>' +
            '<div><label for="ocm_cp_connection1_volts">Voltage (if  known)</label><input type="number" id="ocm_cp_connection1_volts" name="ocm_cp_connection1_volts" value=""></div>' +
         '</fieldset>' +
         '<fieldset> <legend>Connection Details - Secondary</legend>' +
            '<div><label for="ocm_cp_connection2_type">Connection Type</label><select id="ocm_cp_connection2_type" name="ocm_cp_connection2_type"><option value="">Unknown</option></select></div>' +
            '<div><label for="ocm_cp_connection2_level">Charger Type/Level</label><select id="ocm_cp_connection2_level" name="ocm_cp_connection2_level"><option value="">Unknown</option></select></div>' +
            '<div><label for="ocm_cp_connection2_amps">Amps (if known)</label><input type="number" id="ocm_cp_connection2_amps" name="ocm_cp_connection2_amps" value=""></div>' +
            '<div><label for="ocm_cp_connection2_volts">Voltage (if  known)</label><input type="number" id="ocm_cp_connection2_volts" name="ocm_cp_connection2_volts" value=""></div>' +
         '</fieldset>' +
        '<input type="button" value="Submit" onclick="ocm_validateAndSubmit();"/>'+
        '<div style="text-align:right;">powered by the <a href=\"http://openchargemap.org\" target="_blank">open charge map</a> project.</div>'+
    '</form>';

    var contentDiv = document.getElementById(divID);
    if (contentDiv == null) {
        alert("No element id supplied for form location.");
    }
    else {
        contentDiv.innerHTML = contentHTML;

        //populate lookup lists
        ocm_populateConnectionTypeList();
        ocm_populateChargerTypeList();
        ocm_populateUsageTypeList();
        ocm_populateCountryList();
    }
}

function ocm_FindLocation() {
    //openchargemap.org api key
    var apiKey = "ABQIAAAA5NLCtVOwuzZ932YZIZ1IexSabwQokmVCUdtZU8GdOjk1XXjnkRT - qBfdvH_ - PqeAYmnUW9xERfBRkQ";

    //localhost:8080 api key
    //var apiKey = "ABQIAAAA5NLCtVOwuzZ932YZIZ1IexTwM0brOpm - All5BF6PoaKBxRWWERTPuorRy4IAznvLfBmCbnwJ4T18FA";
    var addr_line1 = document.getElementById("ocm_loc_addressline1").value;
    if (addr_line1 == null) addr_line1 = "";

    var addr_line2 = document.getElementById("ocm_loc_addressline2").value;
    if (addr_line2 == null) addr_line2 = "";

    var addr_postcode = document.getElementById("ocm_loc_postcode").value;
    if (addr_postcode == null) addr_postcode = "";

    var address = addr_line1 + "," + addr_line2 + "," + addr_postcode;

    alert("The location lookup feature is not currently available.");
    /*
    $.get("http://maps.google.com/maps/geo?q=" + address + "&key=" + apiKey + "&sensor=false&output=xml",
    function (data) {
    alert(data);
    }
    );*/
}

function ocm_validateAndSubmit() {
    var valid = true;
    var editform = document.getElementById("ocm_submitform");

    //validate form
    var loc_title = document.getElementById("ocm_loc_title");
    if (loc_title.value.length < 5) {
        alert("Please provide a useful title for this location.");
        valid = false;
    }

    var loc_addressline1 = document.getElementById("ocm_loc_addressline1");
    var loc_postcode = document.getElementById("ocm_loc_postcode");

    if (loc_addressline1.value.length < 1 && loc_postcode.value.length < 1) {
        valid = false;
        alert("Please provide a useful address for this location");
    }

    var latitude = document.getElementById("ocm_loc_latitude");
    var longitude = document.getElementById("ocm_loc_longitude");

    if (latitude.value.length > 0 || longitude.value.length > 0) {
        //check lat/long are valid numbers
        if (isNaN(latitude.value) || isNaN(longitude.value)) {
            valid = false;
            alert("Please provide a valid latitude & longitude or leave them blank.");
        }
    }

    //submit if valid
    if (valid) editform.submit();
}

function ocm_populateListWithTitle(results, list) {
    jQuery(list).length = 0;
    for (var i = 0; i <= results.length - 1; i++) {
        if (results[i] != null) jQuery(list).append($("<option></option>").attr("value", results[i]["ID"]).text(results[i]["Title"]));
    }
}

function ocm_populateList_ocm_cp_connectiontype(results) {
    ocm_populateListWithTitle(results.d, "#ocm_cp_connection1_type");
    ocm_populateListWithTitle(results.d, "#ocm_cp_connection2_type");
}

function ocm_populateList_ocm_cp_chargertype(results) {
    ocm_populateListWithTitle(results.d, "#ocm_cp_connection1_level");
    ocm_populateListWithTitle(results.d, "#ocm_cp_connection2_level");
}

function ocm_populateList_ocm_cp_usagetype(results) {
    ocm_populateListWithTitle(results.d, "#ocm_cp_usagetype");
}

function ocm_populateList_ocm_loc_countryid(results) {
    ocm_populateListWithTitle(results.d, "#ocm_loc_countryid");
}


function ocm_populateList(url, list, fallbacklist) {

    //if using Data.js (for IE compat) use OData object instead of jquery
    var callbackname="ocm_populateList_" + list.replace("#", "");

    $.ajax({
        type: "GET",
        url: url + "?$format=json&$callback="+callbackname,
        jsonp: false,
        jsonpCallback: callbackname,
        contentType: "application/json",
        dataType: "jsonp",

        success: function (msg) {
            //invoke json callback
            eval(msg.d); 
        },
        error: function (xhr,  textStatus, errorThrown) {
            //error loading list content, fallback to presets if possible
            if (fallbacklist != null) {
               ocm_populateListWithTitle(fallbacklist, list);
            }
        }
    });

}

function ocm_populateConnectionTypeList() {
    var fallbacklist = [
        { "ID": "1", "Title": "J1772" },
        { "ID": "2", "Title": "CHAdeMO" },
        { "ID": "3", "Title": "UK 13 Amp Domestic Socket" },
        { "ID": "4", "Title": "Blue Commando (2P+E)" },
        { "ID": "5", "Title": "LP Inductive" },
        { "ID": "6", "Title": "SP Inductive" },
        { "ID": "7", "Title": "Avcon Connector" }
    ];

    ocm_populateList(ocm_odata_baseurl + "ConnectionTypes", "#ocm_cp_connectiontype", fallbacklist);
}

function ocm_populateChargerTypeList() {
    var fallbacklist = [
        { "ID": "1", "Title": "Level 1" },
        { "ID": "2", "Title": "Level 2" },
        { "ID": "3", "Title": "Level 3" },
        { "ID": "4", "Title": "Level 1 & 2" },
        { "ID": "5", "Title": "Level 2 & 3" }
    ];
    ocm_populateList(ocm_odata_baseurl + "ChargerTypes", "#ocm_cp_chargertype", fallbacklist);
}

function ocm_populateUsageTypeList() {
    var fallbacklist = [
        { "ID": "1", "Title": "Public" },
        { "ID": "2", "Title": "Private - Restricted Access" },
        { "ID": "3", "Title": "Privately Owned - Notice Required" }
    ];
    ocm_populateList(ocm_odata_baseurl + "UsageTypes", "#ocm_cp_usagetype", fallbacklist);
}

function ocm_populateCountryList() {
    var fallbacklist = [
        { "ID": "1", "Title": "United Kingdom" },
        { "ID": "2", "Title": "United States" },
        { "ID": "3", "Title": "Ireland" }
    ];
    ocm_populateList(ocm_odata_baseurl + "Countries", "#ocm_loc_countryid", fallbacklist);
}
