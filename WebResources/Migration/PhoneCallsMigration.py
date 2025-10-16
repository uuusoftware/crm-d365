

token_url = f"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token"

files = dbutils.fs.ls(
    "abfss://bronze@stcrmdevprodcm01.dfs.core.windows.net/incoming/mapping/phone"
)
csv_files = [f.path for f in files if f.path.endswith('.csv')]

def extract_timestamp(path):
    match = re.search(r'(\d{8,})', path)
    return int(match.group(1)) if match else 0

csv_files_sorted = sorted(csv_files, key=extract_timestamp)

for file_path in csv_files_sorted:
    file_name = os.path.basename(file_path)
    df = spark.read.format("csv").option("header", "true").option("multiLine", "true") \
        .option("escape", "\"").load(file_path)
    if "Phone Call ID" in df.columns:
        df = df.drop("Phone Call ID")
    if "Due" in df.columns:
        df = df.drop("Due")

    
    rename_map = {
        "Subject": "subject",
        "Call From": "from",
        "Call To": "to",
        "Phone Number": "phonenumber",
        "Direction": "directioncode",
        "Duration": "actualdurationminutes",
        "Regarding": "regardingobjectid",
        "Description": "description",
        #"Due": "actualdurationminutes",
        "Priority": "prioritycode",
        "Status": "statuscode",
        "Owner": "ownerid",
        "Created On": "createdon",
        "Object ID": "cm_objectid"
        
    }
    rename_map = {k: v for k, v in rename_map.items() if k}
    df = df.select([col(c).alias(rename_map.get(c, c)) for c in df.columns])

    #display(df)

    token_data = {
        "grant_type": "client_credentials",
        "client_id": client_id,
        "client_secret": client_secret,
        "scope": f"{resource}/.default"
    }

    token_response = requests.post(token_url, data=token_data)
    access_token = token_response.json()["access_token"]

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Accept": "application/json",
        "Content-Type": "application/json"
    }

    #df = df.select("from", "to","subject", "description")

    error_df = df.filter((col("regardingobjectid").isNull()) | (col("regardingobjectid") == "") | (col("cm_objectid") == "") | (col("cm_objectid").isNull()))
    error_df = error_df.select("cm_objectid") 
    error_df = error_df.withColumn("Message", lit("regardingobjectid or cm_objectid is blank from source"))
    error_df = error_df.withColumn("cm_objectid_Timestamp", concat(col("cm_objectid"), lit("_"), lit(current_timestamp_str)))
    error_df = error_df.withColumn("Timestamp", lit(current_timestamp_str))
    df_filtered = df.filter(~((col("regardingobjectid").isNull()) | (col("regardingobjectid") == "") | (col("cm_objectid") == "") | (col("cm_objectid").isNull())))

   

    

    #statuscode
    odata_url_statuscode = f"{resource}/api/data/v9.2/EntityDefinitions(LogicalName='phonecall')/Attributes(LogicalName='statuscode')/Microsoft.Dynamics.CRM.StatusAttributeMetadata/OptionSet?$select=Options"
    response_statuscode = requests.get(odata_url_statuscode, headers=headers)
    response_statuscode_json = response_statuscode.json()
    options_list_statuscode = response_statuscode_json.get('Options', [])
    #print(options_list_statuscode)
    extracted_data_statuscode = [{'Label': option['Label']['UserLocalizedLabel']['Label'], 'Value': option['Value']} for option in options_list_statuscode]    
    df_extracted_statuscode = spark.createDataFrame(extracted_data_statuscode)   
    df_extracted_statuscode.createOrReplaceTempView("statuscode_lookup")
    df_filtered1 = df_filtered.join(
    df_extracted_statuscode,
    df.statuscode == df_extracted_statuscode.Label,
    "left"
    ).withColumn(
    "statuscode",
    when(
        col("Value").isNotNull(),
        col("Value")
        ).otherwise(col("statuscode"))
    ).drop("Label", "Value")

    ##priortycode    
    odata_url_prioritycode = f"{resource}/api/data/v9.2/EntityDefinitions(LogicalName='phonecall')/Attributes(LogicalName='prioritycode')/Microsoft.Dynamics.CRM.PicklistAttributeMetadata/OptionSet?$select=Options"    
    response_prioritycode = requests.get(odata_url_prioritycode, headers=headers)
    response_prioritycode_json = response_prioritycode.json()
    options_list_prioritycode = response_prioritycode_json.get('Options', [])
    extracted_data_prioritycode = [{'Label': option['Label']['UserLocalizedLabel']['Label'], 'Value': option['Value']} for option in options_list_prioritycode]
    df_extracted_prioritycode = spark.createDataFrame(extracted_data_prioritycode)   
    df_extracted_prioritycode.createOrReplaceTempView("prioritycode_lookup")
    df_filtered2 = df_filtered1.join(
    df_extracted_prioritycode,
    df.prioritycode == df_extracted_prioritycode.Label,
     "left"
     ).withColumn(
     "prioritycode",
      when(
        col("Value").isNotNull(),
         col("Value")
         ).otherwise(col("prioritycode"))
     ).drop("Label", "Value")
    
    new_error_list = []
    display(df_filtered2)

    for row in df_filtered2.collect():
        flag = 0
        row_json = row.asDict()
        #intilize to_payload for email binding        
                                        
        cm_objectid = row_json["cm_objectid"]     
        odata_url_phone_check = f"{resource}/api/data/v9.2.25074.190/phonecalls?$filter=cm_objectid eq '{cm_objectid}'&$select=activityid,statuscode"
        response_phone_check = requests.get(odata_url_phone_check, headers=headers)
        phone_data = response_phone_check.json()
        

        if not phone_data.get("value") or response_phone_check.status_code not in [200, 201, 204]:
            #statuscode = row_json.get("statuscode")
            directioncode = True
            row_json["directioncode"] = directioncode
            
            statuscode1 = '1'
            statuscode = statuscode1
            
            original_statuscode = row_json.get("statuscode")
            

            row_json["statuscode"] = statuscode1

            to_payload = {
                        "subject": row_json["subject"],
                        "phonenumber": row_json["phonenumber"],
                        "phonecall_activity_parties": []
                                    } 
            
        
            #create process
            #to
            to = row_json["to"]
            if to is not None:    
                to_addresses = to.split(',') if to else []
                for to_address in to_addresses:                   
                    odata_url_systemuser_to = (
                    f"{resource}/api/data/v9.2.25074.190/systemusers"
                    f"?$filter= eq internalemailaddress '{to_address}'&$select=systemuserid"
                    )
                    response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)
                    to_data = response_systemuser_to.json()
                    if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]:
                        odata_url_systemuser_to = (
                        f"{resource}/api/data/v9.2.25074.190/contacts"
                        f"?$filter=cm_contactid eq '{to_address}'&$select=contactid"
                        )
                        response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)                       
                        to_data = response_systemuser_to.json()                        
                        if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]:
                            odata_url_systemuser_to = (
                            f"{resource}/api/data/v9.2.25074.190/accounts"
                            f"?$filter=accountnumber eq '{to_address}'&$select=accountid"
                            )
                            response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)
                            to_data = response_systemuser_to.json()                        
                            if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]:
                                odata_url_systemuser_to = (
                                    f"{resource}/api/data/v9.2.25074.190/leads"
                                    f"?$filter=cm_leadid eq '{to_address}'&$select=leadid"
                                )
                                response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)
                                to_data = response_systemuser_to.json()
                                if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]: 
                                  
                                    
                                    to_payload["phonecall_activity_parties"].append({
                                                "addressused": to_address,
                                                "participationtypemask": 2,  # 2 is for "To" recipients
                                                    })                                                                     
                                                                  
                                else:
                                    if to_data.get("@odata.count") == 1:                             
                                        to_guid = to_data["value"][0]["leadid"]
                                        to_payload["phonecall_activity_parties"].append({
                                        "partyid_lead@odata.bind": f"/leads({to_guid})",
                                        "participationtypemask": 2,  # 2 is for "To" recipients
                                        })  
                                    else:
                                        to_payload["email_activity_parties"].append({
                                            "addressused": to_address,
                                            "participationtypemask": 2,  # 2 is for "To" recipients
                                            })
                                                                      
                            else:
                                if to_data.get("@odata.count") == 1:                             
                                    to_guid = to_data["value"][0]["accountid"]
                                    to_payload["phonecall_activity_parties"].append({
                                        "partyid_account@odata.bind": f"/accounts({to_guid})",
                                        "participationtypemask": 2,  # 2 is for "To" recipients
                                        })
                                else:
                                    to_payload["email_activity_parties"].append({
                                        "addressused": to_address,
                                        "participationtypemask": 2,  # 2 is for "To" recipients
                                        })
                                
                        else:
                            if to_data.get("@odata.count") == 1:                             
                                to_guid = to_data["value"][0]["contactid"]                        
                                to_payload["phonecall_activity_parties"].append({
                                    "partyid_contact@odata.bind": f"/contacts({to_guid})",
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                })   
                            else:
                                to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })                                
                                                     
                    else:
                        if to_data.get("@odata.count") == 1:                             
                            to_guid = to_data["value"][0]["systemuserid"]                           
                            to_payload["phonecall_activity_parties"].append({
                                    "partyid_systemuser@odata.bind": f"/systemusers({to_guid})",
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                })  
                        else:
                             to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })    
                                             
            else:
                print("to is blank")
                flag = 1
                new_error_list.append(Row(
                    cm_objectid=row["cm_objectid"],
                    Message='to is blank',
                    cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                    Timestamp=current_timestamp_str
                ))

            #from
            from1 = row_json["from"]
            if from1 is not None:
                odata_url_systemuser_from1 = (
                f"{resource}/api/data/v9.2.25074.190/systemusers"
                f"?$filter=internalemailaddress eq '{from1}'&$select=systemuserid"
                )
                response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                from1_data = response_systemuser_from1.json()
                if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]:
                    odata_url_systemuser_from1 = (
                    f"{resource}/api/data/v9.2.25074.190/contacts"
                    f"?$filter=cm_contactid eq '{from1}'&$select=contactid"
                    )
                    response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                    from1_data = response_systemuser_from1.json()
                    
                    if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]:
                        odata_url_systemuser_from1 = (
                        f"{resource}/api/data/v9.2.25074.190/accounts"
                        f"?$filter=accountnumber eq '{from1}'&$select=accountid"
                            )
                        response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                        from1_data = response_systemuser_from1.json()
                        if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]:
                            odata_url_systemuser_from1 = (
                            f"{resource}/api/data/v9.2.25074.190/leads"
                            f"?$filter=cm_leadid eq '{from1}'&$select=leadid"
                            )
                            response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                            from1_data = response_systemuser_from1.json()
                            if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]: 
                                to_payload["phonecall_activity_parties"].append({
                                             "addressused": from1,
                                            "participationtypemask": 1,  # 2 is for "To" recipients
                                                    })      
                            else:
                                if to_data.get("@odata.count") == 1:                             
                                    from1_guid = from1_data["value"][0]["leadid"]
                                    to_payload["phonecall_activity_parties"].append({
                                        "partyid_lead@odata.bind": f"/leads({from1_guid})",
                                        "participationtypemask": 1,  # 2 is for "To" recipients
                                            })
                                else:
                                    to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 1,  # 2 is for "To" recipients
                                    })  
                                
                        else:
                            if to_data.get("@odata.count") == 1:                             
                                from1_guid = from1_data["value"][0]["accountid"]
                                to_payload["phonecall_activity_parties"].append({
                                    "partyid_account@odata.bind": f"/accounts({from1_guid})",
                                     "participationtypemask": 1,  # 2 is for "To" recipients
                                            })
                            else:
                                to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 1,  # 2 is for "To" recipients
                                    })    
                            
                    else:
                        if to_data.get("@odata.count") == 1:                             
                            from1_guid = from1_data["value"][0]["contactid"]
                            to_payload["phonecall_activity_parties"].append({
                                    "partyid_contact@odata.bind": f"/contacts({from1_guid})",
                                    "participationtypemask": 1,  # 2 is for "To" recipients
                                        })
                        else:
                             to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 1,  # 2 is for "To" recipients
                                    })    
                        
                else:
                    if to_data.get("@odata.count") == 1:                             
                            from1_guid = from1_data["value"][0]["systemuserid"]
                            to_payload["phonecall_activity_parties"].append({
                                    "partyid_systemuser@odata.bind": f"/systemusers({from1_guid})",
                                    "participationtypemask": 1,  # 2 is for "To" recipients
                                        })
                    else:
                             to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 1,  # 2 is for "To" recipients
                                    })    
                    
            else: 
               
                flag = 1
                new_error_list.append(Row(
                    cm_objectid=row["cm_objectid"],
                    Message='from is blank',
                    cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                    Timestamp=current_timestamp_str
                ))
                
           
            #regardinbg object
            regardingobjectid = row_json.get("regardingobjectid")
            odata_url_account_regardingobjectid = (
                            f"{resource}/api/data/v9.2.25074.190/accounts"
                            f"?$filter=accountnumber eq '{regardingobjectid}'&$select=accountid"
                        )
            response_account_regardingobjectid = requests.get(odata_url_account_regardingobjectid, headers=headers)
            if response_account_regardingobjectid.status_code not in [200, 201,204] or not response_account_regardingobjectid.json().get("value"):
                odata_url_account_regardingobjectid = (
                            f"{resource}/api/data/v9.2.25074.190/incidents"
                            f"?$filter=ticketnumber eq '{regardingobjectid}'&$select=incidentid"
                        )
                response_cases_regardingobjectid = requests.get(odata_url_account_regardingobjectid, headers=headers)
                if response_cases_regardingobjectid.status_code not in [200, 201,204] or not response_cases_regardingobjectid.json().get("value"):
                   
                    flag = 1
                    new_error_list.append(Row(
                    cm_objectid=row["cm_objectid"],
                    Message='regardingobjectid not in accounts or cases in crm',
                    cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                    Timestamp=current_timestamp_str
                    ))
                else:
                    regardingobjectid_guid = response_cases_regardingobjectid.json()["value"][0]["incidentid"]
                    row_json["regardingobjectid_incident@odata.bind"] = f"/incidents({regardingobjectid_guid})"
                    row_json.pop("regardingobjectid", None)
            else:        
                regardingobjectid_guid = response_account_regardingobjectid.json()["value"][0]["accountid"]
                row_json["regardingobjectid_account@odata.bind"] = f"/accounts({regardingobjectid_guid})"
                row_json.pop("regardingobjectid", None)

            #OwnerID
            ownerid = row_json.get("ownerid")
            if ownerid == None:
                row_json.pop("ownerid", None)
            else:
                ownerid = ownerid.lower()
                if re.match(r"[^@]+@[^@]+\.[^@]+", ownerid):                            
                    odata_url_owner = f"{resource}/api/data/v9.2.25074.190/systemusers"f"?$filter=internalemailaddress eq '{ownerid}'&$select=systemuserid"
                    response_owner = requests.get(odata_url_owner, headers=headers)                    
                    owner_data = response_owner.json()  
                    if not owner_data.get("value") or  response_owner.status_code not in [200, 201,204] :
                        odata_url_owner = f"{resource}/api/data/v9.2.25074.190/teams"f"?$filter=emailaddress eq '{ownerid}'&$select=teamid"
                        response_owner = requests.get(odata_url_owner, headers=headers)
                        owner_data = response_owner.json()
                        if not owner_data.get("value") or  response_owner.status_code not in [200, 201,204] :
                            
                            flag = 1
                            new_error_list.append(Row(
                            cm_objectid=row["cm_objectid"],
                            Message='ownerid not in systemusers or teams in crm',
                            cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                            Timestamp=current_timestamp_str
                            )) 
                        else:
                            owner_guid = owner_data["value"][0]["teamid"]
                            row_json["ownerid_team@odata.bind"] = f"/teams({owner_guid})"   
                            row_json.pop("ownerid", None)
                    else:
                        owner_guid = owner_data["value"][0]["systemuserid"]
                        row_json["ownerid@odata.bind"] = f"/systemusers({owner_guid})"
                        row_json.pop("ownerid", None) 

            #created on 
            cretedon = row_json.get("createdon")
            if cretedon:
                row_json['overriddencreatedon'] = row_json["createdon"]
                row_json.pop("createdon",None)
            else:
                row_json.pop("createdon",None) 
            #create email activity - submit 
            if to_payload:
                row_json.update(to_payload)     
                row_json.pop("from", None)
                row_json.pop("to", None)
            
            print(flag)
            if flag == 0: 
                print(row_json)
                row_json["directioncode"] = True
                odata_url_phonecallcreate = f"{resource}/api/data/v9.2.25074.190/phonecalls"
                response_phonecall= requests.post(odata_url_phonecallcreate, headers=headers, json=row_json)
                
                if response_phonecall.status_code not in [200,201,204]:
                    new_error_list.append(Row(
                        cm_objectid=row["cm_objectid"],
                        Message=response_phonecall.text,
                        cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                        Timestamp=current_timestamp_str))
                else:
                    if original_statuscode == '1' :
                        statecode = '0'
                        directioncode = True
                    elif original_statuscode == '2' :
                        statecode = '1'
                        directioncode = True
                    elif original_statuscode == '4':
                        statecode = '1'
                        directioncode = False
                    else:
                        statecode = '2'
                        directioncode = True
                    
                    print(row_json)
                    odata_url_phonecallcheck = f"{resource}/api/data/v9.2.25074.190/phonecalls?$filter=cm_objectid eq '{cm_objectid}'&$select=activityid"
                    response_phonecallcheck = requests.get(odata_url_phonecallcheck, headers=headers)
                    phonecalldata = response_phonecallcheck.json()  
                    
                    if response_phonecallcheck.status_code not in [200, 201, 204] or not phonecalldata.get("value"):
                        new_error_list.append(Row(
                            cm_objectid=row["cm_objectid"],
                            Message=response_phonecallcheck.text,
                            cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                            Timestamp=current_timestamp_str))
                       
                    else:
                       
                        row_json["statuscode"] = original_statuscode
                        row_json["directioncode"] = directioncode
                        row_json["statecode"] = statecode
                        
                        phonecallguid = phonecalldata["value"][0]["activityid"]
                        response_phonecallupdate = f"{resource}/api/data/v9.2.25074.190/phonecalls({phonecallguid})"
                        response_phonecallupdate = requests.patch(response_phonecallupdate, headers=headers, json=row_json)
                        
                        if response_phonecallupdate.status_code not in [200,201,204]:
                            new_error_list.append(Row(
                            cm_objectid=row["cm_objectid"],
                            Message=response_phonecallupdate.text,
                            cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                            Timestamp=current_timestamp_str))
                   
            #end create phonecallactivity - submit     
        #update phonecallactivity start
        else:
            
            statuscode = row_json.get("statuscode")
                  
            original_statuscode = row_json.get("statuscode")
            
            #row_json["statuscode"] = statuscode1

            to_payload = {
                                        "subject": row_json["subject"],
                                        "phonenumber": row_json["phonenumber"],                                                                                                       
                                        "phone_activity_parties": []
                                    }        
            
        #to 
            to = row_json["to"]
            if to is not None:        
                to_addresses = to.split(',') if to else []
                for to_address in to_addresses:
                    
                    odata_url_systemuser_to = (
                    f"{resource}/api/data/v9.2.25074.190/systemusers"
                    f"?$filter=internalemailaddress eq '{to_address}'&$select=systemuserid"
                    )
                    response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)
                    to_data = response_systemuser_to.json()
                    if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]:
                        odata_url_systemuser_to = (
                        f"{resource}/api/data/v9.2.25074.190/contacts"
                        f"?$filter=cm_contactid eq '{to_address}'&$select=contactid"
                        )
                        response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)
                        to_data = response_systemuser_to.json()
                        if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]:
                            odata_url_systemuser_to = (
                            f"{resource}/api/data/v9.2.25074.190/accounts"
                            f"?$filter=accountnumber eq '{to_address}'&$select=accountid"
                            )
                            response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)
                            to_data = response_systemuser_to.json()                        
                            if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]:
                                odata_url_systemuser_to = (
                                    f"{resource}/api/data/v9.2.25074.190/leads"
                                    f"?$filter=cm_leadid eq '{to_address}'&$select=leadid"
                                )
                                response_systemuser_to = requests.get(odata_url_systemuser_to, headers=headers)
                                to_data = response_systemuser_to.json()
                                if not to_data.get("value") or response_systemuser_to.status_code not in [200, 201, 204]:                                                                  
                                    to_payload["phone_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })  
                                else:
                                    if to_data.get("@odata.count") == 1:                             
                                        to_guid = to_data["value"][0]["leadid"]
                                        to_payload["phone_activity_parties"].append({
                                        "partyid_lead@odata.bind": f"/leads({to_guid})",
                                        "participationtypemask": 2,  # 2 is for "To" recipients
                                        })
                                    else:
                                        to_payload["email_activity_parties"].append({
                                        "addressused": to_address,
                                        "participationtypemask": 2,  # 2 is for "To" recipients
                                        })   
                                    
                            else:
                                if to_data.get("@odata.count") == 1:                             
                                    to_guid = to_data["value"][0]["accountid"]
                                    to_payload["phone_activity_parties"].append({
                                    "partyid_account@odata.bind": f"/accounts({to_guid})",
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                        })
                                else:
                                    to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })   
                                
                        else:
                            if to_data.get("@odata.count") == 1:                             
                                to_guid = to_data["value"][0]["contactid"]                        
                                to_payload["phone_activity_parties"].append({
                                    "partyid_contact@odata.bind": f"/contacts({to_guid})",
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                })
                            else:
                                to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })   
                            
                    else:
                        if to_data.get("@odata.count") == 1:                             
                             to_guid = to_data["value"][0]["systemuserid"]
                             to_payload["phone_activity_parties"].append({
                                    "partyid_systemuser@odata.bind": f"/systemusers({to_guid})",
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                })
                        else:
                             to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })    
                       
            else:
                flag = 1
                new_error_list.append(Row(
                    cm_objectid=row["cm_objectid"],
                    Message='to is blank',
                    cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                    Timestamp=current_timestamp_str
                ))

            #from
            from1 = row_json["from"]
            if from1 is not None:
                odata_url_systemuser_from1 = (
                f"{resource}/api/data/v9.2.25074.190/systemusers"
                f"?$filter=internalemailaddress eq '{from1}'&$select=systemuserid"
                )
                response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                from1_data = response_systemuser_from1.json()
                if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]:
                    odata_url_systemuser_from1 = (
                    f"{resource}/api/data/v9.2.25074.190/contacts"
                    f"?$filter=cm_contactid eq '{from1}'&$select=contactid"
                    )
                    response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                    from1_data = response_systemuser_from1.json()
                    if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]:
                        odata_url_systemuser_from1 = (
                        f"{resource}/api/data/v9.2.25074.190/accounts"
                        f"?$filter=accountnumber eq '{from1}'&$select=contactid"
                        )
                        response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                        from1_data = response_systemuser_from1.json()
                        
                        if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]:
                            odata_url_systemuser_from1 = (
                            f"{resource}/api/data/v9.2.25074.190/leads"
                            f"?$filter=cm_leadid eq '{from1}'&$select=leadid"
                            )
                            response_systemuser_from1 = requests.get(odata_url_systemuser_from1, headers=headers)
                            from1_data = response_systemuser_from1.json()
                            if not from1_data.get("value") or response_systemuser_from1.status_code not in [200, 201, 204]: 
                                to_payload["phone_activity_parties"].append({
                                            "addressused": from1,
                                             "participationtypemask": 1,  # 2 is for "To" recipients
                                                  })     
                            else:
                                if to_data.get("@odata.count") == 1:                             
                                    from1_guid = from1_data["value"][0]["leadid"]
                               
                                    to_payload["phone_activity_parties"].append({
                                        "partyid_lead@odata.bind": f"/leads({from1_guid})",
                                        "participationtypemask": 1,  # 2 is for "To" recipients
                                            })
                                else:
                                    to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })   
                                
                        else:
                            if to_data.get("@odata.count") == 1:                             
                                from1_guid = from1_data["value"][0]["accountid"]
                                to_payload["phone_activity_parties"].append({
                                     "partyid_account@odata.bind": f"/accounts({from1_guid})",
                                     "participationtypemask": 1,  # 2 is for "To" recipients
                                            })
                            else:
                                    to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })   
                            
                    else:
                        if to_data.get("@odata.count") == 1:                             
                              from1_guid = from1_data["value"][0]["contactid"]
                              to_payload["phone_activity_parties"].append({
                                    "partyid_contact@odata.bind": f"/contacts({from1_guid})",
                                    "participationtypemask": 1,  # 2 is for "To" recipients
                                        })
                        else:
                                to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })   
                        
                else:
                    if to_data.get("@odata.count") == 1:                             
                              from1_guid = from1_data["value"][0]["systemuserid"]
                              to_payload["phone_activity_parties"].append({
                                     "partyid_systemuser@odata.bind": f"/systemusers({from1_guid})",
                                     "participationtypemask": 1,  # 2 is for "To" recipients
                                        })
                    else:
                             to_payload["email_activity_parties"].append({
                                    "addressused": to_address,
                                    "participationtypemask": 2,  # 2 is for "To" recipients
                                    })   
                   
            else: 
                flag = 1
                new_error_list.append(Row(
                    cm_objectid=row["cm_objectid"],
                    Message='from is blank',
                    cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                    Timestamp=current_timestamp_str
                ))
          
                   
        

            #regardinbg object
            regardingobjectid = row_json.get("regardingobjectid")
            odata_url_account_regardingobjectid = (
                            f"{resource}/api/data/v9.2.25074.190/accounts"
                            f"?$filter=accountnumber eq '{regardingobjectid}'&$select=accountid"
                        )
            response_account_regardingobjectid = requests.get(odata_url_account_regardingobjectid, headers=headers)
            if response_account_regardingobjectid.status_code not in [200, 201,204] or not response_account_regardingobjectid.json().get("value"):
                odata_url_account_regardingobjectid = (
                            f"{resource}/api/data/v9.2.25074.190/incidents"
                            f"?$filter=ticketnumber eq '{regardingobjectid}'&$select=incidentid"
                        )
                response_cases_regardingobjectid = requests.get(odata_url_account_regardingobjectid, headers=headers)
                if response_cases_regardingobjectid.status_code not in [200, 201,204] or not response_cases_regardingobjectid.json().get("value"):
                    flag = 1
                    new_error_list.append(Row(
                    cm_objectid=row["cm_objectid"],
                    Message='regardingobjectid not in accounts or cases in crm',
                    cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                    Timestamp=current_timestamp_str
                    ))
                else:
                    regardingobjectid_guid = response_cases_regardingobjectid.json()["value"][0]["incidentid"]
                    row_json["regardingobjectid_incident@odata.bind"] = f"/incidents({regardingobjectid_guid})"
                    row_json.pop("regardingobjectid", None)
            else:        
                regardingobjectid_guid = response_account_regardingobjectid.json()["value"][0]["accountid"]
                row_json["regardingobjectid_account@odata.bind"] = f"/accounts({regardingobjectid_guid})"
                row_json.pop("regardingobjectid", None)

            #OwnerID
            ownerid = row_json.get("ownerid")
            if ownerid == None:
                row_json.pop("ownerid", None)
            else:
                ownerid = ownerid.lower()
                if re.match(r"[^@]+@[^@]+\.[^@]+", ownerid):                            
                    odata_url_owner = f"{resource}/api/data/v9.2.25074.190/systemusers"f"?$filter=internalemailaddress eq '{ownerid}'&$select=systemuserid"
                    response_owner = requests.get(odata_url_owner, headers=headers)                    
                    owner_data = response_owner.json()                         
                           
                    if not owner_data.get("value") or  response_owner.status_code not in [200, 201,204] :
                        odata_url_owner = f"{resource}/api/data/v9.2.25074.190/teams"f"?$filter=emailaddress eq '{ownerid}'&$select=teamid"
                        response_owner = requests.get(odata_url_owner, headers=headers)
                        owner_data = response_owner.json()
                        if not owner_data.get("value") or  response_owner.status_code not in [200, 201,204] :
                            flag = 1
                            new_error_list.append(Row(
                            cm_objectid=row["cm_objectid"],
                            Message='ownerid not in systemusers or teams in crm',
                            cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                            Timestamp=current_timestamp_str
                            ))  
                        else:
                            owner_guid = owner_data["value"][0]["teamid"]
                            row_json["ownerid_team@odata.bind"] = f"/teams({owner_guid})"   
                            row_json.pop("ownerid", None)
                    else:
                        owner_guid = owner_data["value"][0]["systemuserid"]
                        row_json["ownerid_systemuser@odata.bind"] = f"/systemusers({owner_guid})"
                        row_json.pop("ownerid", None)

            #created on 
            cretedon = row_json.get("createdon")
            if cretedon:
                row_json['overriddencreatedon'] = row_json["createdon"]
                row_json.pop("createdon",None)
            else:
                row_json.pop("createdon",None)
                            
            #update email activity - end code to submit
            if to_payload:
                row_json.update(to_payload) 
                row_json.pop("from", None)
                row_json.pop("to", None)
           
           

                  
            if flag == 0 and to_payload.get("phone_activity_parties"):   
               
                statuscode_crm =  phone_data["value"][0]["statuscode"]          
                phone_guid = phone_data["value"][0]["activityid"]
                
                if original_statuscode == '1' :
                    statecode = '0'
                    directioncode = True
                elif original_statuscode == '2' :
                    statecode = '1'
                    directioncode = True
                elif original_statuscode == '4':
                    statecode = '1'
                    directioncode = False
                else:
                    statecode = '2'
                    directioncode = True
                row_json["statecode"] = statecode
                row_json["directioncode"] = directioncode
                row_json["statuscode"] = original_statuscode
                 
               
                
                if statuscode_crm == '1':                  
                    response_phone_update = f"{resource}/api/data/v9.2.25074.190/phonecalls({phone_guid})"
                    response_phone_update = requests.patch(response_phone_update, headers=headers, json=row_json)                
                
                    if response_phone_update.status_code not in [200,201,204]:
                        new_error_list.append(Row(
                            cm_objectid=row["cm_objectid"],
                            Message=response_phone_update.text,
                            cm_objectid_Timestamp=f"{row['cm_objectid']}_{current_timestamp_str}",
                            Timestamp=current_timestamp_str
                                ))
                


    df = df.withColumn("file_name", lit(file_name))
    df = df.withColumn("target", lit(resource))
   
    if new_error_list:
        #print(new_error_list)
        new_error_df = spark.createDataFrame(new_error_list)
        error_df = error_df.union(new_error_df)
        error_df = error_df.withColumnRenamed("cm_objectid", "cm_objectid1")
    display(df)
    display(error_df)

    if error_df.count() > 0 and df.count() > 0:
        df_log = df.join(error_df, df.cm_objectid == error_df.cm_objectid1, how="left_outer")
    else: 
        df_log = df.withColumn("cm_objectid1", lit(" "))       
        df_log = df_log.withColumn("Message", lit(" "))
        df_log = df_log.withColumn("cm_objecid_Timestamp", lit(" "))
        df_log = df_log.withColumn("Timestamp", lit(" "))
    


    
    if df_log.count() > 0:
        df_log = df_log.drop("cm_objectid1")
        display(df_log)



       
       