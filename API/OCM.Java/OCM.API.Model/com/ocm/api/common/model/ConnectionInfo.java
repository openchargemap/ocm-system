
package com.ocm.api.common.model;

import org.json.JSONObject;

/**
 * A class that represents the ConnectionInfo data for a Charge Point.
 * @author Thomas Snuggs
 *
 */
public class ConnectionInfo {
	
   	private String mAmps = "";
   	private ConnectionType mConnectionType = null;
   	private int mId;
   	private Level mLevel = null;
   	private String mQuantity = "";
   	private String mReference = "";
   	private String mStatusType = "";
   	private String mVoltage = "";

 	/**
   	 * A constructor to create an empty ConnectionInfo object.
   	 */
	public ConnectionInfo() {};

   	/**
   	 * A constructor to create and populate a ConnectionInfo object.
   	 * @param conectionInfoJSONObj A JSON object containing the ConnectionInfo data.
   	 */
	public ConnectionInfo(JSONObject conectionInfoJSONObj) {		
   		if (conectionInfoJSONObj != null) {
   			this.mAmps = conectionInfoJSONObj.optString("Amps", "");
   			this.mConnectionType = new ConnectionType(conectionInfoJSONObj.optJSONObject("ConnectionType"));
   			this.mId = conectionInfoJSONObj.optInt("ID", -1);
   			this.mLevel = new Level(conectionInfoJSONObj.optJSONObject("Level"));
   			this.mQuantity = conectionInfoJSONObj.optString("Quantity", "");
   			this.mReference = conectionInfoJSONObj.optString("Reference", "");
   			this.mStatusType = conectionInfoJSONObj.optString("StatusType", "");
   			this.mVoltage = conectionInfoJSONObj.optString("Voltage", "");
   		} 
   	}
   	
   	public String getAmps() {
		return this.mAmps;
	}
 	
   	public void setAmps(String amps) {
		this.mAmps = amps;
	}	
	
 	public ConnectionType getConnectionType() {
		return this.mConnectionType;
	}
 	
 	public void setConnectionType(ConnectionType connectionType) {
		this.mConnectionType = connectionType;
	}
	
 	public int getID() {
		return this.mId;
	}
 	
 	public void setID(int iD) {
		this.mId = iD;
	}
	
 	public Level getLevel() {
		return this.mLevel;
	}
 	
 	public void setLevel(Level level) {
		this.mLevel = level;
	}
	
 	public String getQuantity() {
		return this.mQuantity;
	}
 	
 	public void setQuantity(String quantity) {
		this.mQuantity = quantity;
	}
	
 	public String getReference() {
		return this.mReference;
	}
 	
 	public void setReference(String reference) {
		this.mReference = reference;
	}
	
 	public String getStatusType() {
		return this.mStatusType;
	}
 	
 	public void setStatusType(String statusType) {
		this.mStatusType = statusType;
	}
	
 	public String getVoltage() {
		return this.mVoltage;
	}
 	
 	public void setVoltage(String voltage) {
		this.mVoltage = voltage;
	}
}
