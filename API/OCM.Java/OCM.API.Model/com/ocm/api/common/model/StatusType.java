
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the StatusType data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class StatusType {

	private int mId = -1;
	private String mIsOperational = "";
	private String mTitle = "";

 	/**
   	 * A constructor to create an empty StatusType object.
   	 */
	public StatusType() {};

   	/**
   	 * A constructor to create and populate a StatusType object.
   	 * @param statusTypeJSONObj A JSON object containing the StatusType data.
   	 */
	public StatusType(JSONObject statusTypeJSONObj) {
		if (statusTypeJSONObj != null) {
			this.mId = statusTypeJSONObj.optInt("ID", -1);
			this.mIsOperational = statusTypeJSONObj.optString("IsOperational", "");
			this.mTitle = statusTypeJSONObj.optString("Title", "");

		} 
	}

	public int getID() {
		return this.mId;
	}

	public void setID(int iD) {
		this.mId = iD;
	}

	public String getIsOperational() {
		return this.mIsOperational;
	}

	public void setIsOperational(String isOperational) {
		this.mIsOperational = isOperational;
	}

	public String getTitle() {
		return this.mTitle;
	}

	public void setTitle(String title) {
		this.mTitle = title;
	}
}
