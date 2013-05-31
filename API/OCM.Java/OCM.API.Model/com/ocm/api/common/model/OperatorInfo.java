

package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the OperatorInfo data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class OperatorInfo {
	
   	private String mAddressInfo = "";
   	private String mBookingURL = "";
   	private String mComments = "";
   	private int mId = -1;
   	private String mIsPrivateIndividual = "";
   	private String mPhonePrimaryContact = "";
   	private String mPhoneSecondaryContact = "";
   	private String mTitle = "";
   	private String mWebsiteURL = "";

 	/**
   	 * A constructor to create an empty OperatorInfo object.
   	 */
   	public OperatorInfo() {}
   	
   	/**
   	 * A constructor to create and populate a OperatorInfo object.
   	 * @param operatorInfoJSONObj A JSON object containing the OperatorInfo data.
   	 */
   	public OperatorInfo(JSONObject operatorInfoJSONObj) {
   		if (operatorInfoJSONObj != null) {
   			this.mAddressInfo = operatorInfoJSONObj.optString("AddressInfo", "");
   			this.mBookingURL = operatorInfoJSONObj.optString("BookingURL", "");
   			this.mComments = operatorInfoJSONObj.optString("Comments", "");
   			this.mId = operatorInfoJSONObj.optInt("ID", -1);
   			this.mIsPrivateIndividual = operatorInfoJSONObj.optString("IsPrivateIndividual", "");
   			this.mPhonePrimaryContact = operatorInfoJSONObj.optString("PhonePrimaryContact", "");
   			this.mPhoneSecondaryContact = operatorInfoJSONObj.optString("PhoneSecondaryContact", "");
   			this.mTitle = operatorInfoJSONObj.optString("Title", "");
   			this.mWebsiteURL = operatorInfoJSONObj.optString("WebsiteURL", "");
   		} 	
   	}
   	
 	public String getAddressInfo() {
		return this.mAddressInfo;
	}
 	
	public void setAddressInfo(String addressInfo) {
		this.mAddressInfo = addressInfo;
	}
	
 	public String getBookingURL() {
		return this.mBookingURL;
	}
 	
	public void setBookingURL(String bookingURL) {
		this.mBookingURL = bookingURL;
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
	
 	public String getIsPrivateIndividual() {
		return this.mIsPrivateIndividual;
	}
 	
	public void setIsPrivateIndividual(String isPrivateIndividual) {
		this.mIsPrivateIndividual = isPrivateIndividual;
	}
	
 	public String getPhonePrimaryContact() {
		return this.mPhonePrimaryContact;
	}
 	
	public void setPhonePrimaryContact(String phonePrimaryContact) {
		this.mPhonePrimaryContact = phonePrimaryContact;
	}
	
 	public String getPhoneSecondaryContact() {
		return this.mPhoneSecondaryContact;
	}
 	
	public void setPhoneSecondaryContact(String phoneSecondaryContact) {
		this.mPhoneSecondaryContact = phoneSecondaryContact;
	}
	
 	public String getTitle() {
		return this.mTitle;
	}
 	
	public void setTitle(String title) {
		this.mTitle = title;
	}
	
 	public String getWebsiteURL() {
		return this.mWebsiteURL;
	}
 	
	public void setWebsiteURL(String websiteURL) {
		this.mWebsiteURL = websiteURL;
	}
}
