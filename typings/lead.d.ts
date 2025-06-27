/// <reference path="../node_modules/@types/xrm/index.d.ts" />
declare namespace LeadEnum {
    const enum budgetstatus {
        NoCommittedBudget = 0,
        MayBuy = 1,
        CanBuy = 2,
        WillBuy = 3,
    }

    const enum address2_addresstypecode {
        DefaultValue = 1,
    }

    const enum cm_leadtype {
        Producer = 121540000,
        ServiceProvider = 121540001,
        Resident = 121540002,
        Other = 121540003,
    }

    const enum leadsourcecode {
        AnonymousReferral = 1,
        Campaign = 2,
        Crosspect = 3,
        MoeReferral = 4,
        ProgramReferral = 5,
        ProspectResearch = 6,
        Restructuring = 7,
        SelfIdentified = 8,
    }

    const enum address1_shippingmethodcode {
        DefaultValue = 1,
    }

    const enum msdyn_leadgrade {
        GradeA = 0,
        GradeB = 1,
        GradeC = 2,
        GradeD = 3,
    }

    const enum leadqualitycode {
        Hot = 1,
        Warm = 2,
        Cold = 3,
    }

    const enum address1_addresstypecode {
        DefaultValue = 1,
    }

    const enum address2_shippingmethodcode {
        DefaultValue = 1,
    }

    const enum initialcommunication {
        Contacted = 0,
        NotContacted = 1,
    }

    const enum need {
        MustHave = 0,
        ShouldHave = 1,
        GoodToHave = 2,
        NoNeed = 3,
    }

    const enum msdyn_leadscoretrend {
        Improving = 0,
        Steady = 1,
        Declining = 2,
        NotEnoughInfo = 3,
    }

    const enum preferredcontactmethodcode {
        Any = 1,
        Email = 2,
        Phone = 3,
        Fax = 4,
        Mail = 5,
    }

    const enum prioritycode {
        DefaultValue = 1,
    }

    const enum purchasetimeframe {
        Immediate = 0,
        ThisQuarter = 1,
        NextQuarter = 2,
        ThisYear = 3,
        Unknown = 4,
    }

    const enum salesstage {
        Qualify = 0,
    }

    const enum cm_servicetype {
        Contractor = 121540000,
        CollectionSite = 121540001,
        ReceivingFacility = 121540002,
        ProcessingFacility = 121540003,
        EndMarketSite = 121540004,
        BrokerForMaterialMarketing = 121540005,
    }

    const enum purchaseprocess {
        Individual = 0,
        Committee = 1,
        Unknown = 2,
    }

    const enum cm_othertype {
        GovernmentPartner = 121540000,
        FederalGovernment = 121540001,
        InnovatorLead = 121540002,
        NationalAssociation = 121540003,
        CmPartners = 121540004,
        CmBoard = 121540005,
        ProgramClients = 121540006,
        Media = 121540007,
        Regulator = 121540008,
        NgoNfp = 121540009,
        GlobalOrganizations = 121540010,
    }

    const enum statuscode {
        New = 1,
        Contacted = 2,
        Qualified = 3,
        Lost = 4,
        CannotContact = 5,
        NoLongerInterested = 6,
        Canceled = 7,
    }

    const enum salesstagecode {
        DefaultValue = 1,
    }

    const enum statecode {
        Open = 0,
        Qualified = 1,
        Disqualified = 2,
    }

    const enum msdyn_salesassignmentresult {
        Succeeded = 0,
        Failed = 1,
    }

    const enum industrycode {
        Accounting = 1,
        AgricultureAndNonPetrolNaturalResourceExtraction = 2,
        BroadcastingPrintingAndPublishing = 3,
        Brokers = 4,
        BuildingSupplyRetail = 5,
        BusinessServices = 6,
        Consulting = 7,
        ConsumerServices = 8,
        DesignDirectionAndCreativeManagement = 9,
        DistributorsDispatchersAndProcessors = 10,
        DoctorSOfficesAndClinics = 11,
        DurableManufacturing = 12,
        EatingAndDrinkingPlaces = 13,
        EntertainmentRetail = 14,
        EquipmentRentalAndLeasing = 15,
        Financial = 16,
        FoodAndTobaccoProcessing = 17,
        InboundCapitalIntensiveProcessing = 18,
        InboundRepairAndServices = 19,
        Insurance = 20,
        LegalServices = 21,
        NonDurableMerchandiseRetail = 22,
        OutboundConsumerService = 23,
        PetrochemicalExtractionAndDistribution = 24,
        ServiceRetail = 25,
        SigAffiliations = 26,
        SocialServices = 27,
        SpecialOutboundTradeContractors = 28,
        SpecialtyRealty = 29,
        Transportation = 30,
        UtilityCreationAndDistribution = 31,
        VehicleRetail = 32,
        Wholesale = 33,
    }

}

declare namespace Lead {
    const EntityLogicalName: "lead";

    const enum Attributes {
        accountid = "accountid",
        address1_addresstypecode = "address1_addresstypecode",
        address1_city = "address1_city",
        address1_composite = "address1_composite",
        address1_country = "address1_country",
        address1_county = "address1_county",
        address1_fax = "address1_fax",
        address1_latitude = "address1_latitude",
        address1_line1 = "address1_line1",
        address1_line2 = "address1_line2",
        address1_line3 = "address1_line3",
        address1_longitude = "address1_longitude",
        address1_name = "address1_name",
        address1_postalcode = "address1_postalcode",
        address1_postofficebox = "address1_postofficebox",
        address1_shippingmethodcode = "address1_shippingmethodcode",
        address1_stateorprovince = "address1_stateorprovince",
        address1_telephone1 = "address1_telephone1",
        address1_telephone2 = "address1_telephone2",
        address1_telephone3 = "address1_telephone3",
        address1_upszone = "address1_upszone",
        address1_utcoffset = "address1_utcoffset",
        address2_addresstypecode = "address2_addresstypecode",
        address2_city = "address2_city",
        address2_composite = "address2_composite",
        address2_country = "address2_country",
        address2_county = "address2_county",
        address2_fax = "address2_fax",
        address2_latitude = "address2_latitude",
        address2_line1 = "address2_line1",
        address2_line2 = "address2_line2",
        address2_line3 = "address2_line3",
        address2_longitude = "address2_longitude",
        address2_name = "address2_name",
        address2_postalcode = "address2_postalcode",
        address2_postofficebox = "address2_postofficebox",
        address2_shippingmethodcode = "address2_shippingmethodcode",
        address2_stateorprovince = "address2_stateorprovince",
        address2_telephone1 = "address2_telephone1",
        address2_telephone2 = "address2_telephone2",
        address2_telephone3 = "address2_telephone3",
        address2_upszone = "address2_upszone",
        address2_utcoffset = "address2_utcoffset",
        budgetamount = "budgetamount",
        budgetstatus = "budgetstatus",
        campaignid = "campaignid",
        cm_industry = "cm_industry",
        cm_industryname = "cm_industryname",
        cm_leadid = "cm_leadid",
        cm_leadtype = "cm_leadtype",
        cm_subindustry = "cm_subindustry",
        cm_subindustryname = "cm_subindustryname",
        companyname = "companyname",
        confirminterest = "confirminterest",
        contactid = "contactid",
        createdby = "createdby",
        createdon = "createdon",
        createdonbehalfby = "createdonbehalfby",
        customerid = "customerid",
        decisionmaker = "decisionmaker",
        description = "description",
        donotbulkemail = "donotbulkemail",
        donotemail = "donotemail",
        donotfax = "donotfax",
        donotphone = "donotphone",
        donotpostalmail = "donotpostalmail",
        donotsendmm = "donotsendmm",
        emailaddress1 = "emailaddress1",
        emailaddress2 = "emailaddress2",
        emailaddress3 = "emailaddress3",
        estimatedamount = "estimatedamount",
        estimatedclosedate = "estimatedclosedate",
        estimatedvalue = "estimatedvalue",
        evaluatefit = "evaluatefit",
        exchangerate = "exchangerate",
        fax = "fax",
        firstname = "firstname",
        followemail = "followemail",
        fullname = "fullname",
        importsequencenumber = "importsequencenumber",
        industrycode = "industrycode",
        initialcommunication = "initialcommunication",
        isautocreate = "isautocreate",
        jobtitle = "jobtitle",
        lastname = "lastname",
        lastonholdtime = "lastonholdtime",
        lastusedincampaign = "lastusedincampaign",
        leadid = "leadid",
        leadqualitycode = "leadqualitycode",
        leadsourcecode = "leadsourcecode",
        masterid = "masterid",
        merged = "merged",
        middlename = "middlename",
        mobilephone = "mobilephone",
        modifiedby = "modifiedby",
        modifiedon = "modifiedon",
        modifiedonbehalfby = "modifiedonbehalfby",
        msdyn_leadgrade = "msdyn_leadgrade",
        msdyn_leadkpiid = "msdyn_leadkpiid",
        msdyn_leadkpiidname = "msdyn_leadkpiidname",
        msdyn_leadscore = "msdyn_leadscore",
        msdyn_leadscoretrend = "msdyn_leadscoretrend",
        msdyn_predictivescoreid = "msdyn_predictivescoreid",
        msdyn_predictivescoreidname = "msdyn_predictivescoreidname",
        msdyn_salesassignmentresult = "msdyn_salesassignmentresult",
        msdyn_scorehistory = "msdyn_scorehistory",
        msdyn_scorereasons = "msdyn_scorereasons",
        msdyn_segmentid = "msdyn_segmentid",
        msdyn_segmentidname = "msdyn_segmentidname",
        need = "need",
        numberofemployees = "numberofemployees",
        onholdtime = "onholdtime",
        originatingcaseid = "originatingcaseid",
        overriddencreatedon = "overriddencreatedon",
        ownerid = "ownerid",
        owningbusinessunit = "owningbusinessunit",
        owningbusinessunitname = "owningbusinessunitname",
        owningteam = "owningteam",
        owninguser = "owninguser",
        pager = "pager",
        parentaccountid = "parentaccountid",
        parentcontactid = "parentcontactid",
        participatesinworkflow = "participatesinworkflow",
        preferredcontactmethodcode = "preferredcontactmethodcode",
        prioritycode = "prioritycode",
        purchaseprocess = "purchaseprocess",
        purchasetimeframe = "purchasetimeframe",
        qualificationcomments = "qualificationcomments",
        qualifyingopportunityid = "qualifyingopportunityid",
        relatedobjectid = "relatedobjectid",
        revenue = "revenue",
        salesstage = "salesstage",
        salesstagecode = "salesstagecode",
        salutation = "salutation",
        schedulefollowup_prospect = "schedulefollowup_prospect",
        schedulefollowup_qualify = "schedulefollowup_qualify",
        sic = "sic",
        slaid = "slaid",
        slainvokedid = "slainvokedid",
        statecode = "statecode",
        statuscode = "statuscode",
        subject = "subject",
        telephone1 = "telephone1",
        telephone2 = "telephone2",
        telephone3 = "telephone3",
        timespentbymeonemailandmeetings = "timespentbymeonemailandmeetings",
        timezoneruleversionnumber = "timezoneruleversionnumber",
        transactioncurrencyid = "transactioncurrencyid",
        traversedpath = "traversedpath",
        utcconversiontimezonecode = "utcconversiontimezonecode",
        websiteurl = "websiteurl",
        yomicompanyname = "yomicompanyname",
        yomifirstname = "yomifirstname",
        yomifullname = "yomifullname",
        yomilastname = "yomilastname",
        yomimiddlename = "yomimiddlename",
    }

}

declare namespace Xrm {
    type Lead = Omit<FormContext, 'getAttribute'> & Omit<FormContext, 'getControl'> & LeadAttributes;

    interface EventContext {
        getFormContext(): Lead;
    }

    interface LeadAttributes {
        getAttribute(name: "accountid"): Attributes.LookupAttribute;
        getAttribute(name: "address1_addresstypecode"): Attributes.OptionSetAttribute;
        getAttribute(name: "address1_city"): Attributes.StringAttribute;
        getAttribute(name: "address1_composite"): Attributes.StringAttribute;
        getAttribute(name: "address1_country"): Attributes.StringAttribute;
        getAttribute(name: "address1_county"): Attributes.StringAttribute;
        getAttribute(name: "address1_fax"): Attributes.StringAttribute;
        getAttribute(name: "address1_latitude"): Attributes.NumberAttribute;
        getAttribute(name: "address1_line1"): Attributes.StringAttribute;
        getAttribute(name: "address1_line2"): Attributes.StringAttribute;
        getAttribute(name: "address1_line3"): Attributes.StringAttribute;
        getAttribute(name: "address1_longitude"): Attributes.NumberAttribute;
        getAttribute(name: "address1_name"): Attributes.StringAttribute;
        getAttribute(name: "address1_postalcode"): Attributes.StringAttribute;
        getAttribute(name: "address1_postofficebox"): Attributes.StringAttribute;
        getAttribute(name: "address1_shippingmethodcode"): Attributes.OptionSetAttribute;
        getAttribute(name: "address1_stateorprovince"): Attributes.StringAttribute;
        getAttribute(name: "address1_telephone1"): Attributes.StringAttribute;
        getAttribute(name: "address1_telephone2"): Attributes.StringAttribute;
        getAttribute(name: "address1_telephone3"): Attributes.StringAttribute;
        getAttribute(name: "address1_upszone"): Attributes.StringAttribute;
        getAttribute(name: "address1_utcoffset"): Attributes.NumberAttribute;
        getAttribute(name: "address2_addresstypecode"): Attributes.OptionSetAttribute;
        getAttribute(name: "address2_city"): Attributes.StringAttribute;
        getAttribute(name: "address2_composite"): Attributes.StringAttribute;
        getAttribute(name: "address2_country"): Attributes.StringAttribute;
        getAttribute(name: "address2_county"): Attributes.StringAttribute;
        getAttribute(name: "address2_fax"): Attributes.StringAttribute;
        getAttribute(name: "address2_latitude"): Attributes.NumberAttribute;
        getAttribute(name: "address2_line1"): Attributes.StringAttribute;
        getAttribute(name: "address2_line2"): Attributes.StringAttribute;
        getAttribute(name: "address2_line3"): Attributes.StringAttribute;
        getAttribute(name: "address2_longitude"): Attributes.NumberAttribute;
        getAttribute(name: "address2_name"): Attributes.StringAttribute;
        getAttribute(name: "address2_postalcode"): Attributes.StringAttribute;
        getAttribute(name: "address2_postofficebox"): Attributes.StringAttribute;
        getAttribute(name: "address2_shippingmethodcode"): Attributes.OptionSetAttribute;
        getAttribute(name: "address2_stateorprovince"): Attributes.StringAttribute;
        getAttribute(name: "address2_telephone1"): Attributes.StringAttribute;
        getAttribute(name: "address2_telephone2"): Attributes.StringAttribute;
        getAttribute(name: "address2_telephone3"): Attributes.StringAttribute;
        getAttribute(name: "address2_upszone"): Attributes.StringAttribute;
        getAttribute(name: "address2_utcoffset"): Attributes.NumberAttribute;
        getAttribute(name: "budgetamount"): Attributes.NumberAttribute;
        getAttribute(name: "budgetstatus"): Attributes.OptionSetAttribute;
        getAttribute(name: "campaignid"): Attributes.LookupAttribute;
        getAttribute(name: "cm_industry"): Attributes.LookupAttribute;
        getAttribute(name: "cm_industryname"): Attributes.StringAttribute;
        getAttribute(name: "cm_leadid"): Attributes.StringAttribute;
        getAttribute(name: "cm_leadtype"): Attributes.OptionSetAttribute;
        getAttribute(name: "cm_subindustry"): Attributes.LookupAttribute;
        getAttribute(name: "cm_subindustryname"): Attributes.StringAttribute;
        getAttribute(name: "companyname"): Attributes.StringAttribute;
        getAttribute(name: "confirminterest"): Attributes.BooleanAttribute;
        getAttribute(name: "contactid"): Attributes.LookupAttribute;
        getAttribute(name: "createdby"): Attributes.LookupAttribute;
        getAttribute(name: "createdon"): Attributes.DateAttribute;
        getAttribute(name: "createdonbehalfby"): Attributes.LookupAttribute;
        getAttribute(name: "customerid"): Attributes.LookupAttribute;
        getAttribute(name: "decisionmaker"): Attributes.BooleanAttribute;
        getAttribute(name: "description"): Attributes.StringAttribute;
        getAttribute(name: "donotbulkemail"): Attributes.BooleanAttribute;
        getAttribute(name: "donotemail"): Attributes.BooleanAttribute;
        getAttribute(name: "donotfax"): Attributes.BooleanAttribute;
        getAttribute(name: "donotphone"): Attributes.BooleanAttribute;
        getAttribute(name: "donotpostalmail"): Attributes.BooleanAttribute;
        getAttribute(name: "donotsendmm"): Attributes.BooleanAttribute;
        getAttribute(name: "emailaddress1"): Attributes.StringAttribute;
        getAttribute(name: "emailaddress2"): Attributes.StringAttribute;
        getAttribute(name: "emailaddress3"): Attributes.StringAttribute;
        getAttribute(name: "estimatedamount"): Attributes.NumberAttribute;
        getAttribute(name: "estimatedclosedate"): Attributes.DateAttribute;
        getAttribute(name: "estimatedvalue"): Attributes.NumberAttribute;
        getAttribute(name: "evaluatefit"): Attributes.BooleanAttribute;
        getAttribute(name: "exchangerate"): Attributes.NumberAttribute;
        getAttribute(name: "fax"): Attributes.StringAttribute;
        getAttribute(name: "firstname"): Attributes.StringAttribute;
        getAttribute(name: "followemail"): Attributes.BooleanAttribute;
        getAttribute(name: "fullname"): Attributes.StringAttribute;
        getAttribute(name: "importsequencenumber"): Attributes.NumberAttribute;
        getAttribute(name: "industrycode"): Attributes.OptionSetAttribute;
        getAttribute(name: "initialcommunication"): Attributes.OptionSetAttribute;
        getAttribute(name: "isautocreate"): Attributes.BooleanAttribute;
        getAttribute(name: "jobtitle"): Attributes.StringAttribute;
        getAttribute(name: "lastname"): Attributes.StringAttribute;
        getAttribute(name: "lastonholdtime"): Attributes.DateAttribute;
        getAttribute(name: "lastusedincampaign"): Attributes.DateAttribute;
        getAttribute(name: "leadid"): Attributes.StringAttribute;
        getAttribute(name: "leadqualitycode"): Attributes.OptionSetAttribute;
        getAttribute(name: "leadsourcecode"): Attributes.OptionSetAttribute;
        getAttribute(name: "masterid"): Attributes.LookupAttribute;
        getAttribute(name: "merged"): Attributes.BooleanAttribute;
        getAttribute(name: "middlename"): Attributes.StringAttribute;
        getAttribute(name: "mobilephone"): Attributes.StringAttribute;
        getAttribute(name: "modifiedby"): Attributes.LookupAttribute;
        getAttribute(name: "modifiedon"): Attributes.DateAttribute;
        getAttribute(name: "modifiedonbehalfby"): Attributes.LookupAttribute;
        getAttribute(name: "msdyn_leadgrade"): Attributes.OptionSetAttribute;
        getAttribute(name: "msdyn_leadkpiid"): Attributes.LookupAttribute;
        getAttribute(name: "msdyn_leadkpiidname"): Attributes.StringAttribute;
        getAttribute(name: "msdyn_leadscore"): Attributes.NumberAttribute;
        getAttribute(name: "msdyn_leadscoretrend"): Attributes.OptionSetAttribute;
        getAttribute(name: "msdyn_predictivescoreid"): Attributes.LookupAttribute;
        getAttribute(name: "msdyn_predictivescoreidname"): Attributes.StringAttribute;
        getAttribute(name: "msdyn_salesassignmentresult"): Attributes.OptionSetAttribute;
        getAttribute(name: "msdyn_scorehistory"): Attributes.StringAttribute;
        getAttribute(name: "msdyn_scorereasons"): Attributes.StringAttribute;
        getAttribute(name: "msdyn_segmentid"): Attributes.LookupAttribute;
        getAttribute(name: "msdyn_segmentidname"): Attributes.StringAttribute;
        getAttribute(name: "need"): Attributes.OptionSetAttribute;
        getAttribute(name: "numberofemployees"): Attributes.NumberAttribute;
        getAttribute(name: "onholdtime"): Attributes.NumberAttribute;
        getAttribute(name: "originatingcaseid"): Attributes.LookupAttribute;
        getAttribute(name: "overriddencreatedon"): Attributes.DateAttribute;
        getAttribute(name: "ownerid"): Attributes.LookupAttribute;
        getAttribute(name: "owningbusinessunit"): Attributes.LookupAttribute;
        getAttribute(name: "owningbusinessunitname"): Attributes.StringAttribute;
        getAttribute(name: "owningteam"): Attributes.LookupAttribute;
        getAttribute(name: "owninguser"): Attributes.LookupAttribute;
        getAttribute(name: "pager"): Attributes.StringAttribute;
        getAttribute(name: "parentaccountid"): Attributes.LookupAttribute;
        getAttribute(name: "parentcontactid"): Attributes.LookupAttribute;
        getAttribute(name: "participatesinworkflow"): Attributes.BooleanAttribute;
        getAttribute(name: "preferredcontactmethodcode"): Attributes.OptionSetAttribute;
        getAttribute(name: "prioritycode"): Attributes.OptionSetAttribute;
        getAttribute(name: "purchaseprocess"): Attributes.OptionSetAttribute;
        getAttribute(name: "purchasetimeframe"): Attributes.OptionSetAttribute;
        getAttribute(name: "qualificationcomments"): Attributes.StringAttribute;
        getAttribute(name: "qualifyingopportunityid"): Attributes.LookupAttribute;
        getAttribute(name: "relatedobjectid"): Attributes.LookupAttribute;
        getAttribute(name: "revenue"): Attributes.NumberAttribute;
        getAttribute(name: "salesstage"): Attributes.OptionSetAttribute;
        getAttribute(name: "salesstagecode"): Attributes.OptionSetAttribute;
        getAttribute(name: "salutation"): Attributes.StringAttribute;
        getAttribute(name: "schedulefollowup_prospect"): Attributes.DateAttribute;
        getAttribute(name: "schedulefollowup_qualify"): Attributes.DateAttribute;
        getAttribute(name: "sic"): Attributes.StringAttribute;
        getAttribute(name: "slaid"): Attributes.LookupAttribute;
        getAttribute(name: "slainvokedid"): Attributes.LookupAttribute;
        getAttribute(name: "statecode"): Attributes.OptionSetAttribute;
        getAttribute(name: "statuscode"): Attributes.OptionSetAttribute;
        getAttribute(name: "subject"): Attributes.StringAttribute;
        getAttribute(name: "telephone1"): Attributes.StringAttribute;
        getAttribute(name: "telephone2"): Attributes.StringAttribute;
        getAttribute(name: "telephone3"): Attributes.StringAttribute;
        getAttribute(name: "timespentbymeonemailandmeetings"): Attributes.StringAttribute;
        getAttribute(name: "timezoneruleversionnumber"): Attributes.NumberAttribute;
        getAttribute(name: "transactioncurrencyid"): Attributes.LookupAttribute;
        getAttribute(name: "traversedpath"): Attributes.StringAttribute;
        getAttribute(name: "utcconversiontimezonecode"): Attributes.NumberAttribute;
        getAttribute(name: "websiteurl"): Attributes.StringAttribute;
        getAttribute(name: "yomicompanyname"): Attributes.StringAttribute;
        getAttribute(name: "yomifirstname"): Attributes.StringAttribute;
        getAttribute(name: "yomifullname"): Attributes.StringAttribute;
        getAttribute(name: "yomilastname"): Attributes.StringAttribute;
        getAttribute(name: "yomimiddlename"): Attributes.StringAttribute;
        getControl(name: "accountid"): Controls.LookupControl;
        getControl(name: "address1_addresstypecode"): Controls.OptionSetControl;
        getControl(name: "address1_city"): Controls.StringControl;
        getControl(name: "address1_composite"): Controls.StringControl;
        getControl(name: "address1_country"): Controls.StringControl;
        getControl(name: "address1_county"): Controls.StringControl;
        getControl(name: "address1_fax"): Controls.StringControl;
        getControl(name: "address1_latitude"): Controls.NumberControl;
        getControl(name: "address1_line1"): Controls.StringControl;
        getControl(name: "address1_line2"): Controls.StringControl;
        getControl(name: "address1_line3"): Controls.StringControl;
        getControl(name: "address1_longitude"): Controls.NumberControl;
        getControl(name: "address1_name"): Controls.StringControl;
        getControl(name: "address1_postalcode"): Controls.StringControl;
        getControl(name: "address1_postofficebox"): Controls.StringControl;
        getControl(name: "address1_shippingmethodcode"): Controls.OptionSetControl;
        getControl(name: "address1_stateorprovince"): Controls.StringControl;
        getControl(name: "address1_telephone1"): Controls.StringControl;
        getControl(name: "address1_telephone2"): Controls.StringControl;
        getControl(name: "address1_telephone3"): Controls.StringControl;
        getControl(name: "address1_upszone"): Controls.StringControl;
        getControl(name: "address1_utcoffset"): Controls.NumberControl;
        getControl(name: "address2_addresstypecode"): Controls.OptionSetControl;
        getControl(name: "address2_city"): Controls.StringControl;
        getControl(name: "address2_composite"): Controls.StringControl;
        getControl(name: "address2_country"): Controls.StringControl;
        getControl(name: "address2_county"): Controls.StringControl;
        getControl(name: "address2_fax"): Controls.StringControl;
        getControl(name: "address2_latitude"): Controls.NumberControl;
        getControl(name: "address2_line1"): Controls.StringControl;
        getControl(name: "address2_line2"): Controls.StringControl;
        getControl(name: "address2_line3"): Controls.StringControl;
        getControl(name: "address2_longitude"): Controls.NumberControl;
        getControl(name: "address2_name"): Controls.StringControl;
        getControl(name: "address2_postalcode"): Controls.StringControl;
        getControl(name: "address2_postofficebox"): Controls.StringControl;
        getControl(name: "address2_shippingmethodcode"): Controls.OptionSetControl;
        getControl(name: "address2_stateorprovince"): Controls.StringControl;
        getControl(name: "address2_telephone1"): Controls.StringControl;
        getControl(name: "address2_telephone2"): Controls.StringControl;
        getControl(name: "address2_telephone3"): Controls.StringControl;
        getControl(name: "address2_upszone"): Controls.StringControl;
        getControl(name: "address2_utcoffset"): Controls.NumberControl;
        getControl(name: "budgetamount"): Controls.NumberControl;
        getControl(name: "budgetstatus"): Controls.OptionSetControl;
        getControl(name: "campaignid"): Controls.LookupControl;
        getControl(name: "cm_industry"): Controls.LookupControl;
        getControl(name: "cm_industryname"): Controls.StringControl;
        getControl(name: "cm_leadid"): Controls.StringControl;
        getControl(name: "cm_leadtype"): Controls.OptionSetControl;
        getControl(name: "cm_subindustry"): Controls.LookupControl;
        getControl(name: "cm_subindustryname"): Controls.StringControl;
        getControl(name: "companyname"): Controls.StringControl;
        getControl(name: "confirminterest"): Controls.StandardControl;
        getControl(name: "contactid"): Controls.LookupControl;
        getControl(name: "createdby"): Controls.LookupControl;
        getControl(name: "createdon"): Controls.DateControl;
        getControl(name: "createdonbehalfby"): Controls.LookupControl;
        getControl(name: "customerid"): Controls.LookupControl;
        getControl(name: "decisionmaker"): Controls.StandardControl;
        getControl(name: "description"): Controls.StringControl;
        getControl(name: "donotbulkemail"): Controls.StandardControl;
        getControl(name: "donotemail"): Controls.StandardControl;
        getControl(name: "donotfax"): Controls.StandardControl;
        getControl(name: "donotphone"): Controls.StandardControl;
        getControl(name: "donotpostalmail"): Controls.StandardControl;
        getControl(name: "donotsendmm"): Controls.StandardControl;
        getControl(name: "emailaddress1"): Controls.StringControl;
        getControl(name: "emailaddress2"): Controls.StringControl;
        getControl(name: "emailaddress3"): Controls.StringControl;
        getControl(name: "estimatedamount"): Controls.NumberControl;
        getControl(name: "estimatedclosedate"): Controls.DateControl;
        getControl(name: "estimatedvalue"): Controls.NumberControl;
        getControl(name: "evaluatefit"): Controls.StandardControl;
        getControl(name: "exchangerate"): Controls.NumberControl;
        getControl(name: "fax"): Controls.StringControl;
        getControl(name: "firstname"): Controls.StringControl;
        getControl(name: "followemail"): Controls.StandardControl;
        getControl(name: "fullname"): Controls.StringControl;
        getControl(name: "importsequencenumber"): Controls.NumberControl;
        getControl(name: "industrycode"): Controls.OptionSetControl;
        getControl(name: "initialcommunication"): Controls.OptionSetControl;
        getControl(name: "isautocreate"): Controls.StandardControl;
        getControl(name: "jobtitle"): Controls.StringControl;
        getControl(name: "lastname"): Controls.StringControl;
        getControl(name: "lastonholdtime"): Controls.DateControl;
        getControl(name: "lastusedincampaign"): Controls.DateControl;
        getControl(name: "leadid"): Controls.StringControl;
        getControl(name: "leadqualitycode"): Controls.OptionSetControl;
        getControl(name: "leadsourcecode"): Controls.OptionSetControl;
        getControl(name: "masterid"): Controls.LookupControl;
        getControl(name: "merged"): Controls.StandardControl;
        getControl(name: "middlename"): Controls.StringControl;
        getControl(name: "mobilephone"): Controls.StringControl;
        getControl(name: "modifiedby"): Controls.LookupControl;
        getControl(name: "modifiedon"): Controls.DateControl;
        getControl(name: "modifiedonbehalfby"): Controls.LookupControl;
        getControl(name: "msdyn_leadgrade"): Controls.OptionSetControl;
        getControl(name: "msdyn_leadkpiid"): Controls.LookupControl;
        getControl(name: "msdyn_leadkpiidname"): Controls.StringControl;
        getControl(name: "msdyn_leadscore"): Controls.NumberControl;
        getControl(name: "msdyn_leadscoretrend"): Controls.OptionSetControl;
        getControl(name: "msdyn_predictivescoreid"): Controls.LookupControl;
        getControl(name: "msdyn_predictivescoreidname"): Controls.StringControl;
        getControl(name: "msdyn_salesassignmentresult"): Controls.OptionSetControl;
        getControl(name: "msdyn_scorehistory"): Controls.StringControl;
        getControl(name: "msdyn_scorereasons"): Controls.StringControl;
        getControl(name: "msdyn_segmentid"): Controls.LookupControl;
        getControl(name: "msdyn_segmentidname"): Controls.StringControl;
        getControl(name: "need"): Controls.OptionSetControl;
        getControl(name: "numberofemployees"): Controls.NumberControl;
        getControl(name: "onholdtime"): Controls.NumberControl;
        getControl(name: "originatingcaseid"): Controls.LookupControl;
        getControl(name: "overriddencreatedon"): Controls.DateControl;
        getControl(name: "ownerid"): Controls.LookupControl;
        getControl(name: "owningbusinessunit"): Controls.LookupControl;
        getControl(name: "owningbusinessunitname"): Controls.StringControl;
        getControl(name: "owningteam"): Controls.LookupControl;
        getControl(name: "owninguser"): Controls.LookupControl;
        getControl(name: "pager"): Controls.StringControl;
        getControl(name: "parentaccountid"): Controls.LookupControl;
        getControl(name: "parentcontactid"): Controls.LookupControl;
        getControl(name: "participatesinworkflow"): Controls.StandardControl;
        getControl(name: "preferredcontactmethodcode"): Controls.OptionSetControl;
        getControl(name: "prioritycode"): Controls.OptionSetControl;
        getControl(name: "purchaseprocess"): Controls.OptionSetControl;
        getControl(name: "purchasetimeframe"): Controls.OptionSetControl;
        getControl(name: "qualificationcomments"): Controls.StringControl;
        getControl(name: "qualifyingopportunityid"): Controls.LookupControl;
        getControl(name: "relatedobjectid"): Controls.LookupControl;
        getControl(name: "revenue"): Controls.NumberControl;
        getControl(name: "salesstage"): Controls.OptionSetControl;
        getControl(name: "salesstagecode"): Controls.OptionSetControl;
        getControl(name: "salutation"): Controls.StringControl;
        getControl(name: "schedulefollowup_prospect"): Controls.DateControl;
        getControl(name: "schedulefollowup_qualify"): Controls.DateControl;
        getControl(name: "sic"): Controls.StringControl;
        getControl(name: "slaid"): Controls.LookupControl;
        getControl(name: "slainvokedid"): Controls.LookupControl;
        getControl(name: "statecode"): Controls.OptionSetControl;
        getControl(name: "statuscode"): Controls.OptionSetControl;
        getControl(name: "subject"): Controls.StringControl;
        getControl(name: "telephone1"): Controls.StringControl;
        getControl(name: "telephone2"): Controls.StringControl;
        getControl(name: "telephone3"): Controls.StringControl;
        getControl(name: "timespentbymeonemailandmeetings"): Controls.StringControl;
        getControl(name: "timezoneruleversionnumber"): Controls.NumberControl;
        getControl(name: "transactioncurrencyid"): Controls.LookupControl;
        getControl(name: "traversedpath"): Controls.StringControl;
        getControl(name: "utcconversiontimezonecode"): Controls.NumberControl;
        getControl(name: "websiteurl"): Controls.StringControl;
        getControl(name: "yomicompanyname"): Controls.StringControl;
        getControl(name: "yomifirstname"): Controls.StringControl;
        getControl(name: "yomifullname"): Controls.StringControl;
        getControl(name: "yomilastname"): Controls.StringControl;
        getControl(name: "yomimiddlename"): Controls.StringControl;
    }

}

