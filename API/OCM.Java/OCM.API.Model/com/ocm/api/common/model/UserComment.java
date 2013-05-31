package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the UserComment data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class UserComment {

   	private int mChargePointID = 0; 	
   	private String mComment = "";
   	private UserCommentType mUserCommentType = null;
   	private String mDateCreated = "";   	
   	private int mID = -1;
   	private int mRating = -1;
   	private String mRelatedURL = "";
   	private String mUserName = "";
   	   	
 	/**
   	 * A constructor to create an empty UserComment object.
   	 */
   	public UserComment() {};
   	 	
   	/**
   	 * A constructor to create and populate a UserComment object.
   	 * @param userCommentJSONObj A JSON object containing the UserComment data.
   	 */
	public UserComment(JSONObject userCommentJSONObj) {
   		JSONObject commentTypeJSONObj;

   		if (userCommentJSONObj != null) { 			
   			this.mChargePointID = userCommentJSONObj.optInt("ChargePointID", -1);
   			this.mComment = userCommentJSONObj.optString("Comment", "");

   			commentTypeJSONObj = userCommentJSONObj.optJSONObject("CommentType");
   			if (commentTypeJSONObj != null) {
   				this.mUserCommentType = new UserCommentType(commentTypeJSONObj);
   			} 
   			
   			this.mDateCreated = userCommentJSONObj.optString("DateCreated", "");
   			this.mID = userCommentJSONObj.optInt("ID", -1);
   			this.mRating = userCommentJSONObj.optInt("Rating", -1);
   			this.mRelatedURL = userCommentJSONObj.optString("RelatedURL", "");
   			this.mUserName = userCommentJSONObj.optString("UserName", "");

   		} 
   	}

	public int getmChargePointID() {
		return mChargePointID;
	}

	public String getmComment() {
		return mComment;
	}

	public UserCommentType getmUserCommentType() {
		return mUserCommentType;
	}

	public String getmDateCreated() {
		return mDateCreated;
	}

	public int getmID() {
		return mID;
	}

	public int getmRating() {
		return mRating;
	}

	public String getmRelatedURL() {
		return mRelatedURL;
	}

	public String getmUserName() {
		return mUserName;
	}

	public void setmChargePointID(int mChargePointID) {
		this.mChargePointID = mChargePointID;
	}

	public void setmComment(String mComment) {
		this.mComment = mComment;
	}

	public void setmUserCommentType(UserCommentType mUserCommentType) {
		this.mUserCommentType = mUserCommentType;
	}

	public void setmDateCreated(String mDateCreated) {
		this.mDateCreated = mDateCreated;
	}

	public void setmID(int mID) {
		this.mID = mID;
	}

	public void setmRating(int mRating) {
		this.mRating = mRating;
	}

	public void setmRelatedURL(String mRelatedURL) {
		this.mRelatedURL = mRelatedURL;
	}

	public void setmUserName(String mUserName) {
		this.mUserName = mUserName;
	}

}
