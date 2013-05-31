package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the AddressInfo data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class AddressInfo {
	
   	private String mAccessComments = "";
   	private String mAddressLine1 = "";
   	private String mAddressLine2 = "";
   	private String mContactEmail = "";
   	private String mContactTelephone1 = "";
   	private String mContactTelephone2 = "";
   	private Country mCountry = null;
   	private double mDistance = -1;
   	private double mDistanceUnit = -1;
   	private String mGeneralComments = "";
   	private int mId = -1;
   	private double mLatitude = 0xffff;
   	private double mLongitude = 0xffff;
   	private String mPostcode = "";
   	private String mRelatedURL = "";
   	private String mStateOrProvince = "";
   	private String mTitle = "";
   	private String mTown = "";
   	
   	/**
   	 * A constructor to create an empty AddressInfo object.
   	 */
   	public AddressInfo() {};
   	
   	/**
   	 * A constructor to create and populate an AddressInfo object.
   	 * @param addressInfoJSONObj A JSON object containing the AddressInfo data.
   	 */
   	public AddressInfo(JSONObject addressInfoJSONObj) {
   		JSONObject countryJSONObj;

   		if (addressInfoJSONObj != null) {
   			this.mAccessComments = addressInfoJSONObj.optString("AccessComments", "");
   			this.mAddressLine1 = addressInfoJSONObj.optString("AddressLine1", "");
   			this.mAddressLine2 = addressInfoJSONObj.optString("AddressLine2", "");
   			this.mContactEmail = addressInfoJSONObj.optString("ContactEmail", "");
   			this.mContactTelephone1 = addressInfoJSONObj.optString("ContactTelephone1", "");
   			this.mContactTelephone2 = addressInfoJSONObj.optString("ContactTelephone2", "");
   			
   			countryJSONObj = addressInfoJSONObj.optJSONObject("Country");
   			if (countryJSONObj != null) {
   				this.mCountry = new Country(countryJSONObj);
   			}  			
   			
   			this.mDistance = addressInfoJSONObj.optDouble("Distance", -1);
   			this.mDistanceUnit = addressInfoJSONObj.optDouble("DistanceUnit", -1);
   			this.mGeneralComments = addressInfoJSONObj.optString("GeneralComments", "");
   			this.mId = addressInfoJSONObj.optInt("ID", -1);
   			this.mLatitude = addressInfoJSONObj.optDouble("Latitude", 0xff);
   			this.mLongitude = addressInfoJSONObj.optDouble("Longitude", 0xff);
   			this.mPostcode = addressInfoJSONObj.optString("Postcode", "");
   			this.mRelatedURL = addressInfoJSONObj.optString("RelatedURL", "");
   			this.mStateOrProvince = addressInfoJSONObj.optString("StateOrProvince", "");
   			this.mTitle = addressInfoJSONObj.optString("Title", "");
   			this.mTown = addressInfoJSONObj.optString("Town", "");
   		} 
   	}

 	public String getAccessComments() {
		return this.mAccessComments;
	}
 	
 	public void setAccessComments(String accessComments) {
		this.mAccessComments = accessComments;
	}	
	
 	public String getAddressLine1() {
		return this.mAddressLine1;
	}
 	
 	public void setAddressLine1(String addressLine1) {
		this.mAddressLine1 = addressLine1;
	}
	
 	public String getAddressLine2() {
		return this.mAddressLine2;
	}
 	
 	public void setAddressLine2(String addressLine2) {
		this.mAddressLine2 = addressLine2;
	}
	
 	public String getContactEmail() {
		return this.mContactEmail;
	}
 	
 	public void setContactEmail(String contactEmail) {
		this.mContactEmail = contactEmail;
	}
	
 	public String getContactTelephone1() {
		return this.mContactTelephone1;
	}
 	
 	public void setContactTelephone1(String contactTelephone1) {
		this.mContactTelephone1 = contactTelephone1;
	}
	
 	public String getContactTelephone2() {
		return this.mContactTelephone2;
	}
 	
 	public void setContactTelephone2(String contactTelephone2) {
		this.mContactTelephone2 = contactTelephone2;
	}
	
 	public Country getCountry() {
		return this.mCountry;
	}
 	
 	public void setCountry(Country country) {
		this.mCountry = country;
	}
	
 	public double getDistance() {
		return this.mDistance;
	}
 	
 	public void setDistance(double distance) {
		this.mDistance = distance;
	}
	
 	public double getDistanceUnit() {
		return this.mDistanceUnit;
	}
 	
 	public void setDistanceUnit(double distanceUnit) {
		this.mDistanceUnit = distanceUnit;
	}
	
 	public String getGeneralComments() {
		return this.mGeneralComments;
	}
 	
 	public void setGeneralComments(String generalComments) {
		this.mGeneralComments = generalComments;
	}
	
 	public int getID() {
		return this.mId;
	}
 	
 	public void setID(int iD) {
		this.mId = iD;
	}
	
 	public double getLatitude() {
		return this.mLatitude;
	}
 	
 	public void setLatitude(double latitude) {
		this.mLatitude = latitude;
	}
	
 	public double getLongitude() {
		return this.mLongitude;
	}
 	
 	public void setLongitude(double longitude) {
		this.mLongitude = longitude;
	}
	
 	public String getPostcode() {
		return this.mPostcode;
	}
 	
 	public void setPostcode(String postcode) {
		this.mPostcode = postcode;
	}
	
 	public String getRelatedURL() {
		return this.mRelatedURL;
	}
 	
 	public void setRelatedURL(String relatedURL) {
		this.mRelatedURL = relatedURL;
	}
	
 	public String getStateOrProvince() {
		return this.mStateOrProvince;
	}
 	
 	public void setStateOrProvince(String stateOrProvince) {
		this.mStateOrProvince = stateOrProvince;
	}
	
 	public String getTitle() {
		return this.mTitle;
	}
 	
 	public void setTitle(String title) {
		this.mTitle = title;
	}
	
 	public String getTown() {
		return this.mTown;
	}
 	
 	public void setTown(String town) {
		this.mTown = town;
	}
}
