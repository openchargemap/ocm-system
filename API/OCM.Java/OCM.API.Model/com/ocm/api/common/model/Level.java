 
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the Level data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class Level {
	
   	private String mComments = "";
   	private int mId = -1;
   	private boolean mIsFastChargeCapable = false;
   	private String mTitle = "";

 	/**
   	 * A constructor to create an empty Level object.
   	 */
   	public Level() {};
   	
   	/**
   	 * A constructor to create and populate a Level object.
   	 * @param levelJSONObj A JSON object containing the Level data.
   	 */
   	public Level(JSONObject levelJSONObj) {
   		if (levelJSONObj != null) {
   			this.mComments = levelJSONObj.optString("Comments", "");
   			this.mId = levelJSONObj.optInt("ID", -1);
   			this.mIsFastChargeCapable = levelJSONObj.optBoolean("IsFastChargeCapable", false);
   			this.mTitle = levelJSONObj.optString("Title", "");
   		}
   	}
   	
   	public String getComments() {
		return this.mComments;
	}
 	
   	public void setComments(String comments) {
		this.mComments = comments;
	}
	
 	public int getID() {
		return this.mId;
	}
 	
 	public void setID(int iD) {
		this.mId = iD;
	}
	
 	public boolean getIsFastChargeCapable() {
		return this.mIsFastChargeCapable;
	}
 	
 	public void setIsFastChargeCapable(boolean isFastChargeCapable) {
		this.mIsFastChargeCapable = isFastChargeCapable;
	}
	
 	public String getTitle() {
		return this.mTitle;
	}
 	
 	public void setTitle(String title) {
		this.mTitle = title;
	}
}
