package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the Country data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class Country {

	private int mId = -1;
   	private String mISOCode = "";
   	private String mTitle = "";

 	/**
   	 * A constructor to create an empty Country object.
   	 */
   	public Country() {};

   	/**
   	 * A constructor to create and populate a Country object.
   	 * @param countryJSONObj A JSON object containing the Country data.
   	 */
  	public Country(JSONObject countryJSONObj) {
   		if (countryJSONObj != null) {
   			this.mId = countryJSONObj.optInt("ID");
   			this.mISOCode = countryJSONObj.optString("ISOCode");
   			this.mTitle = countryJSONObj.optString("Title");
   		} 
   	}

   	public int getID() {
		return this.mId;
	}
 	
   	public void setID(int iD) {
		this.mId = iD;
	}
	
 	public String getISOCode() {
		return this.mISOCode;
	}
 	
 	public void setISOCode(String iSOCode) {
		this.mISOCode = iSOCode;
	}
	
 	public String getTitle() {
		return this.mTitle;
	}
 	
 	public void setTitle(String title) {
		this.mTitle = title;
	}
}
