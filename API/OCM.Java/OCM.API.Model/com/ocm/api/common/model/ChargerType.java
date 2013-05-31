
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the ChargeType data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class ChargerType {
 
	private String mComments = "";
   	private int mId = -1;
   	private boolean mIsFastChargeCapable = false;
   	private String mTitle = "";

 	/**
   	 * A constructor to create an empty ChargerType object.
   	 */
   	public ChargerType() {};
   	
   	/**
   	 * A constructor to create and populate a ChargerType object.
   	 * @param chargerTypeJSONObj A JSON object containing the ChargerType data.
   	 */
   	public ChargerType(JSONObject chargerTypeJSONObj) {
  		if (chargerTypeJSONObj != null) {
			this.mComments = chargerTypeJSONObj.optString("Comments", "");
			this.mId =chargerTypeJSONObj.optInt("ID", -1);
			this.mIsFastChargeCapable = chargerTypeJSONObj.optBoolean("IsFastChargeCapable", false);
			this.mTitle = chargerTypeJSONObj.optString("Title", "");
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
