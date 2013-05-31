 
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the UsageType data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class UsageType {

	private int mId = -1;
   	private String mIsAccessKeyRequired = "";
   	private String mIsMembershipRequired = "";
   	private String mIsPayAtLocation = "";
   	private String mTitle = "";

 	/**
   	 * A constructor to create an empty UsageType object.
   	 */
   	public UsageType() {};
   	 
   	/**
   	 * A constructor to create and populate a UsageType object.
   	 * @param usageTypeJSONObj A JSON object containing the UsageType data.
   	 */
   	public UsageType(JSONObject usageTypeJSONObj) {
   		if (usageTypeJSONObj != null) {
   			this.mId = usageTypeJSONObj.optInt("ID", -1);
   			this.mIsAccessKeyRequired = usageTypeJSONObj.optString("IsAccessKeyRequired", "");
   			this.mIsMembershipRequired = usageTypeJSONObj.optString("IsMembershipRequired", "");
   			this.mIsPayAtLocation = usageTypeJSONObj.optString("IsPayAtLocation", "");
   			this.mTitle = usageTypeJSONObj.optString("Title", "");
   		} 
   	}
   	
 	public int getID() {
		return this.mId;
	}
 	
 	public void setID(int iD) {
		this.mId = iD;
	}
	
 	public String getIsAccessKeyRequired() {
		return this.mIsAccessKeyRequired;
	}
 	
 	public void setIsAccessKeyRequired(String isAccessKeyRequired) {
		this.mIsAccessKeyRequired = isAccessKeyRequired;
	}
	
 	public String getIsMembershipRequired() {
		return this.mIsMembershipRequired;
	}
 	
 	public void setIsMembershipRequired(String isMembershipRequired) {
		this.mIsMembershipRequired = isMembershipRequired;
	}
	
 	public String getIsPayAtLocation() {
		return this.mIsPayAtLocation;
	}
 	
 	public void setIsPayAtLocation(String isPayAtLocation) {
		this.mIsPayAtLocation = isPayAtLocation;
	}
	
 	public String getTitle() {
		return this.mTitle;
	}
 	
 	public void setTitle(String title) {
		this.mTitle = title;
	}
}
