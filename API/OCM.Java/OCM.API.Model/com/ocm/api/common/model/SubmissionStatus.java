
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the SubmissionStatus data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class SubmissionStatus {
	
   	private int mId = -1;
   	private boolean mIsLive = false;
   	private String mTitle = "";

 	/**
   	 * A constructor to create an empty SubmissionStatus object.
   	 */
   	public SubmissionStatus() {};

   	/**
   	 * A constructor to create and populate a SubmissionStatus object.
   	 * @param submissionStatusJSONObj A JSON object containing the SubmissionStatus data.
   	 */
   	public SubmissionStatus(JSONObject submissionStatusJSONObj) {
   		if (submissionStatusJSONObj != null) {
   			this.mId = submissionStatusJSONObj.optInt("ID", -1);
   			this.mIsLive = submissionStatusJSONObj.optBoolean("IsLive", false);
   			this.mTitle = submissionStatusJSONObj.optString("Title", "");
   		} 
   	}

   	public int getID() {
   		return this.mId;
	}	
 	
   	public void setID(int iD) {
		this.mId = iD;
	}
	
 	public boolean getIsLive() {
		return this.mIsLive;
	}
 	
 	public void setIsLive(boolean isLive) {
		this.mIsLive = isLive;
	}
	
 	public String getTitle() {
		return this.mTitle;
	}
 	
 	public void setTitle(String title) {
		this.mTitle = title;
	}
}
