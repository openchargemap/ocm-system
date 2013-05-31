package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the UserCommentType data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class UserCommentType {
	
   	private int mId = -1;
   	private String mTitle = "";

 	/**
   	 * A constructor to create an empty UserCommentType object.
   	 */
   	public UserCommentType() {};
   	
   	/**
   	 * A constructor to create and populate a UserCommentType object.
   	 * @param userCommentTypeJSONObj A JSON object containing the UserCommentType data.
   	 */
   	public UserCommentType(JSONObject userCommentTypeJSONObj) {
   		if (userCommentTypeJSONObj != null) {
   			this.mId = userCommentTypeJSONObj.optInt("ID", -1);
   			this.mTitle = userCommentTypeJSONObj.optString("Title", "");
   		} 
   	}
   	
 	public int getID() {
		return this.mId;
	}
 	
 	public void setID(int iD) {
		this.mId = iD;
	}
		
 	public String getTitle() {
		return this.mTitle;
	}
 	
 	public void setTitle(String title) {
		this.mTitle = title;
	}
}