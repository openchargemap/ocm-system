/// <reference path="references/jquery.d.ts" />

//external declarations
declare var google;
declare var top;
declare function ocm_getdatasummary();

//OCM.Widgets.GeoSummary
export module OCM {
    export module Widgets {
        export class GeoSummary {

            private geochartContainerId: string;
            private datasummaryContainerId: string;

            constructor(chartContainerId: string, summaryContainerId: string) {
                this.geochartContainerId = chartContainerId;
                this.datasummaryContainerId = summaryContainerId;
            }

            drawVisualization() {
                $("#" + this.geochartContainerId).fadeIn(1500);

                var ocm_summary = ocm_getdatasummary();
                var totalLocations = 0;

                var data = new google.visualization.DataTable();
                data.addRows(ocm_summary.length);
                data.addColumn("string", "Country");
                data.addColumn("number", "Charging Locations");

                for (var i = 0; i < ocm_summary.length; i++) {
                    data.setValue(i, 0, ocm_summary[i].country);
                    data.setValue(i, 1, ocm_summary[i].itemcount);

                    totalLocations += ocm_summary[i].itemcount;
                }

                var options = {
                    width: 840,
                    height: 380,
                    backgroundColor: "#B3D1FF",
                    displayMode: "regions",
                    colorAxis: { colors: ["#5cb85c", "#9fdc9f"] }
                };

                var geochart = new google.visualization.GeoChart(
                    document.getElementById("visualization")
                    );

                google.visualization.events.addListener(geochart, "regionClick", function (eventData) {
                    var countryISO = eventData.region;
                    if (top != null && top.loadCountryMap) {
                        top.loadCountryMap(countryISO, countryISO);
                    }
                });

                geochart.draw(data, options);

                this.refreshDataSummary();
            }

            refreshDataSummary() {
                // <!--data summary-->
                var ocm_summary = ocm_getdatasummary();
                var summaryContent = "";
                var totalLocations = 0;
                var totalStations = 0;
                for (var i = 0; i < ocm_summary.length; i++) {
                    summaryContent += " <a title='" + ocm_summary[i].stationcount + " charging stations across " + ocm_summary[i].locationcount + " locations.' href='javascript:ocmSummary.loadCountryMap(\"" + ocm_summary[i].country + "\",\"" + ocm_summary[i].isocode + "\");'><strong>" + ocm_summary[i].country + ":</strong> " + ocm_summary[i].locationcount + "</a>&nbsp;";
                    totalLocations += ocm_summary[i].locationcount;
                    totalStations += ocm_summary[i].stationcount;
                }

                summaryContent += "<br/><strong>" + totalStations + "</strong> charging stations across <strong>" + totalLocations + "</strong> locations.";
                document.getElementById(this.datasummaryContainerId).innerHTML = summaryContent;

            }

            loadCountryMap(countryName, isoCode) {
                if (top != null && top.loadCountryMap) {
                    top.loadCountryMap(countryName, isoCode);
                }
                //document.getElementById("countrymap").src = "http://api.openchargemap.io/widgets/map/?maptitle=Charging%20Locations: " + countryName + "&maxresults=10000&countrycode=" + isoCode + "&filtercontrols=nearlocation,distance,country,operator,connectiontype,level,usage";
            }

            show() {
                this.drawVisualization();
            }
        }
    }
}


//var summary = new OCM.Widgets.GeoSummary("geochart", "datasummary");
//summary.show();

