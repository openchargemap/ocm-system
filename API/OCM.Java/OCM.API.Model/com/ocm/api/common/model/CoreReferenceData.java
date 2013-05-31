package com.ocm.api.common.model;

import java.util.HashMap;
import java.util.Map;

import org.json.JSONArray;
import org.json.JSONObject;

/**
 * A Singleton class that represents the OCM Core Reference Data. Typically, only a single instance
 * of this class instance is needed for a given project. 
 * 
 * An example usage is as follows:
 * 
 * - Get JSON data from OCM via: http://openchargemap.org/api/?action=getcorereferencedata&output=json 
 * - Invoke method populateCoreReferenceData() passing in the JSON core reference data object retrieved
 *   from the OCM api (i.e. CoreReferenceData.getInstance().populateCoreReferenceData(the json data)).
 * 
 * @author Thomas Snuggs
 *
 */
public class CoreReferenceData {

	private static CoreReferenceData mInstance = null;

	// The data is stored in HashMaps for quick access.
	private Map<String, ChargerType> mChargerTypesMap = new HashMap<String, ChargerType>();
	private Map<String, Country> mCountriesMap = new HashMap<String, Country>();
	private Map<String, ConnectionType> mConnectionTypesMap = new HashMap<String, ConnectionType>();
	private Map<String, DataProvider> mDataProvidersMap = new HashMap<String, DataProvider>();
	private Map<String, OperatorInfo> mOperatorsMap = new HashMap<String, OperatorInfo>();
	private Map<String, UsageType> mUsageTypesMap = new HashMap<String, UsageType>();	
	private Map<String, UserCommentType> mUserCommentTypesMap = new HashMap<String, UserCommentType>();
	private Map<String, StatusType> mStatusTypesMap = new HashMap<String, StatusType>();
	private Map<String, SubmissionStatus> mSubmissionStatusTypesMap = new HashMap<String, SubmissionStatus>();

	/**
	 * Constructor
	 */
	private CoreReferenceData() {}

	/**
	 * Get the instance of the object. 
	 * @return Instance of CoreReferenceData
	 */
	public static synchronized CoreReferenceData getInstance () { 
		if (mInstance == null) {
			mInstance = new CoreReferenceData();
		}
		return mInstance;
	}

	/**
	 * Populates the CoreReferenceData object with all the reference data.
	 * @param coreReferenceObject The JSON object that contains the Core Reference Data
	 */
	public synchronized void populateCoreReferenceData(JSONObject coreReferenceObject) {
		if (coreReferenceObject != null) {
			this.populateChargerTypes(coreReferenceObject.optJSONArray("ChargerTypes"));
			this.populateConnectionTypes(coreReferenceObject.optJSONArray("ConnectionTypes"));
			this.populateCountries(coreReferenceObject.optJSONArray("Countries"));
			this.populateDataProviders(coreReferenceObject.optJSONArray("DataProviders"));
			this.populateOperators(coreReferenceObject.optJSONArray("Operators"));
			this.populateUsageTypes(coreReferenceObject.optJSONArray("UsageTypes"));
			this.populateUserCommentTypes(coreReferenceObject.optJSONArray("UserCommentTypes"));
			this.populateStatusTypes(coreReferenceObject.optJSONArray("StatusTypes"));
			this.populateSudmissionStatusTypes(coreReferenceObject.optJSONArray("SubmissionStatusTypes"));			
		}
	}

	private void populateChargerTypes(JSONArray chargerTypesJSONArray) {
		ChargerType chargerType;
		JSONObject chargerTypeJSONObj;

		if (chargerTypesJSONArray != null) {
			this.mChargerTypesMap.clear();

			for (int i=0; i<chargerTypesJSONArray.length(); i++) {	
				chargerType = new ChargerType();
				chargerTypeJSONObj = chargerTypesJSONArray.optJSONObject(i);
				
				if (chargerTypeJSONObj != null) {
					chargerType.setID(chargerTypeJSONObj.optInt("ID", -1));
					chargerType.setIsFastChargeCapable(chargerTypeJSONObj.optBoolean("IsFastChargeCapable", false));
					chargerType.setTitle(chargerTypeJSONObj.optString("Title", ""));
					this.mChargerTypesMap.put(chargerType.getTitle(), chargerType);
				}
			}			
		}
	}

	private void populateCountries(JSONArray countriesJSONArray) {
		Country country;
		JSONObject countryJSONObj;

		if (countriesJSONArray != null) {
			this.mCountriesMap.clear();

			for (int i=0; i<countriesJSONArray.length(); i++) {	
				country = new Country();
				countryJSONObj = countriesJSONArray.optJSONObject(i);
				
				if (countryJSONObj != null) {
					country.setID(countryJSONObj.optInt("ID", -1));
					country.setISOCode(countryJSONObj.optString("ISOCode", ""));
					country.setTitle(countryJSONObj.optString("Title", ""));
					this.mCountriesMap.put(country.getTitle(), country);
				}
			}		
		} 
	}

	private void populateConnectionTypes(JSONArray connectionTypesJSONArray) {
		ConnectionType connectionType;
		JSONObject connectionTypeJSONObj;

		if (connectionTypesJSONArray != null) {
			this.mConnectionTypesMap.clear();

			for (int i=0; i<connectionTypesJSONArray.length(); i++) {	
				connectionType = new ConnectionType();
				connectionTypeJSONObj = connectionTypesJSONArray.optJSONObject(i);
				
				if (connectionTypeJSONObj != null) {
					connectionType.setID(connectionTypeJSONObj.optInt("ID", -1));
					connectionType.setTitle(connectionTypeJSONObj.optString("Title", ""));
					connectionType.setFormalName(connectionTypeJSONObj.optString("FormalName", ""));
					this.mConnectionTypesMap.put(connectionType.getTitle(), connectionType);
				}
			}			
		} 
	}

	private void populateDataProviders(JSONArray dataProvidersJSONArray) {
		DataProvider dataProvider;
		JSONObject dataProviderJSONObj;

		if (dataProvidersJSONArray != null) {
			this.mDataProvidersMap.clear();

			for (int i=0; i<dataProvidersJSONArray.length(); i++) {	
				dataProvider = new DataProvider();
				dataProviderJSONObj = dataProvidersJSONArray.optJSONObject(i);
				
				if (dataProviderJSONObj != null) {
					dataProvider.setID(dataProviderJSONObj.optInt("ID", -1));
					dataProvider.setTitle(dataProviderJSONObj.optString("Title", ""));
					dataProvider.setWebsiteURL(dataProviderJSONObj.optString("WebsiteURL", ""));
					this.mDataProvidersMap.put(dataProvider.getTitle(), dataProvider);
				}
			}
		} 
	}

	private void populateOperators(JSONArray operatorsJSONArray) {
		OperatorInfo operatorInfo;
		JSONObject operatorInfoJSONObj;

		if (operatorsJSONArray != null) {
			this.mOperatorsMap.clear();

			for (int i=0; i<operatorsJSONArray.length(); i++) {	
				operatorInfo = new OperatorInfo();
				operatorInfoJSONObj = operatorsJSONArray.optJSONObject(i);
				
				if (operatorInfoJSONObj != null) {
					operatorInfo.setID(operatorInfoJSONObj.optInt("ID", -1));
					operatorInfo.setTitle(operatorInfoJSONObj.optString("Title", ""));
					this.mOperatorsMap.put(operatorInfo.getTitle(), operatorInfo);
				}
			}
		} 
	}

	private void populateUsageTypes(JSONArray usageTypeJSONArray) {
		UsageType usageType;
		JSONObject usageTypeJSONObj;

		if (usageTypeJSONArray != null) {
			this.mUsageTypesMap.clear();

			for (int i=0; i<usageTypeJSONArray.length(); i++) {	
				usageType = new UsageType();
				usageTypeJSONObj = usageTypeJSONArray.optJSONObject(i);
				
				if (usageTypeJSONObj != null) {
					usageType.setID(usageTypeJSONObj.optInt("ID", -1));
					usageType.setTitle(usageTypeJSONObj.optString("Title", ""));
					this.mUsageTypesMap.put(usageType.getTitle(), usageType);
				}
			}
		}
	}

	private void populateStatusTypes(JSONArray statusTypesJSONArray) {
		StatusType statusType;
		JSONObject statusTypeJSONObj;

		if (statusTypesJSONArray != null) {
			this.mStatusTypesMap.clear();

			for (int i=0; i<statusTypesJSONArray.length(); i++) {	
				statusType = new StatusType();
				statusTypeJSONObj = statusTypesJSONArray.optJSONObject(i);
				
				if (statusTypeJSONObj != null) {
					statusType.setID(statusTypeJSONObj.optInt("ID", -1));
					statusType.setIsOperational(statusTypeJSONObj.optString("IsOperational", ""));
					statusType.setTitle(statusTypeJSONObj.optString("Title", ""));
					this.mStatusTypesMap.put(statusType.getTitle(), statusType);
				}
			}
		} 
	}

	private void populateUserCommentTypes(JSONArray userCommentTypesJSONArray) {
		UserCommentType userCommentType;
		JSONObject userCommentTypeJSONObj;

		if (userCommentTypesJSONArray != null) {
			this.mUserCommentTypesMap.clear();

			for (int i=0; i<userCommentTypesJSONArray.length(); i++) {	
				userCommentType = new UserCommentType();
				userCommentTypeJSONObj = userCommentTypesJSONArray.optJSONObject(i);
				
				if (userCommentTypeJSONObj != null) {
					userCommentType.setID(userCommentTypeJSONObj.optInt("ID", -1));
					userCommentType.setTitle(userCommentTypeJSONObj.optString("Title", ""));
					this.mUserCommentTypesMap.put(userCommentType.getTitle(), userCommentType);
				}
			}
		} 
	}

	private void populateSudmissionStatusTypes(JSONArray userSubmissionStatusTypesJSONArray) {
		SubmissionStatus submissionStatusType;
		JSONObject submissionStatusJSONObj;

		if (userSubmissionStatusTypesJSONArray != null) {
			this.mSubmissionStatusTypesMap.clear();

			for (int i=0; i<userSubmissionStatusTypesJSONArray.length(); i++) {	
				submissionStatusType = new SubmissionStatus();
				submissionStatusJSONObj = userSubmissionStatusTypesJSONArray.optJSONObject(i);
				
				if (submissionStatusJSONObj != null) {
					submissionStatusType.setID(submissionStatusJSONObj.optInt("ID", -1));
					submissionStatusType.setIsLive(submissionStatusJSONObj.optBoolean("IsLive", false));
					submissionStatusType.setTitle(submissionStatusJSONObj.optString("Title", ""));
					this.mSubmissionStatusTypesMap.put(submissionStatusType.getTitle(), submissionStatusType);
				}
			}
		} 
	}

	public int getOperatorId(String operatorTitle) {
		OperatorInfo operatorInfo = this.mOperatorsMap.get(operatorTitle);
		return operatorInfo != null ? operatorInfo.getID() : -1;
	}

	public int getCountryId(String countryTitle) {
		Country country = this.mCountriesMap.get(countryTitle);
		return country != null ? country.getID() : -1;
	}

	public String getCountryISOCode(String countryTitle) {
		Country country = this.mCountriesMap.get(countryTitle);
		return country != null ? country.getISOCode() : "";
	}

	public int getConnectionTypeId(String connectionTypeTitle) {
		ConnectionType connectionType = this.mConnectionTypesMap.get(connectionTypeTitle);
		return connectionType != null ? connectionType.getID() : -1;
	}

	public int getChargerTypeId(String chargerTypeTitle) {
		ChargerType chargerType = this.mChargerTypesMap.get(chargerTypeTitle);
		return chargerType != null ? chargerType.getID() : -1;		
	}

	public int getUsageTypeId(String usageTypeTitle) {
		UsageType usageType = this.mUsageTypesMap.get(usageTypeTitle);
		return usageType != null ? usageType.getID() : -1;		
	}

	public int getStatusTypeId(String statusTypeTitle) {
		StatusType statusType = this.mStatusTypesMap.get(statusTypeTitle);
		return statusType != null ? statusType.getID() : -1;		
	}

	public Map<String, ChargerType> getChargerTypes() {
		return this.mChargerTypesMap;
	}

	public Map<String, Country> getCountries() {
		return this.mCountriesMap;
	}

	public Map<String, ConnectionType> getConnectionTypes() {
		return this.mConnectionTypesMap;
	}

	public Map<String, DataProvider> getDataProviders() {
		return this.mDataProvidersMap;
	}

	public Map<String, OperatorInfo> getOperators() {
		return this.mOperatorsMap;
	}

	public Map<String, UsageType> getUsageTypes() {
		return this.mUsageTypesMap;
	}
	
	public Map<String, UserCommentType> getUserCommentTypes() {
		return this.mUserCommentTypesMap;
	}
	
	public Map<String, StatusType> getUStatusTypes() {
		return this.mStatusTypesMap;
	}
	
	public Map<String, SubmissionStatus> getSubmissionStatusTypes() {
		return this.mSubmissionStatusTypesMap;
	}

}
