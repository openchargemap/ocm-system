
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the ConnectionType data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class ConnectionType {

	private String mFormalName = "";
   	private int mId = -1;
   	private String mIsDiscontinued = "";
   	private String mIsObsolete = "";
   	private String mTitle = "";

 	/**
   	 * A constructor to create an empty ConnectionType object.
   	 */
	public ConnectionType(){};

  	/**
   	 * A constructor to create and populate a ConnectionType object.
   	 * @param connectionTypeJSONObj A JSON object containing the ConnectionType data.
   	 */
	public ConnectionType(JSONObject connectionTypeJSONObj) {
   		if (connectionTypeJSONObj != null) {
   			this.mFormalName = connectionTypeJSONObj.optString("FormalName", "");
   			this.mId = connectionTypeJSONObj.optInt("ID", -1);
   			this.mIsDiscontinued = connectionTypeJSONObj.optString("IsDiscontinued", "");
   			this.mIsObsolete = connectionTypeJSONObj.optString("IsObsolete", "");
   			this.mTitle = connectionTypeJSONObj.optString("Title", "");
   		}
   	}
   	
   	public String getFormalName() {
		return this.mFormalName;
	}
 	
   	public void setFormalName(String formalName) {
		this.mFormalName = formalName;
	}
	
 	public int getID() {
		return this.mId;
	}
 	
 	public void setID(int iD) {
		this.mId = iD;
	}
	
 	public String getIsDiscontinued() {
		return this.mIsDiscontinued;
	}
 	
 	public void setIsDiscontinued(String isDiscontinued) {
		this.mIsDiscontinued = isDiscontinued;
	}
	
 	public String getIsObsolete() {
		return this.mIsObsolete;
	}
 	
 	public void setIsObsolete(String isObsolete) {
		this.mIsObsolete = isObsolete;
	}
	
 	public String getTitle() {
		return this.mTitle;
	}
 	
 	public void setTitle(String title) {
		this.mTitle = title;
	}
}
