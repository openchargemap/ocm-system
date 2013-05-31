/*
OCM charging point data browser
Christopher Cook 2011 
http://openchargemap.org
 	
See http://www.openchargemap.org/ for more details.
*/

// load dojo modules, only used for fetching data
dojo.require("dojo.fx");
dojo.ready(init);

var dataCache = null;
var isEditMode = false;
var fetchOptions = null;

function init() {
    initDefaultFetchOptions();
    fetchData(true);
}

function initDefaultFetchOptions() {
    fetchOptions = {
        "maxresults": 100,
        "chargepointid": null,
        "countrycode": null,
        "locationtitle": null,
        "submissionstatustypeid": null,
        "dataprovidername": null,
        "includecomments": false
    };
}

function fetchData(refreshGrid) {
    var requestUrl = "http://openchargemap.org/api/service.ashx?output=json&enablecaching=false";

    if (fetchOptions.maxresults != null) requestUrl += "&maxresults=" + fetchOptions.maxresults;
    if (fetchOptions.chargepointid != null && fetchOptions.chargepointid != "") requestUrl += "&chargepointid=" + fetchOptions.chargepointid;
    if (fetchOptions.countrycode != null && fetchOptions.countrycode != "") requestUrl += "&countrycode=" + fetchOptions.countrycode;
    if (fetchOptions.locationtitle != null && fetchOptions.locationtitle != "") requestUrl += "&locationtitle=" + fetchOptions.locationtitle;
    if (fetchOptions.submissionstatustypeid != null && fetchOptions.submissionstatustypeid != "") requestUrl += "&submissionstatustypeid=" + fetchOptions.submissionstatustypeid;
    if (fetchOptions.dataprovidername != null && fetchOptions.dataprovidername != "") requestUrl += "&dataprovidername=" + fetchOptions.dataprovidername;
    if (fetchOptions.includecomments) requestUrl += "&includecomments=true";

    // The "xhrGet" method executing an HTTP GET
    dojo.xhrGet({
        // The URL to request
        url: requestUrl,
        // The method that handles the request's successful result
        // Handle the response any way you'd like!
        load: function (result) {
            dojo.byId("debug").value = result;
            dataCache = eval(result);
            if (refreshGrid) refreshDataGrid(dataCache);
        }
    });
}

function refreshDataGrid(dataList) {
    var html = "<table class='datagrid'><tr><th>ID</th><th>Title</th></tr>";
    var alternate = false;

    if (dataList.length == 0) alert("There are no items matching your search criteria.");

    for (var i = 0; i < dataList.length; i++) {
        var item = dataList[i];
        var rowclass = "";
        if (alternate) rowclass = " class='alternate' ";
        html += "<tr " + rowclass + "><td><a href='#' onclick='editItem(" + item.ID + ");'>" + item.ID + "</a></td><td>" + item.AddressInfo.Title + "</td></tr>";
        alternate = !alternate;
    }

    html += "</table>";

    document.getElementById("gridContainer").innerHTML = html;
}

function getItem(id) {
    for (var i = 0; i < dataCache.length; i++) {
        if (dataCache[i].ID == id.toString()) return dataCache[i];
    }
    return null;
}

function setTextValue(itemID, itemValue) {
    if (itemValue != null) document.getElementById(itemID).value = itemValue;
}

function cancelEdit() {
    document.getElementById("editContainer").style.display = 'none';
}

function saveEdit() {
    if (isEditMode) {
        alert("save");
    }
}

function searchWeb() {
    //search web for details related to the location we're viewing
    var searchTerm = document.getElementById("ocm_loc_title").value;
    if (searchTerm != null && searchTerm != "") {
        window.open('http://www.google.com/search?q=' + searchTerm, '_blank');
    }
}

function editItem(id) {
    var item = getItem(id);

    var html = '<fieldset><legend>Edit Item: ' + id + '</legend><p>All information is optional (except basic location details) but please provide as much detail as possible.</p>' +
            '<div><label for="ocm_loc_title">Descriptive Location Name</label><input type="text" id="ocm_loc_title" name="ocm_loc_title" value=""> <a href="javascript:searchWeb();">Web Search</a></div>' +
            '<div id="mapCanvas"></div>' +
            '<div><label for="ocm_loc_addressline1">Address Line 1</label><input type="text" id="ocm_loc_addressline1" name="ocm_loc_addressline1" Value=""></div>' +
            '<div><label for="ocm_loc_addressline2">Address Line 2</label><input type="text" id="ocm_loc_addressline2" name="ocm_loc_addressline2" value=""></div>' +
            '<div><label for="ocm_loc_town">Town/City</label><input type="text" id="ocm_loc_town" name="ocm_loc_town" value=""></div>' +
            '<div><label for="ocm_loc_stateorprovince">State/Province</label><input type="text" id="ocm_loc_stateorprovince" name="ocm_loc_stateorprovince" value=""></div>' +
            '<div><label for="ocm_loc_postcode">Zip/Postcode</label><input type="text" id="ocm_loc_postcode" name="ocm_loc_postcode" value=""></div>' +
            '<div><label for="ocm_loc_countryid">Country</label><select id="ocm_loc_countryid" name="ocm_loc_countryid"><option value="">(Not Listed)</option></select></div>' +
    //'<div>Lookup the map location (latitude/longitude) for the above location: <input type="button" onclick="ocm_FindLocation()" value="Find" /></div>' +
            '<div><label for="ocm_loc_latitude">Latitude</label><input type="text" id="ocm_loc_latitude" name="ocm_loc_latitude" value=""></div>' +
            '<div><label for="ocm_loc_longitude">Longitude</label><input type="text" id="ocm_loc_longitude" name="ocm_loc_longitude" value=""></div>' +
            '<div><label for="ocm_loc_accesscomments">Comment for Access/Directions</label><input type="text" id="ocm_loc_accesscomments" name="ocm_loc_accesscomments" value=""></div>' +
            '<div><label for="ocm_loc_contacttelephone1">Contact telephone for enquiries</label><input type="text" id="ocm_loc_contacttelephone1" name="ocm_loc_contacttelephone1" value=""></div>' +
            '<div><label for="ocm_loc_contacttelephone2">Additional Contact telephone</label><input type="text" id="ocm_loc_contacttelephone2" name="ocm_loc_contacttelephone2" value=""></div>' +
            '<div><label for="ocm_loc_contactemail">Contact email for enquiries</label><input type="text" id="ocm_loc_contactemail" name="ocm_loc_contactemail" value=""></div>' +
            '</fieldset>' +
            '<fieldset><legend>General Details</legend>' +
            '<div><label for="ocm_cp_numberofpoints">Number of points available</label><input type="text" id="ocm_cp_numberofpoints" name="ocm_cp_numberofpoints" value="1"></div>' +
            '<div><label for="ocm_cp_usagetype">Usage Restrictions</label><select id="ocm_cp_usagetype" name="ocm_cp_usagetype"><option value="">Unknown</option></select></div>' +
            '<div><label for="ocm_cp_statustype">Status (if known)</label><select id="ocm_cp_statustype" name="ocm_cp_statustype"><option value="0">Unknown</option><option value="50">Operational</option><option value="100">Not Operational</option></select></div>' +
            '<div><label for="ocm_cp_generalcomments">Comments or Additional Info</label><input type="text" id="ocm_cp_generalcomments" name="ocm_cp_generalcomments" value=""></div>' +
            '<div><label for="ocm_cp_connectiontype">Connection Type</label><select id="ocm_cp_connectiontype" name="ocm_cp_connectiontype"><option value="">Unknown</option></select></div>' +
            '<div><label for="ocm_cp_chargertype">Charger Type/Level</label><select id="ocm_cp_chargertype" name="ocm_cp_chargertype"><option value="">Unknown</option></select></div>' +
            '</fieldset>' +
            '<fieldset><legend>General Equipment Details</legend>' +
            '</fieldset>' +
            '<input type="button" value="Cancel" onclick="cancelEdit();"/> ';

    if (isEditMode) html += '<input type="button" value="Save" onclick="saveEdit();"/>';

    document.getElementById("editContainer").innerHTML = html;

    setTextValue("ocm_loc_title", item.AddressInfo.Title);
    setTextValue("ocm_loc_addressline1", item.AddressInfo.AddressLine1);
    setTextValue("ocm_loc_addressline2", item.AddressInfo.AddressLine2);
    setTextValue("ocm_loc_town", item.AddressInfo.Town);
    setTextValue("ocm_loc_stateorprovince", item.AddressInfo.StateOrProvince);
    setTextValue("ocm_loc_postcode", item.AddressInfo.Postcode);
    // setTextValue("ocm_loc_countryid", item.AddressInfo.Postcode);

    setTextValue("ocm_loc_latitude", item.AddressInfo.Latitude);
    setTextValue("ocm_loc_longitude", item.AddressInfo.Longitude);
    setTextValue("ocm_loc_accesscomments", item.AddressInfo.AccessComments);
    setTextValue("ocm_loc_contacttelephone1", item.AddressInfo.ContactTelephone1);
    setTextValue("ocm_loc_contacttelephone2", item.AddressInfo.ContactTelephone2);
    setTextValue("ocm_loc_contactemail", item.AddressInfo.ContactEmail);

    setTextValue("ocm_cp_numberofpoints", item.NumberOfPoints);
    setTextValue("ocm_cp_generalcomments", item.GeneralComments);

    document.getElementById("editContainer").style.display = 'block';

    if (item.AddressInfo.Latitude != null && item.AddressInfo.Longitude != null) {
        showItemOnMap(item.AddressInfo.Latitude, item.AddressInfo.Longitude, item.AddressInfo.Title);
    }
}

function showItemOnMap(lat, lon, title) {
    //map item if possible:
    var markerLatlng = new google.maps.LatLng(lat, lon);
    var myOptions = {
        zoom: 14,
        center: markerLatlng,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };
    var map = new google.maps.Map(document.getElementById("mapCanvas"), myOptions);

    var marker = new google.maps.Marker({
        position: markerLatlng,
        map: map,
        title: title
    });
    /*var bounds = new google.maps.LatLngBounds();
    bounds.extend(markerLatlng);
    map.fitBounds(bounds);*/
    map.setCenter(markerLatlng);
}

function searchItems() {
    //init fetch options
    cancelEdit();
    initDefaultFetchOptions();

    //determine search filters by form values

    var findby_id = document.getElementById("ocm_findby_id").value;
    if (findby_id != "") {
        //find by id
        var i = getItem(findby_id);
        if (i != null) {
            editItem(i.ID);
            return;
        } else {
            //item not found in current list, will need to fetch from service
        }
        fetchOptions.chargepointid = findby_id;
    }

    fetchOptions.countrycode = document.getElementById("ocm_findby_countrycode").value;
    fetchOptions.locationtitle = document.getElementById("ocm_findby_locationtitle").value;
    fetchOptions.submissionstatustypeid = document.getElementById("ocm_findby_submissionstatustypeid").value;
    fetchOptions.includecomments = true;

    //perform search for data
    fetchData(true);
}
        