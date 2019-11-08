/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/leaflet/leaflet.d.ts" />
/// <reference path="OCM_App.ts" />
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var OCM;
(function (OCM) {
    var LocationEditor = /** @class */ (function (_super) {
        __extends(LocationEditor, _super);
        function LocationEditor() {
            return _super !== null && _super.apply(this, arguments) || this;
        }
        LocationEditor.prototype.initEditors = function () {
            this.numConnectionEditors = 5;
            this.editorMapInitialised = false;
            this.editorMap = null;
            this.editMarker = null;
            this.positionAttribution = null;
            var editorSubmitMethod = $.proxy(this.performLocationSubmit, this);
            $("#editlocation-form").validate({
                rules: {
                    edit_addressinfo_title: {
                        required: true,
                        email: true
                    }
                },
                submitHandler: function (form) {
                    editorSubmitMethod();
                }
            });
            //fetch editor reference data
            this.apiClient.referenceData = this.apiClient.getCachedDataObject("CoreReferenceData");
            if (this.apiClient.referenceData == null) {
                //no cached reference data, fetch from service
                this.apiClient.fetchCoreReferenceData("ocm_app.populateEditor", this.getLoggedInUserInfo());
            }
            else {
                //cached ref data exists, use that
                this.log("Using cached reference data..");
                var _app = this;
                setTimeout(function () { _app.populateEditor(null); }, 50);
                //attempt to fetch fresh data later (wait 1 second)
                setTimeout(function () { _app.apiClient.fetchCoreReferenceData("ocm_app.populateEditor", _app.getLoggedInUserInfo()); }, 1000);
            }
        };
        LocationEditor.prototype.resetEditorForm = function () {
            //init editor to default settings
            document.getElementById("editlocation-form").reset();
            for (var n = 1; n <= this.numConnectionEditors; n++) {
                //reset editor dropdowns
                this.setDropdown("edit_connection" + n + "_connectiontype", "0");
                this.setDropdown("edit_connection" + n + "_level", "");
                this.setDropdown("edit_connection" + n + "_status", "0");
                this.setDropdown("edit_connection" + n + "_currenttype", "");
            }
            this.setDropdown("edit_addressinfo_countryid", 1);
            this.setDropdown("edit_operator", 1);
            this.setDropdown("edit_dataprovider", 1);
            this.setDropdown("edit_submissionstatus", 1);
            this.setDropdown("edit_statustype", 50); //operational
            this.positionAttribution = null;
            $("#editor-map").hide();
        };
        LocationEditor.prototype.populateEditor = function (refData) {
            this.hideProgressIndicator();
            //todo:move this into OCM_Data then pass to here from callback
            if (refData == null) {
                //may be loaded from cache
                refData = this.apiClient.referenceData;
            }
            else {
                //store cached ref data
                if (refData != null) {
                    this.apiClient.setCachedDataObject("CoreReferenceData", refData);
                    this.log("Updated cached CoreReferenceData.");
                }
            }
            this.apiClient.referenceData = refData;
            this.apiClient.sortCoreReferenceData();
            refData = this.apiClient.referenceData;
            //
            this.isLocationEditMode = false;
            //populate location editor dropdowns etc
            this.populateDropdown("edit_addressinfo_countryid", refData.Countries, null);
            this.populateDropdown("edit_usagetype", refData.UsageTypes, null);
            this.populateDropdown("edit_statustype", refData.StatusTypes, null);
            this.populateDropdown("edit_operator", refData.Operators, 1);
            //populate connection editor(s)
            for (var n = 1; n <= this.numConnectionEditors; n++) {
                //create editor section
                var $connection = ($("#edit_connection" + n));
                if (!($connection.length > 0)) {
                    //create new section using section 1 as template
                    var templateHTML = $("#edit_connection1").html();
                    if (templateHTML != null) {
                        templateHTML = templateHTML.replace("Equipment Details 1", "Equipment Details " + n);
                        templateHTML = templateHTML.replace(/connection1/gi, "connection" + n);
                        $connection = $("<div id=\"edit_connection" + n + "\" class='panel panel-default'>" + templateHTML + "</div>");
                        $("#edit-connectioneditors").append($connection);
                    }
                    $connection.collapse("show");
                }
                //populate dropdowns
                this.populateDropdown("edit_connection" + n + "_connectiontype", refData.ConnectionTypes, null);
                this.populateDropdown("edit_connection" + n + "_level", refData.ChargerTypes, null, true);
                this.populateDropdown("edit_connection" + n + "_status", refData.StatusTypes, null);
                this.populateDropdown("edit_connection" + n + "_currenttype", refData.CurrentTypes, null, true);
            }
            //setup geocoding lookup of address in editor
            var appContext = this;
            appContext.setElementAction("#edit-location-lookup", function (event, ui) {
                //format current address as string
                var lookupString = ($("#edit_addressinfo_addressline1").val().length > 0 ? $("#edit_addressinfo_addressline1").val() + "," : "") +
                    ($("#edit_addressinfo_addressline2").val().length > 0 ? $("#edit_addressinfo_addressline2").val() + "," : "") +
                    ($("#edit_addressinfo_town").val().length > 0 ? $("#edit_addressinfo_town").val() + "," : "") +
                    ($("#edit_addressinfo_stateorprovince").val().length > 0 ? $("#edit_addressinfo_stateorprovince").val() + "," : "") +
                    ($("#edit_addressinfo_postcode").val().length > 0 ? $("#edit_addressinfo_postcode").val() + "," : "") +
                    appContext.apiClient.getRefDataByID(refData.Countries, $("#edit_addressinfo_countryid").val()).Title;
                //attempt to geocode address
                appContext.geolocationManager.determineGeocodedLocation(lookupString, $.proxy(appContext.populateEditorLatLon, appContext), $.proxy(appContext.determineGeocodedLocationFailed, appContext));
            });
            appContext.setElementAction("#editlocation-submit", function () {
                if (appContext.validateLocationEditor() === true) {
                    appContext.performLocationSubmit();
                }
            });
            //populate user comment editor
            this.populateDropdown("comment-type", refData.UserCommentTypes, null);
            this.populateDropdown("checkin-type", refData.CheckinStatusTypes, null);
            //populate and hide non-edit mode items: submission status etc by default
            this.populateDropdown("edit_submissionstatus", refData.SubmissionStatusTypes, 1);
            this.populateDropdown("edit_dataprovider", refData.DataProviders, 1);
            $("#edit-submissionstatus-container").hide();
            $("#edit-dataprovider-container").hide();
            //avoid resetting pref selections on list change
            this.appState.suppressSettingsSave = true;
            //populate lists in filter/prefs/about page
            this.populateDropdown("filter-connectiontype", refData.ConnectionTypes, this.getMultiSelectionAsArray($("#filter-connectiontype"), ""), true, false, "(All)");
            this.populateDropdown("filter-operator", refData.Operators, this.getMultiSelectionAsArray($("#filter-operator"), ""), true, false, "(All)");
            this.populateDropdown("filter-usagetype", refData.UsageTypes, this.getMultiSelectionAsArray($("#filter-usagetype"), ""), true, false, "(All)");
            this.populateDropdown("filter-statustype", refData.StatusTypes, this.getMultiSelectionAsArray($("#filter-statustype"), ""), true, false, "(All)");
            this.appState.suppressSettingsSave = false;
            //refresh of core ref data wipes settings selections, load from prefs so set them again
            appContext.loadSettings();
            this.resetEditorForm();
            if (refData.UserProfile && refData.UserProfile != null && refData.UserProfile.IsCurrentSessionTokenValid == false) {
                //login info is stale, logout user
                if (this.isUserSignedIn()) {
                    this.log("Sign In token is stale, logging out user.");
                    appContext.logout(false);
                }
            }
        };
        LocationEditor.prototype.populateEditorLatLon = function (result) {
            var lat = result.coords.latitude;
            var lng = result.coords.longitude;
            $("#edit_addressinfo_latitude").val(lat);
            $("#edit_addressinfo_longitude").val(lng);
            //show data attribution for lookup
            $("#position-attribution").html(result.attribution);
            this.positionAttribution = result.attribution;
            //refresh map view
            this.refreshEditorMap();
        };
        LocationEditor.prototype.validateLocationEditor = function () {
            var isValid = true;
            if (isValid == true && $("#edit_addressinfo_title").val().length < 3) {
                this.showMessage("Please provide a descriptive/summary title for this location");
                isValid = false;
            }
            if (isValid == true && $("#edit_addressinfo_latitude").val() == "") {
                this.showMessage("Please provide a valid Latitude or use the lookup button.");
                isValid = false;
            }
            else if (isValid == true && $("#edit_addressinfo_longitude").val() == "") {
                this.showMessage("Please provide a valid Longitude or use the lookup button.");
                isValid = false;
            }
            return isValid;
        };
        LocationEditor.prototype.performLocationSubmit = function () {
            var app = this;
            if (!app.appState.isSubmissionInProgress) {
                var refData = this.apiClient.referenceData;
                var item = this.apiClient.referenceData.ChargePoint;
                if (this.isLocationEditMode == true)
                    item = this.viewModel.selectedPOI;
                //collect form values
                item.AddressInfo.Title = $("#edit_addressinfo_title").val();
                item.AddressInfo.AddressLine1 = $("#edit_addressinfo_addressline1").val();
                item.AddressInfo.AddressLine2 = $("#edit_addressinfo_addressline2").val();
                item.AddressInfo.Town = $("#edit_addressinfo_town").val();
                item.AddressInfo.StateOrProvince = $("#edit_addressinfo_stateorprovince").val();
                item.AddressInfo.Postcode = $("#edit_addressinfo_postcode").val();
                item.AddressInfo.Latitude = $("#edit_addressinfo_latitude").val();
                item.AddressInfo.Longitude = $("#edit_addressinfo_longitude").val();
                var country = this.apiClient.getRefDataByID(refData.Countries, $("#edit_addressinfo_countryid").val());
                item.AddressInfo.Country = country;
                item.AddressInfo.CountryID = null;
                item.AddressInfo.AccessComments = $("#edit_addressinfo_accesscomments").val();
                item.AddressInfo.ContactTelephone1 = $("#edit_addressinfo_contacttelephone1").val();
                item.AddressInfo.ContactTelephone2 = $("#edit_addressinfo_contacttelephone2").val();
                item.AddressInfo.ContactEmail = $("#edit_addressinfo_contactemail").val();
                item.AddressInfo.RelatedURL = $("#edit_addressinfo_relatedurl").val();
                item.NumberOfPoints = $("#edit_numberofpoints").val();
                item.UsageType = this.apiClient.getRefDataByID(refData.UsageTypes, $("#edit_usagetype").val());
                item.UsageTypeID = null;
                item.UsageCost = $("#edit_usagecost").val();
                item.StatusType = this.apiClient.getRefDataByID(refData.StatusTypes, $("#edit_statustype").val());
                item.StatusTypeID = null;
                item.GeneralComments = $("#edit_generalcomments").val();
                item.OperatorInfo = this.apiClient.getRefDataByID(refData.Operators, $("#edit_operator").val());
                item.OperatorID = null;
                if (this.isLocationEditMode != true) {
                    item.DataProvider = null;
                    item.DataProviderID = null;
                    //if user is editor for this new location, set to publish on submit
                    if (this.hasUserPermissionForPOI(item, "Edit")) {
                        item.SubmissionStatus = this.apiClient.getRefDataByID(refData.SubmissionStatusTypes, 200);
                        item.SubmissionStatusTypeID = null;
                    }
                }
                else {
                    //in edit mode use submission status from form
                    item.SubmissionStatus = this.apiClient.getRefDataByID(refData.SubmissionStatusTypes, $("#edit_submissionstatus").val());
                    item.SubmissionStatusTypeID = null;
                    /*item.DataProvider = this.ocm_data.getRefDataByID(refData.DataProviders, $("#edit_dataprovider").val());
                    item.DataProviderID = null;*/
                }
                if (item.Connections == null)
                    item.Connections = new Array();
                //read settings from connection editors
                var numConnections = 0;
                for (var n = 1; n <= this.numConnectionEditors; n++) {
                    var originalConnection = null;
                    if (item.Connections.length >= n) {
                        originalConnection = item.Connections[n - 1];
                    }
                    var connectionInfo = {
                        "ID": -1,
                        "Reference": null,
                        "ConnectionType": this.apiClient.getRefDataByID(refData.ConnectionTypes, $("#edit_connection" + n + "_connectiontype").val()),
                        "StatusType": this.apiClient.getRefDataByID(refData.StatusTypes, $("#edit_connection" + n + "_status").val()),
                        "Level": this.apiClient.getRefDataByID(refData.ChargerTypes, $("#edit_connection" + n + "_level").val()),
                        "CurrentType": this.apiClient.getRefDataByID(refData.CurrentTypes, $("#edit_connection" + n + "_currenttype").val()),
                        "Amps": $("#edit_connection" + n + "_amps").val(),
                        "Voltage": $("#edit_connection" + n + "_volts").val(),
                        "PowerKW": $("#edit_connection" + n + "_powerkw").val(),
                        "Quantity": $("#edit_connection" + n + "_quantity").val()
                    };
                    //preserve original connection info not editable in this editor
                    if (originalConnection != null) {
                        connectionInfo.ID = originalConnection.ID;
                        connectionInfo.Reference = originalConnection.Reference;
                        connectionInfo.Comments = originalConnection.Comments;
                    }
                    //add new connection or update existing
                    if (item.Connections.length >= n) {
                        item.Connections[n - 1] = connectionInfo;
                    }
                    else {
                        item.Connections.push(connectionInfo);
                    }
                }
                //stored attribution metadata if any
                if (this.positionAttribution != null) {
                    //add/update position attributiom
                    if (item.MetadataValues == null)
                        item.MetadataValues = new Array();
                    var attributionMetadata = this.apiClient.getMetadataValueByMetadataFieldID(item.MetadataValues, this.apiClient.ATTRIBUTION_METADATAFIELDID);
                    if (attributionMetadata != null) {
                        attributionMetadata.ItemValue = this.positionAttribution;
                    }
                    else {
                        attributionMetadata = {
                            MetadataFieldID: this.apiClient.ATTRIBUTION_METADATAFIELDID,
                            ItemValue: this.positionAttribution
                        };
                        item.MetadataValues.push(attributionMetadata);
                    }
                }
                else {
                    //clear position attribution if required
                    var attributionMetadata = this.apiClient.getMetadataValueByMetadataFieldID(item.MetadataValues, this.apiClient.ATTRIBUTION_METADATAFIELDID);
                    if (attributionMetadata != null) {
                        //remove existing item from array
                        item.MetadataValues = jQuery.grep(item.MetadataValues, function (a, i) {
                            return a.MetadataFieldID !== app.apiClient.ATTRIBUTION_METADATAFIELDID;
                        });
                    }
                }
                //show progress indicator
                this.showProgressIndicator();
                this.appState.isSubmissionInProgress = true;
                //submit
                this.apiClient.submitLocation(item, this.getLoggedInUserInfo(), function (jqXHR, textStatus) {
                    app.submissionCompleted(jqXHR, textStatus);
                    //refresh POI details via API
                    if (item.ID > 0) {
                        app.showDetailsViewById(item.ID, true);
                        app.navigateToLocationDetails();
                    }
                }, $.proxy(app.submissionFailed, app));
            }
        };
        LocationEditor.prototype.showLocationEditor = function () {
            this.resetEditorForm();
            //populate editor with currently selected poi
            if (this.viewModel.selectedPOI != null) {
                this.isLocationEditMode = true;
                var poi = this.viewModel.selectedPOI;
                this.positionAttribution = null;
                //load existing position attribution (if any)
                if (poi.MetadataValues != null) {
                    var attributionMetadata = this.apiClient.getMetadataValueByMetadataFieldID(poi.MetadataValues, this.apiClient.ATTRIBUTION_METADATAFIELDID);
                    if (attributionMetadata != null) {
                        this.positionAttribution = attributionMetadata.ItemValue;
                    }
                }
                $("#edit_addressinfo_title").val(poi.AddressInfo.Title);
                $("#edit_addressinfo_addressline1").val(poi.AddressInfo.AddressLine1);
                $("#edit_addressinfo_addressline2").val(poi.AddressInfo.AddressLine2);
                $("#edit_addressinfo_town").val(poi.AddressInfo.Town);
                $("#edit_addressinfo_stateorprovince").val(poi.AddressInfo.StateOrProvince);
                $("#edit_addressinfo_postcode").val(poi.AddressInfo.Postcode);
                this.setDropdown("edit_addressinfo_countryid", poi.AddressInfo.Country.ID);
                $("#edit_addressinfo_latitude").val(poi.AddressInfo.Latitude);
                $("#edit_addressinfo_longitude").val(poi.AddressInfo.Longitude);
                //show map based on current position
                this.refreshEditorMap();
                $("#edit_addressinfo_accesscomments").val(poi.AddressInfo.AccessComments);
                $("#edit_addressinfo_contacttelephone1").val(poi.AddressInfo.ContactTelephone1);
                $("#edit_addressinfo_contacttelephone2").val(poi.AddressInfo.ContactTelephone2);
                $("#edit_addressinfo_contactemail").val(poi.AddressInfo.ContactEmail);
                $("#edit_addressinfo_relatedurl").val(poi.AddressInfo.RelatedURL);
                $("#edit_numberofpoints").val(poi.NumberOfPoints);
                $("#edit_usagecost").val(poi.UsageCost);
                $("#edit_generalcomments").val(poi.GeneralComments);
                this.setDropdown("edit_usagetype", poi.UsageType != null ? poi.UsageType.ID : "0");
                this.setDropdown("edit_statustype", poi.StatusType != null ? poi.StatusType.ID : "50");
                this.setDropdown("edit_submissionstatus", poi.SubmissionStatus != null ? poi.SubmissionStatus.ID : "1");
                this.setDropdown("edit_operator", poi.OperatorInfo != null ? poi.OperatorInfo.ID : "1");
                this.setDropdown("edit_dataprovider", poi.DataProvider != null ? poi.DataProvider.ID : "1");
                //show edit-only mode dropdowns
                $("#edit-operator-container").show();
                $("#edit-submissionstatus-container").show();
                /*$("#edit-dataprovider-container").show();*/
                //populate connection editor(s)
                if (poi.Connections != null) {
                    for (var n = 1; n <= this.numConnectionEditors; n++) {
                        var $connection = ($("#edit_connection" + n));
                        $connection.removeClass("panel-primary");
                        $connection.removeClass("panel-default");
                        if (poi.Connections.length >= n) {
                            //create editor section
                            var con = poi.Connections[n - 1];
                            if (con != null) {
                                if ($connection.length > 0) {
                                    //populate connection editor
                                    this.setDropdown("edit_connection" + n + "_connectiontype", con.ConnectionType != null ? con.ConnectionType.ID : "0");
                                    this.setDropdown("edit_connection" + n + "_level", con.Level != null ? con.Level.ID : "");
                                    this.setDropdown("edit_connection" + n + "_status", con.StatusType != null ? con.StatusType.ID : "0");
                                    this.setDropdown("edit_connection" + n + "_currenttype", con.CurrentType != null ? con.CurrentType.ID : "");
                                    $("#edit_connection" + n + "_amps").val(con.Amps);
                                    $("#edit_connection" + n + "_volts").val(con.Voltage);
                                    $("#edit_connection" + n + "_quantity").val(con.Quantity);
                                    $("#edit_connection" + n + "_powerkw").val(con.PowerKW);
                                    $connection.data("_connection_id", con.ID);
                                    $connection.addClass("panel-primary");
                                }
                            }
                        }
                        else {
                            //null data (if present) from connection editor
                            $connection.data("_connection_id", 0);
                            $connection.addClass("panel-default");
                        }
                    }
                }
            }
        };
        LocationEditor.prototype.refreshEditorMap = function () {
            var lat = parseFloat($("#edit_addressinfo_latitude").val());
            var lng = parseFloat($("#edit_addressinfo_longitude").val());
            if (this.editorMap != null) {
                if (this.editorMap != null) {
                    this.editMarker.setLatLng([lat, lng]);
                    this.editorMap.panTo([lat, lng]);
                    $("#editor-map").show();
                }
            }
            else {
                this.initEditorMap(lat, lng);
            }
        };
        LocationEditor.prototype.initEditorMap = function (currentLat, currentLng) {
            if (this.editorMapInitialised === false) {
                this.editorMapInitialised = true;
                var app = this;
                //listen for changes to lat/lng input boxes
                $('#edit_addressinfo_latitude, #edit_addressinfo_longitude').change(function () {
                    if (app.editorMap != null) {
                        //reflect new pos on map
                        app.refreshEditorMap();
                        //clear attribution when manually modified
                        app.positionAttribution = null;
                    }
                });
                // Create editor map view
                $("#editor-map").show();
                this.editorMap = this.mappingManager.createMapLeaflet("editor-map-canvas", currentLat, currentLng, false, 14);
                var unknownPowerMarker = L.AwesomeMarkers.icon({
                    icon: 'bolt',
                    color: 'darkpurple'
                });
                this.editMarker = new L.Marker(new L.LatLng(currentLat, currentLng), { draggable: true });
                this.editMarker.addTo(this.editorMap);
                $("#editor-map-canvas").show();
                this.editMarker.on("dragend", function () {
                    //move map to new map centre
                    var point = app.editMarker.getLatLng();
                    app.editorMap.panTo(point);
                    $("#edit_addressinfo_latitude").val(point.lat);
                    $("#edit_addressinfo_longitude").val(point.lng);
                    //suggest new address as well?
                    //clear attribution when manually modified
                    app.positionAttribution = null;
                });
                //refresh map rendering
                var map = this.editorMap;
                setTimeout(function () { map.invalidateSize(false); }, 300);
            }
            else {
                var point = app.editMarker.getLatLng();
                app.editorMap.panTo(point);
            }
        };
        LocationEditor.prototype.hasUserPermissionForPOI = function (poi, permissionLevel) {
            var userInfo = this.getLoggedInUserInfo();
            if (userInfo.Permissions != null) {
                if (userInfo.Permissions.indexOf("[Administrator=true]") != -1) {
                    return true;
                }
                if (permissionLevel == "Edit") {
                    //check if user has country level or all countries edit permission
                    if (userInfo.Permissions.indexOf("[CountryLevel_Editor=All]") != -1) {
                        return true;
                    }
                    if (poi.AddressInfo.Country != null) {
                        if (userInfo.Permissions.indexOf("[CountryLevel_Editor=" + poi.AddressInfo.Country.ID + "]") != -1) {
                            return true;
                        }
                    }
                }
            }
            return false;
        };
        return LocationEditor;
    }(OCM.AppBase));
    OCM.LocationEditor = LocationEditor;
})(OCM || (OCM = {}));
//# sourceMappingURL=OCM_App_LocationEditor.js.map