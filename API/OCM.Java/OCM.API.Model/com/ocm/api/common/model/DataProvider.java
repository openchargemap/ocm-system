
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the DataProvider data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class DataProvider {
	
   	private String mComments = "";
   	private int mId = -1;
   	private String mTitle = "";
   	private String mWebsiteURL = "";

 	/**
   	 * A constructor to create an empty DataProvider object.
   	 */
    public DataProvider() {};
   	
   	/**
   	 * A constructor to create and populate a DataProvider object.
   	 * @param dataProviderJSONObj A JSON object containing the DataProvider data.
   	 */
    public DataProvider(JSONObject dataProviderJSONObj) {
   		if (dataProviderJSONObj != null) {
   			this.mComments = dataProviderJSONObj.optString("Comments", "");
   			this.mId = dataProviderJSONObj.optInt("ID", -1);
   			this.mTitle = dataProviderJSONObj.optString("Title", "");
   			this.mWebsiteURL = dataProviderJSONObj.optString("WebsiteURL", "");				
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
