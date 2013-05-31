
package com.ocm.api.common.model;

import java.util.ArrayList;
import java.util.List;

import org.json.JSONArray;
import org.json.JSONObject;

/**
 * A class that represents a ChargePoint.
 * @author Thomas Snuggs
 *
 */
public class ChargePoint {
 
	private AddressInfo mAddressInfo = null;
   	private List<ChargerType> mChargers = null;
   	private List<ConnectionInfo> mConnections = null;
   	private DataProvider mDataProvider = null;
   	private String mDataProvidersReference = "";
   	private String mDataQualityLevel = "";
   	private String mDateCreated = "";
   	private String mDateLastConfirmed = "";
   	private String mDateLastStatusUpdate = "";
   	private String mDatePlanned = "";
   	private String mGeneralComments = "";
   	private int mId = -1;
   	private String mMetadataTags = "";
   	private int mNumberOfPoints = 0;
   	private OperatorInfo mOperatorInfo = null;
   	private String mOperatorsReference = "";
   	private String mPercentageSimilarity = "";
   	private StatusType mStatusType = null;
   	private SubmissionStatus mSubmissionStatus = null;
   	private String mUUID = "";
   	private String mUsageCost = "";
   	private UsageType mUsageType = null;
   	private List<UserComment> mUserComments = null;
   	
 	/**
   	 * A constructor to create an empty Country object.
   	 */
   	public ChargePoint() {};
   	
   	/**
   	 * A constructor to create and populate a ChargePoint object.
   	 * @param chargePointJSONObj A JSON object containing the ChargePoint data.
   	 */
   	public ChargePoint(JSONObject chargePointJSONObj) {
   		JSONArray chargersJSONArray;
   		JSONArray connectionsJSONArray;
   		JSONArray userCommentsJSONArray;
   		
   		if (chargePointJSONObj != null) { 			
   			this.mAddressInfo = new AddressInfo(chargePointJSONObj.optJSONObject("AddressInfo"));
   			
   			chargersJSONArray = chargePointJSONObj.optJSONArray("Chargers");
   			if (chargersJSONArray != null) {
   				this.mChargers = new ArrayList<ChargerType>();
   				for (int i=0; i<chargersJSONArray.length(); i++) {
   					this.mChargers.add(new ChargerType(chargersJSONArray.optJSONObject(i)));
   				}
   			}
   			
   			connectionsJSONArray = chargePointJSONObj.optJSONArray("Connections");
   			if (connectionsJSONArray != null) {
   				this.mConnections = new ArrayList<ConnectionInfo>();
   				for (int i=0; i<connectionsJSONArray.length(); i++) {
   					this.mConnections.add(new ConnectionInfo(connectionsJSONArray.optJSONObject(i)));
   				}
   			} 
   			
   			this.mDataProvider = new DataProvider(chargePointJSONObj.optJSONObject("DataProvider"));
   			this.mDataProvidersReference = chargePointJSONObj.optString("DataProvidersReference", "");
   			this.mDataQualityLevel = chargePointJSONObj.optString("DataQualityLevel", "");
   			this.mDateCreated = chargePointJSONObj.optString("DateCreated", "");
   			this.mDateLastConfirmed = chargePointJSONObj.optString("DateLastConfirmed", "");
   			this.mDateLastStatusUpdate = chargePointJSONObj.optString("DateLastStatusUpdate", "");
   			this.mDatePlanned = chargePointJSONObj.optString("DatePlanned", "");
   			this.mGeneralComments = chargePointJSONObj.optString("GeneralComments", "");
   			this.mId = chargePointJSONObj.optInt("ID", -1);
   			this.mMetadataTags = chargePointJSONObj.optString("MetadataTags", "");
   			this.mNumberOfPoints = chargePointJSONObj.optInt("NumberOfPoints", 0); 			
   			this.mOperatorInfo = new OperatorInfo(chargePointJSONObj.optJSONObject("OperatorInfo"));
  			this.mOperatorsReference = chargePointJSONObj.optString("OperatorsReference", "");
   			this.mPercentageSimilarity = chargePointJSONObj.optString("PercentageSimilarity", "");
   			this.mStatusType = new StatusType(chargePointJSONObj.optJSONObject("StatusType"));
   			this.mSubmissionStatus = new SubmissionStatus(chargePointJSONObj.optJSONObject("SubmissionStatus"));
  			this.mUUID = chargePointJSONObj.optString("UUID", "");
   			this.mUsageCost = chargePointJSONObj.optString("UsageCost", ""); 			
   			this.mUsageType = new UsageType(chargePointJSONObj.optJSONObject("UsageType"));		

   			userCommentsJSONArray = chargePointJSONObj.optJSONArray("UserComments");
   			if (userCommentsJSONArray != null) {
   				this.mUserComments = new ArrayList<UserComment>();
   				for (int i=0; i<userCommentsJSONArray.length(); i++) {
   					this.mUserComments.add(new UserComment(userCommentsJSONArray.optJSONObject(i)));
   				}
   			}
   		} 
   	}

   	public AddressInfo getAddressInfo() {
   		return this.mAddressInfo;
   	}

   	public void setAddressInfo(AddressInfo addressInfo) {
   		this.mAddressInfo = addressInfo;
   	}

   	public List<ChargerType> getChargers() {
		return this.mChargers;
	}
 	
   	public void setChargers(List<ChargerType> chargerType) {
		this.mChargers = chargerType;
	}
	
 	public List<ConnectionInfo> getConnections() {
		return this.mConnections;
	}
 	
 	public void setConnections(ArrayList<ConnectionInfo> connections) {
		this.mConnections = connections;
	}
	
 	public DataProvider getDataProvider() {
		return this.mDataProvider;
	}
 	
 	public void setDataProvider(DataProvider dataProvider) {
		this.mDataProvider = dataProvider;
	}
	
 	public String getDataProvidersReference() {
		return this.mDataProvidersReference;
	} 	
 	
 	public void setDataProvidersReference(String dataProvidersReference) {
		this.mDataProvidersReference = dataProvidersReference;
	}
	
 	public String getDataQualityLevel() {
		return this.mDataQualityLevel;
	}
 	
 	public void setDataQualityLevel(String dataQualityLevel) {
		this.mDataQualityLevel = dataQualityLevel;
	}
	
 	public String getDateCreated() {
		return this.mDateCreated;
	}
 	
 	public void setDateCreated(String dateCreated) {
		this.mDateCreated = dateCreated;
	}
	
 	public String getDateLastConfirmed() {
		return this.mDateLastConfirmed;
	}
 	
 	public void setDateLastConfirmed(String dateLastConfirmed) {
		this.mDateLastConfirmed = dateLastConfirmed;
	}
	
 	public String getDateLastStatusUpdate() {
		return this.mDateLastStatusUpdate;
	}
 	
 	public void setDateLastStatusUpdate(String dateLastStatusUpdate) {
		this.mDateLastStatusUpdate = dateLastStatusUpdate;
	}
	
 	public String getDatePlanned() {
		return this.mDatePlanned;
	}
 	
 	public void setDatePlanned(String datePlanned) {
		this.mDatePlanned = datePlanned;
	}
	
 	public String getGeneralComments() {
		return this.mGeneralComments;
	}
 	
 	public void setGeneralComments(String generalComments) {
		this.mGeneralComments = generalComments;
	}
	
 	public int getID() {
		return this.mId;
	}
 	
 	public void setID(int iD) {
		this.mId = iD;
	}
		
 	public String getMetadataTags() {
		return this.mMetadataTags;
	}
 	
 	public void setMetadataTags(String metadataTags) {
		this.mMetadataTags = metadataTags;
	}
	
 	public int getNumberOfPoints() {
		return this.mNumberOfPoints;
	}
 	
 	public void setNumberOfPoints(int numberOfPoints) {
		this.mNumberOfPoints = numberOfPoints;
	}
	
 	public OperatorInfo getOperatorInfo() {
		return this.mOperatorInfo;
	}
 	
 	public void setOperatorInfo(OperatorInfo operatorInfo) {
		this.mOperatorInfo = operatorInfo;
	}
	
 	public String getOperatorsReference() {
		return this.mOperatorsReference;
	}	
 	
 	public void setOperatorsReference(String operatorsReference) {
		this.mOperatorsReference = operatorsReference;
	}
	
 	public String getPercentageSimilarity() {
		return this.mPercentageSimilarity;
	}
 	
 	public void setPercentageSimilarity(String percentageSimilarity) {
		this.mPercentageSimilarity = percentageSimilarity;
	}
	
 	public StatusType getStatusType() {
		return this.mStatusType;
	}
 	
 	public void setStatusType(StatusType statusType) {
		this.mStatusType = statusType;
	}
	
 	public SubmissionStatus getSubmissionStatus() {
		return this.mSubmissionStatus;
	}
 	
 	public void setSubmissionStatus(SubmissionStatus submissionStatus) {
		this.mSubmissionStatus = submissionStatus;
	}
	
 	public String getUUID() {
		return this.mUUID;
	}	
 	
 	public void setUUID(String uUID) {
		this.mUUID = uUID;
	}
	
 	public String getUsageCost() {
		return this.mUsageCost;
	}
 	
 	public void setUsageCost(String usageCost) {
		this.mUsageCost = usageCost;
	}
	
 	public UsageType getUsageType() {
		return this.mUsageType;
	}
 	
 	public void setUsageType(UsageType usageType) {
		this.mUsageType = usageType;
	}
	
 	public List<UserComment> getUserComments() {
		return this.mUserComments;
	}
 	
 	public void setUserComments(List<UserComment> userComments) {
		this.mUserComments = userComments;
	}
}
